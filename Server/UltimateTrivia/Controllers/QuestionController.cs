using System.Linq;
using System.Threading.Tasks;
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
    }
}