using Microsoft.AspNetCore.Mvc;
using MovieWebApplication.BLL;



namespace MovieWebApplication.Controllers
{
    public class MoviesController : Controller
    {
        private readonly MovieBLL _moviebll;
        public MoviesController(MovieBLL movieBLL)
        {
            _moviebll = movieBLL;

        }

        public async Task<IActionResult> Index(string search, int page = 1)
        {
            var result = await _moviebll.getmovie();
            return View(result);
        }
        public async Task<IActionResult> GetMovies(string? searchTerm,int pagesize,int pageNumber)
        {
            if (pageNumber == 0 || pageNumber == null)
            {
                pageNumber = 1;
                pagesize = 5;
            }
            var movies = await _moviebll.GetPaginationAsync(searchTerm,pagesize, pageNumber);
            return View(movies);
        }

    }
}
