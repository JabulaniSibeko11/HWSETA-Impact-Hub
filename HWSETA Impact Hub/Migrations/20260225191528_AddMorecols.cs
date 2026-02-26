using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddMorecols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "BeneficiaryInvites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "BeneficiaryInvites",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "BeneficiaryInvites");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "BeneficiaryInvites");
        }
    }
}
