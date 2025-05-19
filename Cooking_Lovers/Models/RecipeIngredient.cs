using System.ComponentModel.DataAnnotations;

namespace Cooking_Lovers.Models
{
    public class RecipeIngredient
    {
        public int Id { get; set; }
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; }
        public int IngredientId { get; set; }
        public Ingredient Ingredient { get; set; }
        [Required]
        public string Quantity { get; set; }
        public string Unit { get; set; }
    }
}
