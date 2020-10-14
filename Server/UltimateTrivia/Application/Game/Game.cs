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
    public class Game : StateMachineBase
    {
        public enum GameState
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

        public enum GameStateTransition
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

        public class GameData
        {
            public class Answer
            {
                public string Content { get; set; }
                public DateTimeOffset AnswerGivenAt { get; set; }
            }
            public int CurrentRound { get; set; }
            public string CurrentPlayer { get; set; }
            public DateTimeOffset QuestionStartedAt { get; set; }
            public string CorrectAnswer { get; set; }
            public List<Player> Users { get; set; }
            public Dictionary<string, int> Points { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, Answer> PlayerAnswers { get; set; } = new Dictionary<string, Answer>();
            public string Category { get; set; }

        }
        
        
        private readonly IHubContext<TriviaGameHub> _hubContext;
        private readonly IDateProvider _dateProvider;
        private readonly PlayerManager _playerManager;
        private readonly QuestionRepository _questionRepository;
        private readonly GameConfiguration _configuration;
        
        public string Id { get; set; } = Guid.NewGuid().ToString();
        private GameData GameStateData;
        
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
                .On(GameStateTransition.StartGame).Goto(GameState.Started);

            AddState(GameState.Started)
                .On(GameStateTransition.StartNewRound).Goto(GameState.StartingNewRound)
                .OnEnter(StartedEntered);

            AddState(GameState.StartingNewRound)
                .On(GameStateTransition.ShowCategories).Goto(GameState.ShowingCategories)
                .On(GameStateTransition.ShowFinalResult).Goto(GameState.ShowingFinalResult)
                .On(StateMachineBaseCommand.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(StartingNewRoundEnter);

            AddState(GameState.ShowingCategories)
                .On(GameStateTransition.WaitForCategory).Goto(GameState.WaitingForCategory)
                .OnEnter(ShowingCategoriesEnter);

            AddState(GameState.WaitingForCategory)
                .On(GameStateTransition.CollectCategory).Goto(GameState.CollectingCategory);

            AddState(GameState.CollectingCategory)
                .On(GameStateTransition.ShowQuestion).Goto(GameState.ShowingQuestion)
                .On(GameStateTransition.WaitForCategory).Goto(GameState.WaitingForCategory)
                .OnEnter(CollectingCategoryEnter);
            
            AddState(GameState.ShowingQuestion)
                .On(GameStateTransition.WaitForAnswers).Goto(GameState.WaitingForAnswers)
                .OnEnter(ShowingQuestionEnter);

            AddState(GameState.WaitingForAnswers)
                .On(GameStateTransition.CollectAnswers).Goto(GameState.CollectingAnswers);

            AddState(GameState.CollectingAnswers)
                .On(GameStateTransition.WaitForAnswers).Goto(GameState.WaitingForAnswers)
                .On(GameStateTransition.HighlightCorrectAnswer).Goto(GameState.HighlightingCorrectAnswer)
                .OnEnter(CollectingAnswerEnter);

            AddState(GameState.HighlightingCorrectAnswer)
                .On(GameStateTransition.CalculatePoints).Goto(GameState.CalculatingPoints)
                .OnEnter(HighlightingCorrectAnswerEnter);
            
            AddState(GameState.CalculatingPoints)
                .On(GameStateTransition.StartNewRound).Goto(GameState.StartingNewRound)
                .OnEnter(CalculatingPointsEnter);

            AddState(GameState.ShowingFinalResult)
                .On(StateMachineBaseCommand.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(ShowingFinalResultEnter);
        }

      

        private async Task StartedEntered(object data, CancellationToken ct)
        {
            Logger.LogDebug("Game {gameId} started", Id);
            GameStateData = new GameData
            {
                CurrentRound = 1
            };
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.GameStarted, cancellationToken: ct);
            await MoveNext(GameStateTransition.StartNewRound, ct);
        }
        
        private async Task StartingNewRoundEnter(object data, CancellationToken ct)
        {
            var users = _playerManager.GetPlayerInLobby(_configuration.LobbyId);

            if (users.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            
            GameStateData.Users = users;
            GameStateData.PlayerAnswers = new Dictionary<string, GameData.Answer>(); // clear answers from last round
            
            
            var orderedUsers = users.OrderBy(u => u.Name).ToList();

            // get next user in alphabetical order, return null when last user was reached
            var next = orderedUsers.FirstOrDefault(u => string.CompareOrdinal(u.Name, GameStateData.CurrentPlayer) > 0);

            if (next == null)
            {
                GameStateData.CurrentRound++;
                next = orderedUsers.First();
                
                // TODO: send event for newRound?
            }

            if (GameStateData.CurrentRound > _configuration.Rounds)
            {
                await MoveNext(GameStateTransition.ShowFinalResult, ct);
                return;
            }
            
            GameStateData.CurrentPlayer = next.Name;

            await MoveNext(GameStateTransition.ShowCategories, ct);
        }
        
        private async Task ShowingCategoriesEnter(object data, CancellationToken ct)
        {
            var categories = await _questionRepository.GetRandomCategories(3);
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowCategories, new ShowCategoriesEvent
            {
                Username = GameStateData.CurrentPlayer,
                Categories = categories
            }, ct);

            await MoveNext(GameStateTransition.WaitForCategory, ct);
        }
        
        private async Task CollectingCategoryEnter(object data, CancellationToken ct)
        {
            if (data is CategoryCollectedEvent categorySelectedEvent)
            {
                if (categorySelectedEvent.Username != GameStateData.CurrentPlayer)    // if some other user than current user choose a category go back to waiting for correct event
                {
                    Logger.LogWarning("{username} tried to select a category, but it is {currentUser}´s turn", categorySelectedEvent.Username, GameStateData.CurrentPlayer);
                    await MoveNext(GameStateTransition.WaitForCategory, ct);
                }
                else
                {
                    Logger.LogDebug("{username} selected category {category}", categorySelectedEvent.Username, categorySelectedEvent.Category);
                    GameStateData.Category = categorySelectedEvent.Category;
                    
                    await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.CategoryChoosen,
                        new CategoryCollectedEvent
                        {
                            Category = categorySelectedEvent.Category,
                            Username = categorySelectedEvent.Username
                        }, cancellationToken: ct);
                    
                    await MoveNext(GameStateTransition.ShowQuestion, ct);
                }
            }
            else
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
            
        }
        
        private async Task ShowingQuestionEnter(object data, CancellationToken ct)
        {
            var question = await _questionRepository.GetRandomQuestionsFromCategory(GameStateData.Category);

            GameStateData.QuestionStartedAt = _dateProvider.Now;

            GameStateData.CorrectAnswer = question.Answers.FirstOrDefault(a => a.IsCorrectAnswer)?.Content;

            if (GameStateData.CorrectAnswer == null)
            {
                throw new ApplicationException($"no correct answer for question {question.Id}");
            }
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowQuestion,
                new ShowQuestionEvent
                {
                    Question = question.Content,
                    Answers = question.Answers.Select(a => a.Content).OrderBy(a => Guid.NewGuid()).ToList()
                }, cancellationToken: ct);
            
            await MoveNext(GameStateTransition.WaitForAnswers, ct);
        }
        
        private async Task CollectingAnswerEnter(object data, CancellationToken ct)
        {
            var players = _playerManager.GetPlayerInLobby(_configuration.LobbyId);

            if (players.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            GameStateData.Users = players;
            
            if (data is AnswerCollectedEvent answerCollectedEvent)
            {
                GameStateData.PlayerAnswers[answerCollectedEvent.Username] = new GameData.Answer
                {
                    Content = answerCollectedEvent.Answer,
                    AnswerGivenAt = _dateProvider.Now
                };

                Logger.LogDebug("{username} selected answer {answer}", answerCollectedEvent.Username, answerCollectedEvent.Answer);
                
                await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.UserAnswered,
                    new UserAnsweredEvent
                    {
                        Username = answerCollectedEvent.Username,
                        RemainingUsers = GameStateData.Users.Where(u => !GameStateData.PlayerAnswers.ContainsKey(u.Name)).Select(u => u.Name).ToList()
                    }, cancellationToken: ct);
                
                if (GameStateData.Users.Any(u => !GameStateData.PlayerAnswers.ContainsKey(u.Name)))
                {
                    await MoveNext(GameStateTransition.WaitForAnswers, ct);
                }
                else
                {
                    await MoveNext(GameStateTransition.HighlightCorrectAnswer, ct);
                }
            }
            else
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
        }
        
        private async Task HighlightingCorrectAnswerEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.HighlightCorrectAnswer,
                new HighlightCorrectAnswerEvent
                {
                    CorrectAnswer = GameStateData.CorrectAnswer,
                    UserAnswers = GameStateData.PlayerAnswers.ToDictionary(kv => kv.Key, kv => kv.Value.Content)
                }, cancellationToken: ct);

            await Task.Delay(3000, ct);
            
            await MoveNext(GameStateTransition.CalculatePoints, ct);
        }
        
        private async Task CalculatingPointsEnter(object data, CancellationToken ct)
        {
            var correctAnswers = GameStateData.PlayerAnswers
                .Where(a => a.Value.Content == GameStateData.CorrectAnswer)
                .OrderBy(a => a.Value.AnswerGivenAt)
                .ToList();

            for (var i = 0; i < correctAnswers.Count; i++)
            {
                var answerPair = correctAnswers[i];

                if (GameStateData.Points.ContainsKey(answerPair.Key))
                {
                    GameStateData.Points[answerPair.Key] += GameStateData.Users.Count - i;
                }
                else
                {
                    GameStateData.Points[answerPair.Key] = GameStateData.Users.Count - i;
                }
            }

            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.UpdatePoints,
                new UpdatePointsEvent
                {
                    Points = GameStateData.Points
                }, cancellationToken: ct);

            await MoveNext(GameStateTransition.StartNewRound, ct);
        }
        
        private async Task ShowingFinalResultEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(RpcFunctionNames.ShowFinalResult,
                new ShowFinalResultEvent
                {
                    Points = GameStateData.Points
                }, cancellationToken: ct);

            await MoveNext(StateMachineBaseCommand.Done, ct);
        }
    }
}