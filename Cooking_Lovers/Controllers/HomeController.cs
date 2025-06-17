using System.Diagnostics;
using System.Threading.Tasks;
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

            var userId = _userManager.GetUserId(User);

            var likedIds = await _db.UserActions
                .Where(a => a.UserId == userId && a.HasLiked)
                .Select(a => a.RecipeId)
                .ToListAsync();

            ViewBag.UserLikedRecipeIds = likedIds;
            //var viewModels = Helper.MapRecipesToViewModels(allRecipes);
            return View(allRecipes); 
        }

        [Authorize]
        [HttpGet("get-create-recipe")]
        public IActionResult CreateRecipe()
        {
            return View();
        }

        [Authorize]
        [HttpPost("create-recipe")]
        public async Task<IActionResult> PostCreateRecipe(RecipeDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Get the current user's ID
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

            //var viewModels = Helper.MapRecipesToViewModels(myRecipes);
            return View(myRecipes);
        }

        [Authorize]
        [HttpPost("save-recipe/{id}")]
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

            return RedirectToAction("Index");
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

        [Authorize(Roles = "Admin")]
        [HttpGet("ban-users")]
        public async Task<IActionResult> BanUsers()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [Authorize(Roles = "Admin")]
        [IgnoreAntiforgeryToken]
        [HttpPatch("ban-status-by-email")]
        public async Task<IActionResult> PatchBanUser([FromQuery] string email, [FromQuery] bool isBanned)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required." });

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Id == _userManager.GetUserId(User))
                return BadRequest(new { message = "Admin can't ban themselves." });

            if (user.IsBanned == isBanned)
            {
                return Ok(new { message = $"User is already {(isBanned ? "banned" : "unbanned")}." });
            }

            user.IsBanned = isBanned;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return StatusCode(500, new { message = "Failed to update ban status.", errors = result.Errors });

            return Ok(new
            {
                message = $"User has been {(isBanned ? "banned" : "unbanned")} successfully.",
                isBanned = user.IsBanned
            });
        }

        [Authorize]
        [HttpPost("delete-recipe")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (await Helper.IsBanned(_userManager, userId))
            {
                return Forbid();
            }

            var recipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .Include(r => r.UserActions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound(new { message = "Recipe not found." });

            if (recipe.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to delete this recipe." });
            _db.UserActions.RemoveRange(recipe.UserActions);
            _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();

            return RedirectToAction("GetMyRecipes");
        }

        [Authorize]
        [HttpGet("get-update-recipe")]
        public IActionResult UpdateRecipe(RecipeViewModel model)
        {
            return View(model);
        }

        [Authorize]
        [HttpPost("{id}")]
        public async Task<IActionResult> PostUpdateRecipe(int id, RecipeDto model)
        {

            var userId = _userManager.GetUserId(User);
            if (await Helper.IsBanned(_userManager, userId))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var recipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (recipe == null)
                return NotFound();

            recipe.Title = model.Title;
            recipe.Description = model.Description;
            recipe.PreparationTime = model.PreparationTime;
            recipe.UpdatedDate = DateTime.UtcNow;

            _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            recipe.RecipeIngredients.Clear();

            await Helper.AddIngredientsToRecipe(model, recipe, _db);
            await _db.SaveChangesAsync();

            var updatedModel = Helper.MapRecipeToViewModel(recipe);
            return View(updatedModel);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin-delete")]
        public async Task<IActionResult> AdminDeleteRecipe(int id)
        {
            var recipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .Include(r => r.UserActions)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound(new { message = "Recipe not found." });

            _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            _db.UserActions.RemoveRange(recipe.UserActions);
            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost("like-recipe/{id}")]
        public async Task<IActionResult> LikeRecipe(int id)
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
                    HasLiked = true
                };

                _db.UserActions.Add(userAction);
            }
            else
            {
                userAction.HasLiked = !userAction.HasLiked;
                _db.UserActions.Update(userAction);
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

