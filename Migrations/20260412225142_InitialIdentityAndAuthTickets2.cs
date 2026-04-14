using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasisBank.Identity.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityAndAuthTickets2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceTokenId",
                table: "AuthTickets",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceTokenId",
                table: "AuthTickets");
        }
    }
}
