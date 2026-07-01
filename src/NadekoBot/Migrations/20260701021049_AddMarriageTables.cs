using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    /// <inheritdoc />
    public partial class AddMarriageTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Interval",
                table: "Repeaters",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(TimeSpan),
                oldType: "TEXT");

            migrationBuilder.CreateTable(
                name: "AdoptionProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ProposerId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdoptionProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Adoptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Parent1Id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Parent2Id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    AdoptedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adoptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarriageProposals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ProposerId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarriageProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Marriages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    User1 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    User2 = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MarriedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProposedBy = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marriages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdoptionProposals_TargetId",
                table: "AdoptionProposals",
                column: "TargetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Adoptions_Parent1Id",
                table: "Adoptions",
                column: "Parent1Id");

            migrationBuilder.CreateIndex(
                name: "IX_Adoptions_UserId",
                table: "Adoptions",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarriageProposals_TargetId",
                table: "MarriageProposals",
                column: "TargetId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Marriages_User1",
                table: "Marriages",
                column: "User1");

            migrationBuilder.CreateIndex(
                name: "IX_Marriages_User2",
                table: "Marriages",
                column: "User2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdoptionProposals");

            migrationBuilder.DropTable(
                name: "Adoptions");

            migrationBuilder.DropTable(
                name: "MarriageProposals");

            migrationBuilder.DropTable(
                name: "Marriages");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "Interval",
                table: "Repeaters",
                type: "TEXT",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0),
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
