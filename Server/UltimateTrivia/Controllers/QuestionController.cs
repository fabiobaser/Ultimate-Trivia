using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UltimateTrivia.Database.Game;
using UltimateTrivia.Database.Game.Entities;

namespace UltimateTrivia.Controllers
{
    public class QuestionController : BaseApiController
    {
        private readonly ApplicationDbContext _context;

        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestions([FromQuery] int limit, [FromQuery] string filter)
        {
            IQueryable<Question> query = _context.Questions.Include(q => q.Answers);
            
            if (!string.IsNullOrWhiteSpace(filter))
            {
                query = query.Where(q => q.Content.Contains(filter));
            }
            
            if (limit > 0)
            {
                query = query.Take(limit);
            }

            return Ok(await query.ToListAsync());
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuestionById([FromRoute] string id)
        {
            return Ok(await _context.Questions.Include(q => q.Answers).FirstOrDefaultAsync(q => q.Id == id));
        }
        
        [HttpPost()]
        public async Task<IActionResult> AddQuestions([FromBody] Question question)
        {
            await _context.Questions.AddAsync(question);
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// upload questions via csv
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        /// <para>Question,CorrectAnswer,Answer1,Answer2,Answer3,Category</para>
        /// <para>"In welchem Land hat die Steckdose drei Schlitze, die in einem Dreieck angeordnet sind?",England,Austarlien,Amerika,China,Wunder der Technik</para>
        /// <para>"In welchem technischen Bereich sind PAL, SECAM und NTSC verschiedene Systeme?",Fernsehen,Funktechnik,Betriebsysteme,Flugzeugbau,Wunder der Technik</para>
        /// <para>In welcher Stadt befindet sich das größte naturwissenschaftlich-technische Museum der Welt?,München,San Francisco,Peking,Seoul,Wunder der Technik</para>
        /// <para>Jedes siebte Jahr wird der Eifelturm in einer anderen Farbe gestrichen. Wie lange dauert der gesamte Vorgang ungefähr?,20 Monate,5 Wochen,1 Jahr,8 Monate,Wunder der Technik</para>
        /// <para>Moderne TV-Geräte haben einen so genannten LCD-Bildschirm. Wofür steht LCD?,Liquid Crystal Display,Local Clear Display,Light Clarity Display,Lasting Clear Display,Wunder der Technik</para>
        /// <para>Seit wann besteht in Deutschland Gurtpflicht für Pkw-Hersteller auf den Vordersitzen?,1974,1984,1963,1991,Wunder der Technik</para>
        ///
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("csv-upload")]
        public async Task<IActionResult> AddQuestionsViaCsvFile(IFormFile file)
        {
            using (var reader = new StreamReader(file.OpenReadStream()))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                var records = csv.GetRecords<CsvQuestionUpload>();

                foreach (var csvQuestionUpload in records)
                {
                    var question = new Question
                    {
                        Content = csvQuestionUpload.Question,
                        Type = Question.QuestionType.MultipleChoice,
                        Category = csvQuestionUpload.Category,
                        Answers = new List<Answer>
                        {
                            new Answer
                            {
                                Content = csvQuestionUpload.CorrectAnswer,
                                IsCorrectAnswer = true
                            },
                            new Answer
                            {
                                Content = csvQuestionUpload.Answer1,
                            },
                            new Answer
                            {
                                Content = csvQuestionUpload.Answer2,
                            },
                            new Answer
                            {
                                Content = csvQuestionUpload.Answer3,
                            },
                        }
                    };

                    await _context.Questions.AddAsync(question);
                }

                await _context.SaveChangesAsync();

            }


            return Ok();
        }

        public class CsvQuestionUpload
        {
            public string Question { get; set; }
            public string CorrectAnswer { get; set; }
            public string Answer1 { get; set; }
            public string Answer2 { get; set; }
            public string Answer3 { get; set; }
            public string Category { get; set; }
        }
    }
}