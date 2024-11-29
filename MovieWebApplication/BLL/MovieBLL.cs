using MovieWebApplication.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Data.SqlClient;
using System.Data;
using Dapper;


namespace MovieWebApplication.BLL
{
    public class MovieBLL
    {


        private readonly MovieDbContext _context;
        private readonly HttpClient _httpClient;

        public MovieBLL(MovieDbContext context, HttpClient httpClient)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> SendHttpRequestAsync(string url, HttpMethod method, object payload = null, Dictionary<string, string> headers = null)
        {
            try
            {
                // Prepare the request
                HttpRequestMessage request = new HttpRequestMessage(method, url);

                // Add payload for POST/PUT methods
                if (payload != null && (method == HttpMethod.Post || method == HttpMethod.Put))
                {
                    string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                }

                // Add custom headers if provided
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Send the request and get the response
                HttpResponseMessage response = await _httpClient.SendAsync(request);

                // Ensure success status code or throw exception
                response.EnsureSuccessStatusCode();

                // Return the response content
                return await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException ex)
            {
                // Handle HTTP-specific errors
                throw new Exception($"Request error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Handle other exceptions
                throw new Exception($"Unexpected error: {ex.Message}");
            }
        }
        public async Task<MovieResponse> getmovie()
        {
            string url = "https://api.themoviedb.org/3/movie/popular";
            var headers = new Dictionary<string, string>
            {
                { "Authorization", "Bearer eyJhbGciOiJIUzI1NiJ9.eyJhdWQiOiI0NGM3NzhjNTg1YTExNjk2MzY3NWZkMDlmZjI1NjdlMSIsIm5iZiI6MTczMjcxMTExOC4wMzk1NzU2LCJzdWIiOiI2NzQ3MTE2ZmM0NmViZDFmZDRhNDI1YzUiLCJzY29wZXMiOlsiYXBpX3JlYWQiXSwidmVyc2lvbiI6MX0._0-LcBOJ5Hy-iUflBePf6fDwlVE6QZAw2F-hJ32Wsas" }

            };
            string response = await SendHttpRequestAsync(url, HttpMethod.Get, null, headers);
            var movieResponse = JsonConvert.DeserializeObject<MovieResponse>(response);

            await SaveMoviesToDatabaseAsync(movieResponse.Results);

            return movieResponse;
        }      

        private async Task SaveMoviesToDatabaseAsync(List<Movie> movies)
        {
            // Use async/await when querying the DB to avoid blocking the thread
            var existingMovieIds = await _context.Movies
                .Where(m => movies.Select(movie => movie.Id).Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync();

            // Separate movies into ones to update and new ones to add
            var moviesToUpdate = new List<Movie>();
            var moviesToAdd = new List<Movie>();

            foreach (var movie in movies)
            {
                if (existingMovieIds.Contains(movie.Id))
                {
                    // If the movie exists, update its details
                    var existingMovie = await _context.Movies.FirstOrDefaultAsync(m => m.Id == movie.Id);
                    if (existingMovie != null)
                    {
                        existingMovie.OriginalTitle = movie.OriginalTitle;
                        existingMovie.Title = movie.Title;
                        existingMovie.Overview = movie.Overview;
                        existingMovie.PosterPath = movie.PosterPath;
                        existingMovie.BackdropPath = movie.BackdropPath;
                        existingMovie.VoteAverage = movie.VoteAverage;
                        existingMovie.VoteCount = movie.VoteCount;
                        existingMovie.ReleaseDate = movie.ReleaseDate;

                        moviesToUpdate.Add(existingMovie);
                    }
                }
                else
                {
                    // If the movie does not exist, create a new one without setting the Id
                    var newMovie = new Movie
                    {
                        OriginalTitle = movie.OriginalTitle,
                        Title = movie.Title,
                        Overview = movie.Overview,
                        PosterPath = movie.PosterPath,
                        BackdropPath = movie.BackdropPath,
                        VoteAverage = movie.VoteAverage,
                        VoteCount = movie.VoteCount,
                        ReleaseDate = movie.ReleaseDate
                        // Do not set Id here
                    };
                    moviesToAdd.Add(newMovie);
                }
            }

            // Perform bulk insert and update operations
            if (moviesToAdd.Any())
            {
                await _context.Movies.AddRangeAsync(moviesToAdd);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<Movie>> GetPaginationAsync(string? searchterm,int pagesize,int pageindex)
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@PageNumber", pageindex, DbType.Int32);
                parameters.Add("@PageSize", pagesize, DbType.Int32);
                parameters.Add("@SearchTerm", searchterm, DbType.String);
                var movies = await connection.QueryAsync<Movie>(
                    "sp_Pagination",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                var totalCount = await connection.QuerySingleAsync<int>(
            "SELECT COUNT(*) FROM Movies WHERE Title LIKE @SearchTerm",
            new { SearchTerm = "%" + searchterm + "%" });
                return new PagedResult<Movie>
                {
                    Results = movies.ToList(),
                    CurrentPage = pageindex,
                    PageSize = pagesize,
                    TotalItems = totalCount
                };
            }
        }
    }
}
