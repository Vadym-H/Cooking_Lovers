namespace Cooking_Lovers.Models
{
    public class RecipeViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public TimeSpan PreparationTime { get; set; }
        public ICollection<string> Ingaradients { get; set; }
    }
}
