using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Trivial.Database.Entities;
using Trivial.Services;

namespace Trivial.Database
{
    public class ApplicationDbContext : DbContext
    {
        private readonly IDateProvider _dateProvider;

        public ApplicationDbContext(IDateProvider dateProvider)
        {
            _dateProvider = dateProvider;
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