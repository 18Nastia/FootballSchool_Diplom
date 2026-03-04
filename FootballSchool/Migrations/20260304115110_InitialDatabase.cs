using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FootballSchool.Migrations
{
    /// <inheritdoc />
    public partial class InitialDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Branch",
                columns: table => new
                {
                    Branch_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name_branch = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    City_branch = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Street_branch = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    House_branch = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Phone_branch = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Photo_branch = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Branch__12CEB04107BE5E5D", x => x.Branch_ID);
                });

            migrationBuilder.CreateTable(
                name: "Coach",
                columns: table => new
                {
                    Coach_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Surname_coach = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name_coach = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Middle_coach = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Qualification_coach = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Specialty_coach = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Schedule_coach = table.Column<string>(type: "text", nullable: true),
                    Salary_coach = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Coach__BDB636274675589F", x => x.Coach_ID);
                });

            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Team_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category_team = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Status_team = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Team__02215C0AA6728748", x => x.Team_ID);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    User_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Login = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Parent")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.User_ID);
                });

            migrationBuilder.CreateTable(
                name: "Facility",
                columns: table => new
                {
                    Facility_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Branch_ID = table.Column<int>(type: "int", nullable: false),
                    Name_facility = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Type_facility = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Capacity_facility = table.Column<int>(type: "int", nullable: false),
                    Status_facility = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Cost_facility = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    Number_facility = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Facility__CEAA23C5BD2B8BF4", x => x.Facility_ID);
                    table.ForeignKey(
                        name: "FK_Facility_Branch",
                        column: x => x.Branch_ID,
                        principalTable: "Branch",
                        principalColumn: "Branch_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    Student_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Team_ID = table.Column<int>(type: "int", nullable: true),
                    User_ID = table.Column<int>(type: "int", nullable: true),
                    Surname_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Middle_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Birth_student = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender_student = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    Phone_student = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    Email_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Medical_student = table.Column<string>(type: "text", nullable: true),
                    Level_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Photo_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Parent_number = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Surname_parent = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Name_parent = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Middle_parent = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    City_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Street_student = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    House_student = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    Apartment_student = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student__A2F4E9AC4021C8FB", x => x.Student_ID);
                    table.ForeignKey(
                        name: "FK_Student_Team",
                        column: x => x.Team_ID,
                        principalTable: "Team",
                        principalColumn: "Team_ID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Student_User",
                        column: x => x.User_ID,
                        principalTable: "User",
                        principalColumn: "User_ID");
                });

            migrationBuilder.CreateTable(
                name: "Training",
                columns: table => new
                {
                    Training_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Facility_ID = table.Column<int>(type: "int", nullable: false),
                    Team_ID = table.Column<int>(type: "int", nullable: false),
                    Coach_ID = table.Column<int>(type: "int", nullable: false),
                    Date_training = table.Column<DateOnly>(type: "date", nullable: false),
                    Time_training = table.Column<TimeOnly>(type: "time", nullable: false),
                    Plan_training = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Training__EF9C38168C9E7A2D", x => x.Training_ID);
                    table.ForeignKey(
                        name: "FK_Training_Coach",
                        column: x => x.Coach_ID,
                        principalTable: "Coach",
                        principalColumn: "Coach_ID");
                    table.ForeignKey(
                        name: "FK_Training_Facility",
                        column: x => x.Facility_ID,
                        principalTable: "Facility",
                        principalColumn: "Facility_ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Training_Team",
                        column: x => x.Team_ID,
                        principalTable: "Team",
                        principalColumn: "Team_ID");
                });

            migrationBuilder.CreateTable(
                name: "Progress",
                columns: table => new
                {
                    Progress_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Date_progress = table.Column<DateOnly>(type: "date", nullable: false),
                    Tests_progress = table.Column<string>(type: "text", nullable: true),
                    Physical_progress = table.Column<string>(type: "text", nullable: true),
                    Plan_progress = table.Column<string>(type: "text", nullable: true),
                    Comment_progress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Progress__D558799A376577B4", x => x.Progress_ID);
                    table.ForeignKey(
                        name: "FK_Progress_Student",
                        column: x => x.Student_ID,
                        principalTable: "Student",
                        principalColumn: "Student_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscription",
                columns: table => new
                {
                    Subscription_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Type_subscription = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Terms_subscription = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    Days_subscription = table.Column<int>(type: "int", nullable: true),
                    Cost_subscription = table.Column<decimal>(type: "decimal(10,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Subscrip__518059B16793CE27", x => x.Subscription_ID);
                    table.ForeignKey(
                        name: "FK_Subscription_Student",
                        column: x => x.Student_ID,
                        principalTable: "Student",
                        principalColumn: "Student_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    Attendance_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Training_ID = table.Column<int>(type: "int", nullable: false),
                    Student_ID = table.Column<int>(type: "int", nullable: false),
                    Status_attendance = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Note_attendance = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__57FA4934437C3D8E", x => x.Attendance_ID);
                    table.ForeignKey(
                        name: "FK_Attendance_Student",
                        column: x => x.Student_ID,
                        principalTable: "Student",
                        principalColumn: "Student_ID");
                    table.ForeignKey(
                        name: "FK_Attendance_Training",
                        column: x => x.Training_ID,
                        principalTable: "Training",
                        principalColumn: "Training_ID");
                });

            migrationBuilder.CreateTable(
                name: "Payment",
                columns: table => new
                {
                    Payment_ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subscription_ID = table.Column<int>(type: "int", nullable: false),
                    Amount_payment = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Date_payment = table.Column<DateOnly>(type: "date", nullable: false),
                    Method_payment = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Status_payment = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Payment__DA6C7FE17E85319F", x => x.Payment_ID);
                    table.ForeignKey(
                        name: "FK_Payment_Subscription",
                        column: x => x.Subscription_ID,
                        principalTable: "Subscription",
                        principalColumn: "Subscription_ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Student_ID",
                table: "Attendance",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Training_ID",
                table: "Attendance",
                column: "Training_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Facility_Branch_ID",
                table: "Facility",
                column: "Branch_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Payment_Subscription_ID",
                table: "Payment",
                column: "Subscription_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Progress_Student_ID",
                table: "Progress",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Team_ID",
                table: "Student",
                column: "Team_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Student_User_ID",
                table: "Student",
                column: "User_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Subscription_Student_ID",
                table: "Subscription",
                column: "Student_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Training_Coach_ID",
                table: "Training",
                column: "Coach_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Training_Facility_ID",
                table: "Training",
                column: "Facility_ID");

            migrationBuilder.CreateIndex(
                name: "IX_Training_Team_ID",
                table: "Training",
                column: "Team_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Payment");

            migrationBuilder.DropTable(
                name: "Progress");

            migrationBuilder.DropTable(
                name: "Training");

            migrationBuilder.DropTable(
                name: "Subscription");

            migrationBuilder.DropTable(
                name: "Coach");

            migrationBuilder.DropTable(
                name: "Facility");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Branch");

            migrationBuilder.DropTable(
                name: "Team");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
