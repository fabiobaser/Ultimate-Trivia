using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StateMachine;
using UltimateTrivia.Application.Game.TransitionData;
using UltimateTrivia.Constants;
using UltimateTrivia.Database.Game;
using UltimateTrivia.Hubs;
using UltimateTrivia.Hubs.Events;
using UltimateTrivia.Hubs.Events.Models;
using UltimateTrivia.Services;
using Utils;
using CategorySelectedEvent = UltimateTrivia.Application.Game.TransitionData.CategorySelectedEvent;

namespace UltimateTrivia.Application.Game
{
    public partial class Game : StateMachineBase
    {
        public enum EGameState
        {
            Started,
            StartingNewRound,
            ShowingCategories,
            WaitingForCategory,
            CollectingCategory,
            ShowingQuestion,
            WaitingForAnswers,
            CollectingAnswers,
            HighlightingCorrectAnswer,
            CalculatingPoints,
            ShowingFinalResult
        }

        public enum EGameStateTransition
        {
            StartGame,
            StartNewRound,
            ShowCategories,
            WaitForCategory,
            CollectCategory,
            ShowQuestion,
            WaitForAnswers,
            CollectAnswers,
            HighlightCorrectAnswer,
            CalculatePoints,
            ShowFinalResult
        }


        private readonly IHubContext<TriviaGameHub> _hubContext;
        private readonly IDateProvider _dateProvider;
        private readonly PlayerManager _playerManager;
        private readonly QuestionRepository _questionRepository;
        private readonly GameConfiguration _configuration;
        
        public string Id { get; } = Guid.NewGuid().ToString();
        private GameState _gameState;
        
        public Game(ILogger<Game> logger, IHubContext<TriviaGameHub> hubContext, IDateProvider dateProvider, PlayerManager playerManager, QuestionRepository questionRepository, GameConfiguration configuration) : base(logger)
        {
            _hubContext = hubContext;
            _dateProvider = dateProvider;
            _playerManager = playerManager;
            _questionRepository = questionRepository;
            _configuration = configuration;
        }

        protected override void AddStates()
        {
            base.AddStates();

            GetState(StateMachineBaseState.Idle)
                .On(EGameStateTransition.StartGame).Goto(EGameState.Started);

            AddState(EGameState.Started)
                .On(EGameStateTransition.StartNewRound).Goto(EGameState.StartingNewRound)
                .OnEnter(StartedEntered);

            AddState(EGameState.StartingNewRound)
                .On(EGameStateTransition.ShowCategories).Goto(EGameState.ShowingCategories)
                .On(EGameStateTransition.ShowFinalResult).Goto(EGameState.ShowingFinalResult)
                .On(StateMachineBaseCommand.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(StartingNewRoundEnter);

            AddState(EGameState.ShowingCategories)
                .On(EGameStateTransition.WaitForCategory).Goto(EGameState.WaitingForCategory)
                .OnEnter(ShowingCategoriesEnter);

            AddState(EGameState.WaitingForCategory)
                .On(EGameStateTransition.CollectCategory).Goto(EGameState.CollectingCategory);

            AddState(EGameState.CollectingCategory)
                .On(EGameStateTransition.ShowQuestion).Goto(EGameState.ShowingQuestion)
                .On(EGameStateTransition.WaitForCategory).Goto(EGameState.WaitingForCategory)
                .OnEnter(CollectingCategoryEnter);
            
            AddState(EGameState.ShowingQuestion)
                .On(EGameStateTransition.WaitForAnswers).Goto(EGameState.WaitingForAnswers)
                .OnEnter(ShowingQuestionEnter);

            AddState(EGameState.WaitingForAnswers)
                .On(EGameStateTransition.CollectAnswers).Goto(EGameState.CollectingAnswers);

            AddState(EGameState.CollectingAnswers)
                .On(EGameStateTransition.WaitForAnswers).Goto(EGameState.WaitingForAnswers)
                .On(EGameStateTransition.HighlightCorrectAnswer).Goto(EGameState.HighlightingCorrectAnswer)
                .OnEnter(CollectingAnswerEnter);

            AddState(EGameState.HighlightingCorrectAnswer)
                .On(EGameStateTransition.CalculatePoints).Goto(EGameState.CalculatingPoints)
                .OnEnter(HighlightingCorrectAnswerEnter);
            
            AddState(EGameState.CalculatingPoints)
                .On(EGameStateTransition.StartNewRound).Goto(EGameState.StartingNewRound)
                .OnEnter(CalculatingPointsEnter);

            AddState(EGameState.ShowingFinalResult)
                .On(StateMachineBaseCommand.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(ShowingFinalResultEnter);
        }

      

        private async Task StartedEntered(object data, CancellationToken ct)
        {
            if (!(data is GameStartedData gameStartedData))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }

            Logger.LogDebug("Game {gameId} started", Id);
            _gameState = new GameState
            {
                CurrentRoundNr = 1,
                CurrentQuestionNr = 1,
                MaxRounds = gameStartedData.Rounds,
                AnswerDuration = gameStartedData.AnswerDuration
            };
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.GameStarted, cancellationToken: ct);
            await MoveNext(EGameStateTransition.StartNewRound, ct);
        }
        
