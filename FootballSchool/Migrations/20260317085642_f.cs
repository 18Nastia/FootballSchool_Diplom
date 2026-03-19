using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballSchool.Migrations
{
    /// <inheritdoc />
    public partial class f : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Branch_ID",
                table: "Team",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.CreateIndex(
                name: "IX_Team_Branch_ID",
                table: "Team",
                column: "Branch_ID");

            migrationBuilder.AddForeignKey(
                name: "FK_Team_Branch_Branch_ID",
                table: "Team",
                column: "Branch_ID",
                principalTable: "Branch",
                principalColumn: "Branch_ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Team_Branch_Branch_ID",
                table: "Team");

            migrationBuilder.DropIndex(
                name: "IX_Team_Branch_ID",
                table: "Team");

            migrationBuilder.DropColumn(
                name: "Branch_ID",
                table: "Team");
        }
    }
}
