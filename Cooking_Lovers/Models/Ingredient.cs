using System.ComponentModel.DataAnnotations;

namespace Cooking_Lovers.Models
{
    public class Ingredient
    {
        public int Id { get; set; }
        [Required]
        public required string Name { get; set; }

        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
