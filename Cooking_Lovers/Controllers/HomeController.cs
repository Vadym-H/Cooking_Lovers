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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _logger = logger;
            _userManager = userManager;
            _db = db;
        }

        [AllowAnonymous]
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
        public async Task<IActionResult> CreateRecipe(RecipeDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

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

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet("get-my-recipes")]
        public async Task<IActionResult> GetMyRecipes()
        {

            var userId = _userManager.GetUserId(User);
            var myRecipes = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var viewModels = Helper.MapRecipesToViewModels(myRecipes);
            return View(viewModels);
        }

        [Authorize]
        [HttpPatch("save-recipe/{id}")]
        public async Task<IActionResult> SaveRecipes(int id)
        {
            var userId = _userManager.GetUserId(User);

            var recipe = await _db.Recipes
                .Include(r => r.UserActions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound(new { message = "Recipe not found." });

            var userAction = recipe.UserActions.FirstOrDefault(ua => ua.UserId == userId);

            if (userAction == null)
            {
                userAction = new UserActions
                {
                    UserId = userId,
                    RecipeId = id,
                    HasSaved = true
                };

                _db.UserActions.Add(userAction);
            }
            else
            {
                userAction.HasSaved = !userAction.HasSaved;
                _db.UserActions.Update(userAction);
            }

            await _db.SaveChangesAsync();

            return Ok(new { saved = userAction.HasSaved });
        }

        [Authorize]
        [HttpGet("get-saved-recipes")]
        public async Task<IActionResult> GetSavedRecipes()
        {
            var userId = _userManager.GetUserId(User);
            var savedRecipeIds = await _db.UserActions
                .Where(ua => ua.UserId == userId && ua.HasSaved)
                .Select(ua => ua.RecipeId)
                .ToListAsync();

            var recipes = await _db.Recipes
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .Where(r => savedRecipeIds.Contains(r.Id))
                .ToListAsync();

            var viewModels = Helper.MapRecipesToViewModels(recipes);
            return View(viewModels);
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

