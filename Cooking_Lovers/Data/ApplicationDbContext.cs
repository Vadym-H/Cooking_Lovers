using Cooking_Lovers.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Cooking_Lovers.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }

        public DbSet<UserActions> UserActions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<RecipeIngredient>()
             .HasOne(ri => ri.Recipe)
             .WithMany(r => r.RecipeIngredients)
             .HasForeignKey(ri => ri.RecipeId);

            builder.Entity<RecipeIngredient>()
            .HasOne(ri => ri.Ingredient)
            .WithMany(i => i.RecipeIngredients)
            .HasForeignKey(ri => ri.IngredientId);

            builder.Entity<UserActions>()
            .HasIndex(ua => new { ua.UserId, ua.RecipeId })
            .IsUnique();


            builder.Entity<UserActions>()
            .HasOne(ua => ua.Recipe)
            .WithMany()
            .HasForeignKey(ua => ua.RecipeId)
            .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserActions>()
             .HasOne(ua => ua.User)
             .WithMany()
             .HasForeignKey(ua => ua.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        }
    }
}