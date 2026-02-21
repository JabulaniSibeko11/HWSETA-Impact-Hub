using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreTableTODB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "FormSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "FormSubmissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultipleSubmissions",
                table: "FormPublishes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CloseAtUtc",
                table: "FormPublishes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsPublished",
                table: "FormPublishes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxSubmissions",
                table: "FormPublishes",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "FormSubmissions");

            migrationBuilder.DropColumn(
                name: "AllowMultipleSubmissions",
                table: "FormPublishes");

            migrationBuilder.DropColumn(
                name: "CloseAtUtc",
                table: "FormPublishes");

            migrationBuilder.DropColumn(
                name: "IsPublished",
                table: "FormPublishes");

            migrationBuilder.DropColumn(
                name: "MaxSubmissions",
                table: "FormPublishes");
        }
    }
}
