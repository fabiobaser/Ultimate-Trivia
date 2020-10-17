using System;
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
            ShowingQuestion,
            WaitingForAnswers,
            HighlightingCorrectAnswer,
            CalculatingPoints,
            ShowingFinalResult
        }

        public enum EGameCommand
        {
            StartGame,
            StartNewRound,
            ShowCategories,
            WaitForCategory,
            ShowQuestion,
            WaitForAnswers,
            HighlightCorrectAnswer,
            CalculatePoints,
            ShowFinalResult
        }

        public enum EGameEvent
        {
            CategorySelected,
            AnswerSelected,
            PlayerJoined,
            PlayerLeft
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

        #region Configuration

        protected override void Configure()
        {
            base.Configure();

            GetState(StateMachineBaseState.Idle)
                .On(EGameCommand.StartGame).Goto(EGameState.Started);

            AddState(EGameState.Started)
                .On(EGameCommand.StartNewRound).Goto(EGameState.StartingNewRound)
                .OnEnter(StartedEntered);

            AddState(EGameState.StartingNewRound)
                .On(EGameCommand.ShowCategories).Goto(EGameState.ShowingCategories)
                .On(EGameCommand.ShowFinalResult).Goto(EGameState.ShowingFinalResult)
                .On(StateMachineBaseTransition.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(StartingNewRoundEnter);

            AddState(EGameState.ShowingCategories)
                .On(EGameCommand.WaitForCategory).Goto(EGameState.WaitingForCategory)
                .OnEnter(ShowingCategoriesEnter);

            AddState(EGameState.WaitingForCategory)
                .On(EGameCommand.ShowQuestion).Goto(EGameState.ShowingQuestion)
                .OnTimeout(WaitingForCategoryTimeout);

            AddState(EGameState.ShowingQuestion)
                .On(EGameCommand.WaitForAnswers).Goto(EGameState.WaitingForAnswers)
                .OnEnter(ShowingQuestionEnter);

            AddState(EGameState.WaitingForAnswers)
                .On(EGameCommand.HighlightCorrectAnswer).Goto(EGameState.HighlightingCorrectAnswer)
                .OnTimeout(WaitingForAnswersTimeout);

            AddState(EGameState.HighlightingCorrectAnswer)
                .On(EGameCommand.CalculatePoints).Goto(EGameState.CalculatingPoints)
                .OnEnter(HighlightingCorrectAnswerEnter);
            
            AddState(EGameState.CalculatingPoints)
                .On(EGameCommand.StartNewRound).Goto(EGameState.StartingNewRound)
                .OnEnter(CalculatingPointsEnter);

            AddState(EGameState.ShowingFinalResult)
                .On(StateMachineBaseTransition.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(ShowingFinalResultEnter);

            AddEvent(EGameEvent.CategorySelected).WithAction(HandleCategorySelected);
            AddEvent(EGameEvent.AnswerSelected).WithAction(HandleAnswerSelected);
            AddEvent(EGameEvent.PlayerJoined).WithAction(HandleUserJoined);
            AddEvent(EGameEvent.PlayerLeft).WithAction(HandleUserLeft);
        }

        #endregion
        
        
        #region Transitions 
        
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
            };

            GetState(EGameState.WaitingForAnswers).TimeoutAfter(TimeSpan.FromSeconds(gameStartedData.AnswerDuration));
            GetState(EGameState.WaitingForCategory).TimeoutAfter(TimeSpan.FromSeconds(gameStartedData.AnswerDuration));

            LoadPlayers();

            _gameState.Points = _gameState.Players.ToDictionary(p => p.Data.Id, p => 0);
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.GameStarted, new GameStartedEvent
            {
                CurrentRoundNr = _gameState.CurrentRoundNr,
                CurrentQuestionNr = _gameState.CurrentQuestionNr,
                Points = _gameState.Points
            }, cancellationToken: ct);
            await MoveNext(EGameCommand.StartNewRound, ct);
        }
        
        private async Task StartingNewRoundEnter(object data, CancellationToken ct)
        {
            // get next player in alphabetical order, return null when last player was reached
            var next = _gameState.Players.FirstOrDefault(u => string.CompareOrdinal(u.Data.Id, _gameState.CurrentPlayer.Id) > 0);

            if (next == null)
            {
                _gameState.NextRound();
                next = _gameState.Players.First();
                
                // TODO: send event for newRound?
            }
            else
            {
                _gameState.NextQuestion();
                
                // TODO: send event for nextQuestion Round?
            }

            if (_gameState.CurrentRoundNr > _gameState.MaxRounds)
            {
                await MoveNext(EGameCommand.ShowFinalResult, ct);
                return;
            }
            
            _gameState.CurrentPlayer = next.Data;

            await MoveNext(EGameCommand.ShowCategories, ct);
        }
        
        private async Task ShowingCategoriesEnter(object data, CancellationToken ct)
        {
            _gameState.Categories = await _questionRepository.GetRandomCategories(3);
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowCategories, new ShowCategoriesEvent
            {
                CurrentPlayer = _gameState.CurrentPlayer,
                Categories = _gameState.Categories
            }, ct);

            await MoveNext(EGameCommand.WaitForCategory, ct);
        }
        
        private async Task WaitingForCategoryTimeout(object data, CancellationToken ct)
        {
            Logger.LogInformation("no category selected. use random category");

            _gameState.CurrentCategory = _gameState.Categories.First();
                    
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.CategorySelected,
                new CategorySelectedEvent
                {
                    Category = _gameState.CurrentCategory,
                    Player = _gameState.CurrentPlayer
                }, cancellationToken: ct);
                    
            await MoveNext(EGameCommand.ShowQuestion, ct);
            
        }
        
        private async Task ShowingQuestionEnter(object data, CancellationToken ct)
        {
            var question = await _questionRepository.GetRandomQuestionsFromCategory(_gameState.CurrentCategory);

            _gameState.CurrentQuestion = question.Content;
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
            
            await MoveNext(EGameCommand.WaitForAnswers, ct);
        }
        
        private async Task WaitingForAnswersTimeout(object data, CancellationToken ct)
        {
            await MoveNext(EGameCommand.HighlightCorrectAnswer, ct);
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
            
            await MoveNext(EGameCommand.CalculatePoints, ct);
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

            await MoveNext(EGameCommand.StartNewRound, ct);
        }
        
        private async Task ShowingFinalResultEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowFinalResult,
                new ShowFinalResultEvent
                {
                    Points = _gameState.Points
                }, cancellationToken: ct);

            await MoveNext(StateMachineBaseTransition.Done, ct);
        }
        
        #endregion

        
        
        
        
        
        #region Events

        private async Task HandleCategorySelected(object data, CancellationToken ct)
        {
            if (!(data is CategorySelectedData categorySelectedData))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
            
            if (categorySelectedData.Player.Id != _gameState.CurrentPlayer.Id)
            {
                Logger.LogWarning("{playerId} tried to select a category, but it is {currentPlayer}´s turn", categorySelectedData.Player.Id, _gameState.CurrentPlayer);
            }
            else
            {
                Logger.LogDebug("{playerId} selected category {category}", categorySelectedData.Player.Id, categorySelectedData.Category);
                _gameState.CurrentCategory = categorySelectedData.Category;
                    
                await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.CategorySelected,
                    new CategorySelectedEvent
                    {
                        Category = categorySelectedData.Category,
                        Player = categorySelectedData.Player
                    }, cancellationToken: ct);
                    
                await MoveNext(EGameCommand.ShowQuestion, ct);
            }
        }
        private async Task HandleAnswerSelected(object data, CancellationToken ct)
        {
            if (!(data is AnswerSelectedData answerSelectedData))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }

            if (_gameState.CurrentPlayerAnswers.Any(a => a.Player.Id == answerSelectedData.Player.Id))
            {
                Logger.LogInformation($"player {answerSelectedData.Player.Id} already answered");
                return;
            }

            var selectedAnswer = _gameState.CurrentAnswers.FirstOrDefault(a => a.Id == answerSelectedData.AnswerId);

            if (selectedAnswer == null)
            {
                throw new ApplicationException($"Answer {answerSelectedData.AnswerId} doesnt exist");
            }
            
            _gameState.CurrentPlayerAnswers.Add(new GameState.PlayerAnswer
            {
                Answer = selectedAnswer,
                Player = answerSelectedData.Player,
                AnswerGivenAt = _dateProvider.Now
            });

            Logger.LogDebug("{playerId} ({playerName}) selected answer {answer}", answerSelectedData.Player.Id, answerSelectedData.Player.Name, answerSelectedData.AnswerId);

            var remainingPlayers = _gameState.Players
                .Where(player => _gameState.CurrentPlayerAnswers.All(p => player.Data.Id != p.Player.Id))
                .Select(u => u.Data).ToList();
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.PlayerAnswered,
                new AnswerSelectedEvent
                {
                    Player = answerSelectedData.Player,
                    RemainingPlayers = remainingPlayers
                }, cancellationToken: ct);
                
            if (!remainingPlayers.Any())
            {
                await MoveNext(EGameCommand.HighlightCorrectAnswer, ct);
            }
        }
        
        private async Task HandleUserJoined(object data, CancellationToken ct)
        {
            LoadPlayers();
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.GameStarted, new GameStartedEvent
            {
                CurrentRoundNr = _gameState.CurrentRoundNr,
                CurrentQuestionNr = _gameState.CurrentQuestionNr,
                CurrentCategory = _gameState.CurrentCategory,
                CurrentQuestion = _gameState.CurrentQuestion,
                CurrentAnswers = _gameState.CurrentAnswers.Select(a => new Answer
                {
                    Content = a.Content,
                    Id = a.Id
                }).ToList(),
                CurrentPlayer = _gameState.CurrentPlayer,
                Points = _gameState.Points
            }, cancellationToken: ct);
        }
        
        private async Task HandleUserLeft(object data, CancellationToken ct)
        {
            LoadPlayers();
            
            if (_gameState.Players.Count == 0)
            {
                Cancel("no more users in game");
            }

            if (Equals(CurrentState.Name, EGameState.WaitingForCategory))
            {
                Logger.LogInformation("selecting user left. use random category");

                _gameState.CurrentCategory = _gameState.Categories.First();
                    
                await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.CategorySelected,
                    new CategorySelectedEvent
                    {
                        Category = _gameState.CurrentCategory,
                        Player = _gameState.CurrentPlayer
                    }, cancellationToken: ct);
                    
                await MoveNext(EGameCommand.ShowQuestion, ct);
            }
            else if(Equals(CurrentState.Name, EGameState.WaitingForAnswers))
            {
                var remainingPlayers = _gameState.Players
                    .Where(player => _gameState.CurrentPlayerAnswers.All(p => player.Data.Id != p.Player.Id))
                    .Select(u => u.Data).ToList();
                
                if (!remainingPlayers.Any())
                {
                    await MoveNext(EGameCommand.HighlightCorrectAnswer, ct);
                }
            }
        }

        #endregion


        #region Utils

        private void LoadPlayers()
        {
            var players = _playerManager.GetPlayerInLobby(_configuration.LobbyId).OrderBy(u => u.Data.Id).ToList();
            _gameState.Players = players;
        }

        #endregion
        
    }
}