namespace MovieWebApplication.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string? OriginalTitle { get; set; }
        public string? Title { get; set; }
        public string? Overview { get; set; }
        public string? PosterPath { get; set; }
        public string? BackdropPath { get; set; }
        public double? VoteAverage { get; set; }
        public int VoteCount { get; set; }
        public DateTime? ReleaseDate { get; set; }
        //public List<Genre> Genres { get; set; }
    }
    //public class Genre
    //{
    //    public int Id { get; set; }
    //    public string? Name { get; set; }
    //}

    public class MovieResponse
    {
        public int Page { get; set; }
        public List<Movie> Results { get; set; }
        public int TotalPages { get; set; }
        public int TotalResults { get; set; }
    }
    public class PagedResult<T>
    {
        public List<T> Results { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 5;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    }
}
