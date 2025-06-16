using Cooking_Lovers.consts;
using Cooking_Lovers.Data;
using Cooking_Lovers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cooking_Lovers.Data
{

    namespace Cooking_Lovers.Data
    {
        public class UserSeeder
        {
            public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
            {
                var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                await CreateUserWithRole(userManager, "admin@gmail.com", "Admin123!", Roles.Admin);
                await CreateUserWithRole(userManager, "user1@gmail.com", "User123!", Roles.Admin);
                await CreateUserWithRole(userManager, "user2@gmail.com", "User123!", Roles.Admin);
                await CreateUserWithRole(userManager, "user3@gmail.com", "User123!", Roles.User);
            }

            private static async Task CreateUserWithRole(UserManager<ApplicationUser> userManager, string email, string password, string role)
            {
                if (await userManager.FindByEmailAsync(email) == null)
                {
                    var user = new ApplicationUser()
                    {
                        Email = email,
                        EmailConfirmed = false,
                        UserName = email
                    };
                    var result = await userManager.CreateAsync(user, password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, role);
                    }
                    else
                    {
                        throw new Exception($"Failed creating user with email {user.Email}. Errors: {string.Join(",", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }

}
