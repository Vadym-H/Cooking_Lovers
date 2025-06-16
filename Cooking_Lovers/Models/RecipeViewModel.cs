namespace Cooking_Lovers.Models
{
    public class IngredientViewModel
    {
        public string Name { get; set; }
        public string Quantity { get; set; }
        public string Unit { get; set; }
    }

    public class RecipeViewModel
    {
        public int? RecipeId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public TimeSpan PreparationTime { get; set; }
        public List<IngredientViewModel> Ingredients { get; set; }
    }
}
