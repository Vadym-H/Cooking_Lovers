using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cooking_Lovers.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserActionsRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RecipeId1",
                table: "UserActions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActions_RecipeId1",
                table: "UserActions",
                column: "RecipeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserActions_Recipes_RecipeId1",
                table: "UserActions",
                column: "RecipeId1",
                principalTable: "Recipes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserActions_Recipes_RecipeId1",
                table: "UserActions");

            migrationBuilder.DropIndex(
                name: "IX_UserActions_RecipeId1",
                table: "UserActions");

            migrationBuilder.DropColumn(
                name: "RecipeId1",
                table: "UserActions");
        }
    }
}
