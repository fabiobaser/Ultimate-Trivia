using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using StateMachine;
using Trivia.Hubs;
using Trivia.Hubs.Events;
using Trivia.Services;
using Utils;

namespace Trivia.Application.Game
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
            public string CurrentUser { get; set; }
            public DateTimeOffset QuestionStartedAt { get; set; }
            public List<User> Users { get; set; }
            public Dictionary<string, int> Points { get; set; } = new Dictionary<string, int>();
            public Dictionary<string, Answer> UserAnswers { get; set; } = new Dictionary<string, Answer>();
            public string Category { get; set; }

        }
        
        private readonly IHubContext<TriviaGameHub> _hubContext;
        private readonly IDateProvider _dateProvider;
        private readonly UserManager _userManager;
        private readonly GameConfiguration _configuration;
        private GameData GameStateData;
        
        public Game(ILogger<Game> logger, IHubContext<TriviaGameHub> hubContext, IDateProvider dateProvider, UserManager userManager, GameConfiguration configuration) : base(logger)
        {
            _hubContext = hubContext;
            _dateProvider = dateProvider;
            _userManager = userManager;
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
                .On(GameStateTransition.CalculatePoints).Goto(GameState.CalculatingPoints)
                .OnEnter(CollectingAnswerEnter);

            AddState(GameState.CalculatingPoints)
                .On(GameStateTransition.StartNewRound).Goto(GameState.StartingNewRound)
                .OnEnter(CalculatingPointsEnter);

            AddState(GameState.ShowingFinalResult)
                .On(StateMachineBaseCommand.Done).Goto(StateMachineBaseState.Idle)
                .OnEnter(ShowingFinalResultEnter);
        }

        private async Task StartedEntered(object data, CancellationToken ct)
        {
            GameStateData = new GameData
            {
                CurrentRound = 1
            };
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(ClientCallNames.GameStarted, cancellationToken: ct);
            await MoveNext(GameStateTransition.StartNewRound, ct);
        }
        
        private async Task StartingNewRoundEnter(object data, CancellationToken ct)
        {
            var users = _userManager.GetUsersInLobby(_configuration.LobbyId);

            if (users.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            
            GameStateData.Users = users;
            GameStateData.UserAnswers = new Dictionary<string, GameData.Answer>(); // clear answers from last round
            
            
            var orderedUsers = users.OrderBy(u => u.Name).ToList();

            // get next user in alphabetical order, return null when last user was reached
            var next = orderedUsers.FirstOrDefault(u => string.CompareOrdinal(u.Name, GameStateData.CurrentUser) > 0);

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
            
            GameStateData.CurrentUser = next.Name;

            await MoveNext(GameStateTransition.ShowCategories, ct);
        }
        
        private async Task ShowingCategoriesEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(ClientCallNames.ShowCategories, new ShowCategoriesEvent
            {
                Username = GameStateData.CurrentUser,
                Categories = new List<string>
                {
                    "Im Labor", "Hausparty", "Fabio´s Lieblingsaktivitäten"    // TODO: get random categories from database
                }
            }, ct);

            await MoveNext(GameStateTransition.WaitForCategory, ct);
        }
        
        private async Task CollectingCategoryEnter(object data, CancellationToken ct)
        {
            if (data is CategorySelectedEvent categorySelectedEvent)
            {
                if (categorySelectedEvent.Username != GameStateData.CurrentUser)    // if some other user than current user choose a category go back to waiting for correct event
                {
                    await MoveNext(GameStateTransition.WaitForCategory, ct);
                }
                else
                {
                    GameStateData.Category = categorySelectedEvent.Category;
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
            
            // TODO: get question and answers from database

            GameStateData.QuestionStartedAt = _dateProvider.Now;
            
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(ClientCallNames.ShowQuestion,
                new ShowQuestionEvent
                {
                    Question = "Ole?",
                    Answers = new List<string>
                    {
                        "definitiv", "ne", "ole", "aha"
                    }
                }, cancellationToken: ct);
            
            await MoveNext(GameStateTransition.WaitForAnswers, ct);
        }
        
        private async Task CollectingAnswerEnter(object data, CancellationToken ct)
        {
            var users = _userManager.GetUsersInLobby(_configuration.LobbyId);

            if (users.Count == 0)
            {
                await MoveNext(StateMachineBaseCommand.Done, ct);
                return;
            }
            GameStateData.Users = users;
            
            if (data is AnswerCollectedEvent answerCollectedEvent)
            {
                GameStateData.UserAnswers[answerCollectedEvent.Username] = new GameData.Answer
                {
                    Content = answerCollectedEvent.Answer,
                    AnswerGivenAt = _dateProvider.Now
                };

                if (GameStateData.Users.Any(u => !GameStateData.UserAnswers.ContainsKey(u.Name)))
                {
                    await MoveNext(GameStateTransition.WaitForAnswers, ct);
                }
                else
                {
                    await MoveNext(GameStateTransition.CalculatePoints, ct);
                }
            }
            else
            {
                throw new ApplicationException($"unexpected data received {data.GetType()}");
            }
        }
        
        private async Task CalculatingPointsEnter(object data, CancellationToken ct)
        {
            foreach (var user in GameStateData.UserAnswers)
            {
                GameStateData.Points[user.Key] = 10;    // TODO: calculate points correctly
            }

            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(ClientCallNames.UpdatePoints,
                new UpdatePointsEvent
                {
                    Points = GameStateData.Points
                }, cancellationToken: ct);

            await MoveNext(GameStateTransition.StartNewRound, ct);
        }
        
        private async Task ShowingFinalResultEnter(object data, CancellationToken ct)
        {
            await _hubContext.Clients.Group(_configuration.LobbyId).SendAsync(ClientCallNames.ShowFinalResult,
                new ShowFinalResultEvent
                {
                    Points = GameStateData.Points
                }, cancellationToken: ct);

            await MoveNext(StateMachineBaseCommand.Done, ct);
        }
    }
}