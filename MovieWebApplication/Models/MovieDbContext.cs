using Microsoft.EntityFrameworkCore;

namespace MovieWebApplication.Models
{
    public class MovieDbContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; }
        public MovieDbContext(DbContextOptions<MovieDbContext> options) : base(options)
        {
        }
    }
}
