using System.ComponentModel.DataAnnotations;

namespace Cooking_Lovers.Models
{
    public class RecipeDto
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public TimeSpan PreparationTime { get; set; }

        [Required]
        public List<IngredientViewModel> Ingredients { get; set; }
    }
}