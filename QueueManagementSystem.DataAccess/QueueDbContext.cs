using Microsoft.EntityFrameworkCore;
using QueueManagementSystem.Entities;

namespace QueueManagementSystem.DataAccess
{
    public class QueueDbContext : DbContext
    {
        // Constructor içinde <QueueDbContext> yazması çok kritiktir, yoksa bu hatayı verir.
        public QueueDbContext(DbContextOptions<QueueDbContext> options) : base(options)
        {
        }

        public DbSet<QueueTicket> QueueTickets { get; set; }
        public DbSet<Counter> Counters { get; set; }
        public DbSet<User> Users { get; set; }
    }
}