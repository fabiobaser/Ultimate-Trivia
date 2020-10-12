using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Trivia.Database.Entities;
using Trivia.Services;

namespace Trivia.Database
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IDateProvider _dateProvider;

        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateProvider dateProvider) : base(options)
        {
            _dateProvider = dateProvider;
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Question>()
                .HasKey(q => q.Id);
            modelBuilder.Entity<Answer>()
                .HasKey(a => a.Id);

            modelBuilder.Entity<Question>()
                .Property(q => q.Type)
                .HasConversion<string>();

            modelBuilder.Entity<Question>()
                .HasMany(q => q.Answers)
                .WithOne(q => q.Question)
                .HasForeignKey(a => a.QuestionId);
            
            base.OnModelCreating(modelBuilder);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.Created = _dateProvider.Now;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModified = _dateProvider.Now;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}