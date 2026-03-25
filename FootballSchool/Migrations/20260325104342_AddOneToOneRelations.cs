using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballSchool.Migrations
{
    /// <inheritdoc />
    public partial class AddOneToOneRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Student_User_ID",
                table: "Student");

            migrationBuilder.AlterColumn<string>(
                name: "Photo_student",
                table: "Student",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Photo_coach",
                table: "Coach",
                type: "varchar(255)",
                unicode: false,
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "User_ID",
                table: "Coach",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_User_ID",
                table: "Student",
                column: "User_ID",
                unique: true,
                filter: "[User_ID] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Coach_User_ID",
                table: "Coach",
                column: "User_ID",
                unique: true,
                filter: "[User_ID] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Coach_User",
                table: "Coach",
                column: "User_ID",
                principalTable: "User",
                principalColumn: "User_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coach_User",
                table: "Coach");

            migrationBuilder.DropIndex(
                name: "IX_Student_User_ID",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Coach_User_ID",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "Photo_coach",
                table: "Coach");

            migrationBuilder.DropColumn(
                name: "User_ID",
                table: "Coach");

            migrationBuilder.AlterColumn<string>(
                name: "Photo_student",
                table: "Student",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldUnicode: false,
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_User_ID",
                table: "Student",
                column: "User_ID");
        }
    }
}
