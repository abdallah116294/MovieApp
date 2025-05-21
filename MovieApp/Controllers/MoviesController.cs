using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieApp.Models;
using MovieApp.ViewModels;
using NToastNotify;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MovieApp.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext; //get an object from AppilcationDbContext to deal with db 
        private List<string> _allowedExtentions = new List<string> { ".jpg", ".png" };
        private long _maxLAllowedPosterSize = 1048576;
        private readonly IToastNotification _toastNoftification;
        //Add it in Constructor
        public MoviesController(ApplicationDbContext context, IToastNotification toastNotification)
        {
            _applicationDbContext = context;
            _toastNoftification = toastNotification;

        }
        public async Task<IActionResult> Index()
        {
            var movies = await _applicationDbContext.Movies.OrderByDescending(m=>m.Rate).ToListAsync();
            return View(movies);
        }
        //create new Acction 
        public async Task<IActionResult> Create()
        {
            var viewModel = new MovieFromViewModel
            {
                Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync()
            };
            return View("MovieForm", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovieFromViewModel model)
        {
            //chek model state 
            if (!ModelState.IsValid)
            {
                model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
                //populate the Genres
            }
            //chek for poster
            var file = Request.Form.Files;//get all files attched
            if (!file.Any())
            {
                model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster","Pleas check  Poster uplodaed");
                return View("MovieForm", model);
            }
            //check for file is allow or not
            var poster = file.FirstOrDefault();
            var allowedExtenstions = new List<string> {".jpg", ".png"};
            //Make one function for check the poster 
            var validatePosterAsync = await ValidatePosterAsync(poster,model);
            if (validatePosterAsync != null)
                return validatePosterAsync;
            //if (!_allowedExtentions.Contains(Path.GetExtension(poster.FileName).ToLower()))
            //    {
            //    model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
            //    ModelState.AddModelError("Poster", "Only .PNG .JPG images are allowed");
            //    return View("MovieForm", model);

            //     }
            //if(poster.Length> _maxLAllowedPosterSize)
            //{
            //    model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
            //    ModelState.AddModelError("Poster", "Poster can not be more than 1 MB");
            //    return View("MovieForm", model);
            //}
            using var datastrem = new MemoryStream();
            await poster.CopyToAsync(datastrem);
            //Maping the viwemodel to movie 
            var movie = new Movie
            {
                Title=model.Title,
                GenreId=model.GenreId,
                Year=model.Year,
                Rate=model.Rate,
                StoryLine=model.StoryLine,
                Poster=datastrem.ToArray()
            };
            _applicationDbContext.Movies.Add(movie);
            _applicationDbContext.SaveChanges();
            _toastNoftification.AddSuccessToastMessage("Movie Created Successfully ");
            return RedirectToAction(nameof(Index));
        }
        //Edit Movies 
        public async Task<IActionResult> Edit(int? id) 
        {
            if (id == null)
                return BadRequest();
            var movie = await _applicationDbContext.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();
            var viewModel = new MovieFromViewModel
            {
                Id=movie.Id,
                Title=movie.Title,
                GenreId=movie.GenreId,
                Rate=movie.Rate,
                Year=movie.Year,
                StoryLine=movie.StoryLine,
                Poster=movie.Poster,
                Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync()
            };

            return View("MovieForm", viewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MovieFromViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                return View("MovieForm", model);
                //populate the Genres
            }
            var movie = await _applicationDbContext.Movies.FindAsync( model.Id);
            if (movie == null)
                return NotFound();
            var file = Request.Form.Files;
            if (file.Any())
            {
                var poster = file.FirstOrDefault();
                using var dataStream = new MemoryStream();
                await poster.CopyToAsync(dataStream);
                model.Poster = dataStream.ToArray();
                //if (!_allowedExtentions.Contains(Path.GetExtension(poster.FileName).ToLower()))
                //{
                //    model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                //    ModelState.AddModelError("Poster", "Only .PNG .JPG images are allowed");
                //    return View("MovieForm", model);

                //}
                //if (poster.Length > _maxLAllowedPosterSize)
                //{
                //    model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                //    ModelState.AddModelError("Poster", "Poster can not be more than 1 MB");
                //    return View("MovieForm", model);
                //}
                var validatePosterAsync =await ValidatePosterAsync(poster ,model);
                if (validatePosterAsync != null)
                    return validatePosterAsync;
                movie.Poster = model.Poster;
            }
            movie.Title = model.Title;
            movie.StoryLine = model.StoryLine;
            movie.Rate = model.Rate;
            movie.Year = model.Year;
            movie.GenreId = model.GenreId;
            _applicationDbContext.SaveChanges();
            _toastNoftification.AddSuccessToastMessage("Movie updated Successfully ");
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return BadRequest();
            var movie = await _applicationDbContext.Movies.Include(m => m.Genre).SingleOrDefaultAsync(m => m.Id == id);
            if (movie == null)
                return NotFound();
            return View(movie);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if(id == null)
                return BadRequest();
            var movie = await _applicationDbContext.Movies.FindAsync(id);
            if (movie == null)
                return NotFound();
            _applicationDbContext.Movies.Remove(movie);
            _applicationDbContext.SaveChanges();
            return Ok();
        }
        private async Task<IActionResult> ValidatePosterAsync( IFormFile poster,MovieFromViewModel model)
        {
            //var file = Request.Form.Files;
            //var poster = file.FirstOrDefault();
            if (!_allowedExtentions.Contains(Path.GetExtension(poster.FileName).ToLower()))
            {
                model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Only .PNG .JPG images are allowed");
                return View("MovieForm", model);
            }

            if (poster.Length > _maxLAllowedPosterSize)
            {
                model.Genres = await _applicationDbContext.Genres.OrderBy(m => m.Name).ToListAsync();
                ModelState.AddModelError("Poster", "Poster can not be more than 1 MB");
                return View("MovieForm", model);
            }

            return null; // Valid
        }

    }

}
