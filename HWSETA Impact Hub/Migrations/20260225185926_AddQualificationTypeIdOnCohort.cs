using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <inheritdoc />
    public partial class AddQualificationTypeIdOnCohort : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "QualificationTypeId",
                table: "Cohorts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Cohorts_QualificationTypeId",
                table: "Cohorts",
                column: "QualificationTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cohorts_Lookups_QualificationTypeId",
                table: "Cohorts",
                column: "QualificationTypeId",
                principalTable: "Lookups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cohorts_Lookups_QualificationTypeId",
                table: "Cohorts");

            migrationBuilder.DropIndex(
                name: "IX_Cohorts_QualificationTypeId",
                table: "Cohorts");

            migrationBuilder.DropColumn(
                name: "QualificationTypeId",
                table: "Cohorts");
        }
    }
}
