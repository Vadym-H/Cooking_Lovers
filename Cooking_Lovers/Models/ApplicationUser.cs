using Microsoft.AspNetCore.Identity;

namespace Cooking_Lovers.Models
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsBanned { get; set; } = false;
    }
}
