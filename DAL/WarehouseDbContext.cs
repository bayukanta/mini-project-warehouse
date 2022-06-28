using Microsoft.EntityFrameworkCore;
using DAL.Models;

namespace DAL
{
    public class WarehouseDbContext : DbContext
    {
        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            //add index here
        }
        public DbSet<ODStatus> ODStatus { get; set; }

    }
}
