using System.ComponentModel.DataAnnotations;

namespace MovieApp.Models
{
    public class Movie
    {
        #region Properties
        //Primary Key
        public int Id { get; set; }
        [Required,MaxLength(250)]
        public string Title { get; set; }
        public int Year { get; set; }
        public double Rate { get; set; }
        [Required,MaxLength(2500)]
        public string StoryLine { get; set; }
        [Required]
        public  byte[] Poster { get; set; }
        //Foreign Key =>using Navigation Property 
        public byte GenreId { get; set; }
        public Genre Genre { get; set; }
        #endregion
    }
}