        private async Task StartingNewRoundEnter(object data, CancellationToken ct)
        {
            var players = _playerManager.GetPlayerInLobby(_configuration.LobbyId);

            if (players.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            
            _gameState.Players = players;
            _gameState.CurrentPlayerAnswers = new List<GameState.PlayerAnswer>(); // clear answers from last round
            
            
            var orderedPlayers = players.OrderBy(u => u.Data.Id).ToList();

            // get next user in alphabetical order, return null when last user was reached
            var next = orderedPlayers.FirstOrDefault(u => string.CompareOrdinal(u.Data.Id, _gameState.CurrentPlayer.Id) > 0);

            if (next == null)
            {
                _gameState.CurrentRoundNr++;
                next = orderedPlayers.First();
                
                // TODO: send event for newRound?
            }

            if (_gameState.CurrentRoundNr > _gameState.MaxRounds)
            {
                await MoveNext(EGameStateTransition.ShowFinalResult, ct);
                return;
            }
            
            _gameState.CurrentPlayer = next.Data;

            await MoveNext(EGameStateTransition.ShowCategories, ct);
        }
        
        private async Task ShowingCategoriesEnter(object data, CancellationToken ct)
        {
            var categories = await _questionRepository.GetRandomCategories(3);
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowCategories, new ShowCategoriesEvent
            {
                CurrentPlayer = _gameState.CurrentPlayer,
                Categories = categories
            }, ct);

            await MoveNext(EGameStateTransition.WaitForCategory, ct);
        }
        
        private async Task CollectingCategoryEnter(object data, CancellationToken ct)
        {
            if (!(data is CategorySelectedEvent categoryChosenEvent))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
            
            if (categoryChosenEvent.CurrentPlayer.Id != _gameState.CurrentPlayer.Id)    // if some other user than current user choose a category go back to waiting for correct event
            {
                Logger.LogWarning("{userId} tried to select a category, but it is {currentUser}´s turn", categoryChosenEvent.CurrentPlayer.Id, _gameState.CurrentPlayer);
                await MoveNext(EGameStateTransition.WaitForCategory, ct);
            }
            else
            {
                Logger.LogDebug("{userId} selected category {category}", categoryChosenEvent.CurrentPlayer.Id, categoryChosenEvent.Category);
                _gameState.CurrentCategory = categoryChosenEvent.Category;
                    
                await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.CategorySelected,
                    new CategorySelectedEvent
                    {
                        Category = categoryChosenEvent.Category,
                        CurrentPlayer = categoryChosenEvent.CurrentPlayer
                    }, cancellationToken: ct);
                    
                await MoveNext(EGameStateTransition.ShowQuestion, ct);
            }
        }
        
        private async Task ShowingQuestionEnter(object data, CancellationToken ct)
        {
            var question = await _questionRepository.GetRandomQuestionsFromCategory(_gameState.CurrentCategory);

            _gameState.CurrentQuestionStartedAt = _dateProvider.Now;

            var answers = question.Answers.OrderBy(a => Guid.NewGuid()).ToList();
            
            _gameState.CurrentAnswers = answers.Select(a => new GameState.Answer
            {
                Content = a.Content,
                IsCorrect = a.IsCorrectAnswer,
                Id = a.Id
            }).ToList();

            if (!_gameState.CurrentAnswers.Any(a => a.IsCorrect))
            {
                throw new ApplicationException($"no correct answer for question {question.Id}");
            }
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowQuestion,
                new ShowQuestionEvent
                {
                    Question = question.Content,
                    Answers = answers.Select(a => new Answer
                    {
                        Content = a.Content,
                        Id = a.Id
                    }).ToList()
                }, cancellationToken: ct);
            
