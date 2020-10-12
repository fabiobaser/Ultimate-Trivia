using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Trivia.Database;

namespace Trivia.Controllers
{
    public class QuestionController : BaseController
    {
        private readonly ApplicationDbContext _context;

        public QuestionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuestions([FromQuery] int limit)
        {
            if (limit > 0)
            {
                return Ok(await _context.Questions.Take(limit).ToListAsync());
            }

            return Ok(await _context.Questions.ToListAsync());
        }
    }
}