using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasisBank.Identity.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityAndAuthTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "AuthTickets");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AuthTickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptsCount",
                table: "AuthTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AuthTickets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "HashedOtp",
                table: "AuthTickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AuthTickets_UserId",
                table: "AuthTickets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuthTickets_UserId",
                table: "AuthTickets");

            migrationBuilder.DropColumn(
                name: "AttemptsCount",
                table: "AuthTickets");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AuthTickets");

            migrationBuilder.DropColumn(
                name: "HashedOtp",
                table: "AuthTickets");

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "AuthTickets",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "AuthTickets",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);
        }
    }
}
