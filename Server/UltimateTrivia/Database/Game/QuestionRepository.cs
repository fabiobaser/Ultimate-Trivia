using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UltimateTrivia.Database.Game.Entities;

namespace UltimateTrivia.Database.Game
{
    public class QuestionRepository
    {
        private readonly ApplicationDbContext _context;

        public QuestionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<string>> GetRandomCategories(int count)
        {
            var categories = await _context.Questions
                .Select(q => q.Category)
                .Distinct()
                .OrderBy(c => Guid.NewGuid())
                .Take(count)
                .ToListAsync();

            return categories;
        }

        public async Task<Question> GetRandomQuestionsFromCategory(string category)
        {
            try
            {
                return await _context.Questions
                    .Where(q => q.Category == category)
                    .Include(q => q.Answers)
                    .OrderBy(q => Guid.NewGuid())
                    .FirstAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }
        
    }
}