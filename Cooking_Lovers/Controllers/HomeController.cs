using System.Diagnostics;
using Cooking_Lovers.Data;
using Cooking_Lovers.Models;
using Cooking_Lovers.Utilities;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooking_Lovers.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger, UserManager<IdentityUser> userManager, ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db;
        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] string? search, [FromQuery] string? sort)
        {
            var query = _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.Title.ToLower().Contains(search.ToLower()));
            }

            query = sort?.ToLower() switch
            {
                "asc" => query.OrderBy(r => r.PreparationTime),
                "desc" => query.OrderByDescending(r => r.PreparationTime),
                _ => query
            };

            var allRecipes = await query.ToListAsync();

            var viewModels = Helper.MapRecipesToViewModels(allRecipes);
            return View(viewModels);
        }

        public IActionResult CreateRecipe()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromBody] RecipeDto model)
        {
            if (!ModelState.IsValid)
                return View(model);
            //return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);

            var recipe = new Recipe
            {
                Title = model.Title,
                Description = model.Description,
                PreparationTime = model.PreparationTime,
                UserId = userId,
                User = user,
                RecipeIngredients = new List<RecipeIngredient>()
            };

            await Helper.AddIngredientsToRecipe(model, recipe, _db);
            _db.Recipes.Add(recipe);
            await _db.SaveChangesAsync();

            return View(RedirectToAction("Index"));
            //return Ok(new { recipe.Id, message = "Recipe created successfully." });
        }

        public IActionResult GetMyRecipes()
        {
            return View();
        }

        public IActionResult SavedRecipes()
        {
            return View();
        }

        public IActionResult LikedRecipes()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

