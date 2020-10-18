using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UltimateTrivia.Database.Game.Entities;
using UltimateTrivia.Services;

namespace UltimateTrivia.Database.Game
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IDateProvider _dateProvider;
        private readonly ICurrentUserService _currentUserService;

        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<User> Users { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IDateProvider dateProvider, ICurrentUserService currentUserService) : base(options)
        {
            _dateProvider = dateProvider;
            _currentUserService = currentUserService;
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Question>()
                .HasKey(q => q.Id);
            modelBuilder.Entity<Answer>()
                .HasKey(a => a.Id);
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            
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
                        entry.Entity.CreatedBy = _currentUserService.GetCurrentUserIdentity();
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModified = _dateProvider.Now;
                        entry.Entity.LastModifiedBy = _currentUserService.GetCurrentUserIdentity();
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}