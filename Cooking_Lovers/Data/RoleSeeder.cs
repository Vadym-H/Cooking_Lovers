using Cooking_Lovers.consts;
using Microsoft.AspNetCore.Identity;

namespace Cooking_Lovers.Data
{
    public class RoleSeeder
    {
        public static async Task SeedRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await CreateRole(Roles.Admin, roleManager);
            await CreateRole(Roles.User, roleManager);
        }

        public static async Task CreateRole(string role, RoleManager<IdentityRole> roleManager)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                 await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}
