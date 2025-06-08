using Cooking_Lovers.Data;
using Cooking_Lovers.Models;
using Microsoft.EntityFrameworkCore;

namespace Cooking_Lovers.Utilities
{
    public static class Helper
    {
        public static List<RecipeViewModel> MapRecipesToViewModels(List<Recipe> allRecipes)
        {
            return allRecipes.Select(r => MapRecipeToViewModel(r)).ToList();
        }

        public static RecipeViewModel MapRecipeToViewModel(Recipe recipe)
        {
            return new RecipeViewModel
            {
                Title = recipe.Title,
                Description = recipe.Description,
                PreparationTime = recipe.PreparationTime,
                Ingredients = recipe.RecipeIngredients?.Select(ri => new IngredientViewModel
                {
                    Name = ri.Ingredient?.Name,
                    Quantity = ri.Quantity,
                    Unit = ri.Unit
                }).ToList() ?? new List<IngredientViewModel>()
            };
        }

        public static async Task AddIngredientsToRecipe(RecipeDto model, Recipe recipe, ApplicationDbContext db)
        {
            foreach (var ingredientVm in model.Ingredients)
            {
                if (string.IsNullOrWhiteSpace(ingredientVm.Name) ||
                    string.IsNullOrWhiteSpace(ingredientVm.Quantity) ||
                    string.IsNullOrWhiteSpace(ingredientVm.Unit))
                {
                    continue;
                }

                var name = ingredientVm.Name.Trim().ToLowerInvariant();
                var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Name.ToLower() == name);

                if (ingredient == null)
                {
                    ingredient = new Ingredient { Name = ingredientVm.Name.Trim() };
                    db.Ingredients.Add(ingredient);
                    await db.SaveChangesAsync();
                }

                recipe.RecipeIngredients.Add(new RecipeIngredient
                {
                    IngredientId = ingredient.Id,
                    Quantity = ingredientVm.Quantity.Trim(),
                    Unit = ingredientVm.Unit.Trim()
                });
            }
        }
    }
}
