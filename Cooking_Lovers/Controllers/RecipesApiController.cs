using Cooking_Lovers.Data;
using Cooking_Lovers.Models;
using Cooking_Lovers.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Cooking_Lovers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesApiController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public RecipesApiController(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromBody] RecipeDto model)
        {
            var userId = _userManager.GetUserId(User);
            if (await Helper.IsBanned(_userManager, userId))
            {
                return Forbid();
            }
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            return Ok(new { recipe.Id, message = "Recipe created successfully." });
        }

        [HttpGet]
        public async Task<IActionResult> GetAllRecipes([FromQuery] string? search, [FromQuery] string? sort)
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
            return Ok(viewModels);
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
            return Ok(viewModels);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromBody] RecipeDto model)
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
            return Ok(updatedModel);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRcipe(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (await Helper.IsBanned(_userManager, userId))
            {
                return Forbid();
            }

            var recipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound(new { message = "Recipe not found." });

            if (recipe.UserId != userId)
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to delete this recipe." });

            _db.RecipeIngredients.RemoveRange(recipe.RecipeIngredients);
            _db.Recipes.Remove(recipe);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Recipe deleted successfully." });
        }


        [Authorize]
        [HttpPatch("like-recipe/{id}")]
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

            return Ok(new { liked = userAction.HasLiked });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("admin-update/{id}")]
        public async Task<IActionResult> AdminUpdateRecipe(int id, [FromBody] RecipeDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var recipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);

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
            return Ok(updatedModel);
        }

        [Authorize]
        [HttpPatch("save-recipe/{id}")]
        public async Task<IActionResult> SaveRecipe(int id)
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

        [Authorize(Roles = "Admin")]
        [HttpPatch("ban-status-by-email")]
        public async Task<IActionResult> ChangeUserBanStatusByEmail([FromQuery] string email, [FromQuery] bool isBanned)
        {
            if (string.IsNullOrWhiteSpace(email))
                return BadRequest(new { message = "Email is required." });

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
                return NotFound(new { message = "User not found." });

            if (user.Id == _userManager.GetUserId(User))
                return BadRequest(new { message = "Admin can't ban himself." });

            if (user.IsBanned == isBanned)
            {
                return BadRequest(new { message = $"User is already {(isBanned ? "banned" : "unbanned")}." });
            }

            user.IsBanned = isBanned;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return StatusCode(500, new { message = "Failed to update ban status.", errors = result.Errors });

            return Ok(new { message = $"User has been {(isBanned ? "banned" : "unbanned")}." });
        }
    }
}
