using Microsoft.EntityFrameworkCore.Migrations;

namespace HWSETA_Impact_Hub.Migrations
{
    public partial class AddSystemFlagsToFormTemplate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "FormTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeletable",
                table: "FormTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEditable",
                table: "FormTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "IsSystem", table: "FormTemplates");
            migrationBuilder.DropColumn(name: "IsDeletable", table: "FormTemplates");
            migrationBuilder.DropColumn(name: "IsEditable", table: "FormTemplates");
        }
    }
}