using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreCol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "OpenFromUtc",
                table: "FormTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpenToUtc",
                table: "FormTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicToken",
                table: "FormTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "FormTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UnpublishedAt",
                table: "FormTemplates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Employer",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Programme",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrainingProvider",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenFromUtc",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "OpenToUtc",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "PublicToken",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "UnpublishedAt",
                table: "FormTemplates");

            migrationBuilder.DropColumn(
                name: "Employer",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "Programme",
                table: "Beneficiaries");

            migrationBuilder.DropColumn(
                name: "TrainingProvider",
                table: "Beneficiaries");
        }
    }
}
