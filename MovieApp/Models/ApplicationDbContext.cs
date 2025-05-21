using Microsoft.EntityFrameworkCore;

namespace MovieApp.Models
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext>options ):base(options)
        {
        }
        //Add My Domain model
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Movie> Movies { get; set; }
    }
}
