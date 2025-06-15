using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cooking_Lovers.Models
{
    [Index(nameof(UserId), nameof(RecipeId), IsUnique = true)]
    public class UserActions
    {
        public int Id { get; set; }
        public bool HasLiked { get; set; } = false;
        public bool HasSaved { get; set; } = false;

        [Required]
        public required string UserId { get; set; }

        [Required]
        public required int RecipeId { get; set; }

        [ForeignKey(nameof(UserId))]
        public required IdentityUser User { get; set; }

        [ForeignKey(nameof(RecipeId))]
        public required Recipe Recipe { get; set; }
    }
}