            await MoveNext(EGameStateTransition.WaitForAnswers, ct);
        }
        
        private async Task CollectingAnswerEnter(object data, CancellationToken ct)
        {
            var players = _playerManager.GetPlayerInLobby(_configuration.LobbyId);

            if (players.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            _gameState.Players = players;
            
            if (!(data is AnswerSelectedEvent answerCollectedEvent))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }

            if (_gameState.CurrentPlayerAnswers.Any(a => a.Player.Id == answerCollectedEvent.Player.Id))
            {
                Logger.LogInformation($"player {answerCollectedEvent.Player.Id} already answered");
                return;
            }

            var selectedAnswer = _gameState.CurrentAnswers.FirstOrDefault(a => a.Id == answerCollectedEvent.AnswerId);

            if (selectedAnswer == null)
            {
                throw new ApplicationException($"Answer {answerCollectedEvent.AnswerId} doesnt exist");
            }
            
            _gameState.CurrentPlayerAnswers.Add(new GameState.PlayerAnswer
            {
                Answer = selectedAnswer,
                Player = answerCollectedEvent.Player,
                AnswerGivenAt = _dateProvider.Now
            });
            

            Logger.LogDebug("{username} selected answer {answer}", answerCollectedEvent.Player.Id, answerCollectedEvent.AnswerId);

            var remainingPlayers = _gameState.Players
                .Where(player => _gameState.CurrentPlayerAnswers.All(p => player.Data.Id != p.Player.Id))
                .Select(u => u.Data).ToList();
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.PlayerAnswered,
                new PlayerAnsweredEvent
                {
                    Player = answerCollectedEvent.Player,
                    RemainingPlayers = remainingPlayers
                }, cancellationToken: ct);
                
            if (remainingPlayers.Any())
            {
                await MoveNext(EGameStateTransition.WaitForAnswers, ct);
            }
            else
            {
                await MoveNext(EGameStateTransition.HighlightCorrectAnswer, ct);
            }
        }
        
        private async Task HighlightingCorrectAnswerEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.HighlightCorrectAnswer,
                new HighlightCorrectAnswerEvent
                {
                    Answers = _gameState.CurrentAnswers.Select(a => new HighlightCorrectAnswerEvent.PlayerAnswer
                    {
                        Answer = new Answer
                        {
                            Content = a.Content,
                            Id = a.Id
                        },
                        Correct = a.IsCorrect,
                        SelectedBy = _gameState.CurrentPlayerAnswers.Where(pa => pa.Answer.IsCorrect).Select(pa => pa.Player).ToList()
                    }).ToList()
                }, cancellationToken: ct);

            await Task.Delay(5000, ct);
            
            await MoveNext(EGameStateTransition.CalculatePoints, ct);
        }
        
        private async Task CalculatingPointsEnter(object data, CancellationToken ct)
        {
            var correctAnswers = _gameState.CurrentPlayerAnswers
                .Where(pa => pa.Answer.IsCorrect)
                .OrderBy(pa => pa.AnswerGivenAt)
                .ToList();

            for (var i = 0; i < correctAnswers.Count; i++)
            {
                var answerPair = correctAnswers[i];

                if (_gameState.Points.ContainsKey(answerPair.Player.Id))
                {
                    _gameState.Points[answerPair.Player.Id] += _gameState.Players.Count - i;
                }
                else
                {
                    _gameState.Points[answerPair.Player.Id] = _gameState.Players.Count - i;
                }
            }

            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.UpdatePoints,
                new UpdatePointsEvent
                {
                    Points = _gameState.Points
                }, cancellationToken: ct);

            await MoveNext(EGameStateTransition.StartNewRound, ct);
        }
        
        private async Task ShowingFinalResultEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowFinalResult,
                new ShowFinalResultEvent
                {
                    Points = _gameState.Points
                }, cancellationToken: ct);

            await MoveNext(StateMachineBaseCommand.Done, ct);
        }
    }
}