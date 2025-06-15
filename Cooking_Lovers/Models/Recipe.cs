using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cooking_Lovers.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        [Required]
        public required string Title { get; set; }
        [Required]
        public required string Description { get; set; }
        [Required]
        public TimeSpan PreparationTime { get; set; }
        public DateTime PostedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; }
        public bool IsApproved { get; set; } = false;

        [Required]
        public required string UserId { get; set; }

        public required IdentityUser User { get; set; }


        public required ICollection<RecipeIngredient> RecipeIngredients { get; set; }

        public ICollection<UserActions> UserActions { get; set; } = new List<UserActions>();
    }
}
