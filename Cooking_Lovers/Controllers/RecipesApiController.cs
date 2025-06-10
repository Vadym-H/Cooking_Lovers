using Cooking_Lovers.Data;
using Cooking_Lovers.Models;
using Cooking_Lovers.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cooking_Lovers.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecipesApiController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _db;

        public RecipesApiController(UserManager<IdentityUser> userManager, ApplicationDbContext db)
        {
            _db = db;
            _userManager = userManager;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateRecipe([FromBody] RecipeDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _userManager.GetUserId(User);
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
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound(new { message = "Recipe not found." });

            // todo implement like logic

            await _db.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPatch("save-recipe/{id}")]
        public async Task<IActionResult> SaveRecipe(int id)
        {
            var userId = _userManager.GetUserId(User);
            var recipe = await _db.Recipes
                .Include(r => r.RecipeIngredients)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recipe == null)
                return NotFound(new { message = "Recipe not found." });

            // todo implement save logic

            await _db.SaveChangesAsync();

            return Ok();
        }
    }
}
