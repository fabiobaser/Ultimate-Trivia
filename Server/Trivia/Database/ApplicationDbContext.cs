﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Trivia.Database.Entities;
using Trivia.Services;

namespace Trivia.Database
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IDateProvider _dateProvider;
        private readonly ICurrentUserService _currentUserService;

        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }

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
                        entry.Entity.CreatedBy = _currentUserService.GetCurrentUser()?.Name;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModified = _dateProvider.Now;
                        entry.Entity.LastModifiedBy = _currentUserService.GetCurrentUser()?.Name;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}