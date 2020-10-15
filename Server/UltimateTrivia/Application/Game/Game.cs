using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StateMachine;
using UltimateTrivia.Constants;
using UltimateTrivia.Database.Game;
using UltimateTrivia.Hubs;
using UltimateTrivia.Hubs.Events;
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
            var users = _playerManager.GetPlayerInLobby(_configuration.LobbyId);

            if (users.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            
            _gameState.Users = users;
            _gameState.CurrentPlayerAnswers = new Dictionary<string, GameState.PlayerAnswer>(); // clear answers from last round
            
            
            var orderedUsers = users.OrderBy(u => u.Name).ToList();

            // get next user in alphabetical order, return null when last user was reached
            var next = orderedUsers.FirstOrDefault(u => string.CompareOrdinal(u.Name, _gameState.CurrentPlayer) > 0);

            if (next == null)
            {
                _gameState.CurrentRoundNr++;
                next = orderedUsers.First();
                
                // TODO: send event for newRound?
            }

            if (_gameState.CurrentRoundNr > _gameState.MaxRounds)
            {
                await MoveNext(EGameStateTransition.ShowFinalResult, ct);
                return;
            }
            
            _gameState.CurrentPlayer = next.Name;

            await MoveNext(EGameStateTransition.ShowCategories, ct);
        }
        
        private async Task ShowingCategoriesEnter(object data, CancellationToken ct)
        {
            var categories = await _questionRepository.GetRandomCategories(3);
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowCategories, new ShowCategoriesEvent
            {
                Username = _gameState.CurrentPlayer,
                Categories = categories
            }, ct);

            await MoveNext(EGameStateTransition.WaitForCategory, ct);
        }
        
        private async Task CollectingCategoryEnter(object data, CancellationToken ct)
        {
            if (!(data is CategoryCollectedData categorySelectedEvent))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
            
            if (categorySelectedEvent.Username != _gameState.CurrentPlayer)    // if some other user than current user choose a category go back to waiting for correct event
            {
                Logger.LogWarning("{username} tried to select a category, but it is {currentUser}´s turn", categorySelectedEvent.Username, _gameState.CurrentPlayer);
                await MoveNext(EGameStateTransition.WaitForCategory, ct);
            }
            else
            {
                Logger.LogDebug("{username} selected category {category}", categorySelectedEvent.Username, categorySelectedEvent.Category);
                _gameState.CurrentCategory = categorySelectedEvent.Category;
                    
                await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.CategoryChoosen,
                    new CategoryCollectedData
                    {
                        Category = categorySelectedEvent.Category,
                        Username = categorySelectedEvent.Username
                    }, cancellationToken: ct);
                    
                await MoveNext(EGameStateTransition.ShowQuestion, ct);
            }
        }
        
        private async Task ShowingQuestionEnter(object data, CancellationToken ct)
        {
            var question = await _questionRepository.GetRandomQuestionsFromCategory(_gameState.CurrentCategory);

            _gameState.CurrentQuestionStartedAt = _dateProvider.Now;

            _gameState.CurrentAnswers = question.Answers.Select(a => new GameState.Answer
            {
                Content = a.Content,
                IsCorrect = a.IsCorrectAnswer
            }).ToList();

            if (!_gameState.CurrentAnswers.Any(a => a.IsCorrect))
            {
                throw new ApplicationException($"no correct answer for question {question.Id}");
            }
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowQuestion,
                new ShowQuestionEvent
                {
                    Question = question.Content,
                    Answers = question.Answers.Select(a => a.Content).OrderBy(a => Guid.NewGuid()).ToList()
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
            _gameState.Users = players;
            
            if (!(data is AnswerCollectedData answerCollectedEvent))
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
            
            _gameState.CurrentPlayerAnswers[answerCollectedEvent.Username] = new GameState.PlayerAnswer
            {
                Content = answerCollectedEvent.Answer,
                AnswerGivenAt = _dateProvider.Now
            };

            Logger.LogDebug("{username} selected answer {answer}", answerCollectedEvent.Username, answerCollectedEvent.Answer);
                
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.UserAnswered,
                new UserAnsweredEvent
                {
                    Username = answerCollectedEvent.Username,
                    RemainingUsers = _gameState.Users.Where(u => !_gameState.CurrentPlayerAnswers.ContainsKey(u.Name)).Select(u => u.Name).ToList()
                }, cancellationToken: ct);
                
            if (_gameState.Users.Any(u => !_gameState.CurrentPlayerAnswers.ContainsKey(u.Name)))
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
                    Answers = _gameState.CurrentAnswers.Select(a => new HighlightCorrectAnswerEvent.Answer
                    {
                        Content = a.Content,
                        Correct = a.IsCorrect,
                        SelectedBy = _gameState.CurrentPlayerAnswers.Where(pa => pa.Value.Content == a.Content).Select(pa => pa.Key).ToList()
                    }).ToList()
                }, cancellationToken: ct);

            await Task.Delay(5000, ct);
            
            await MoveNext(EGameStateTransition.CalculatePoints, ct);
        }
        
        private async Task CalculatingPointsEnter(object data, CancellationToken ct)
        {
            var correctAnswers = _gameState.CurrentPlayerAnswers
                .Where(a => a.Value.Content == _gameState.CurrentCorrectAnswer)
                .OrderBy(a => a.Value.AnswerGivenAt)
                .ToList();

            for (var i = 0; i < correctAnswers.Count; i++)
            {
                var answerPair = correctAnswers[i];

                if (_gameState.Points.ContainsKey(answerPair.Key))
                {
                    _gameState.Points[answerPair.Key] += _gameState.Users.Count - i;
                }
                else
                {
                    _gameState.Points[answerPair.Key] = _gameState.Users.Count - i;
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