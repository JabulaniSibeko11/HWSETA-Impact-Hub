using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <inheritdoc />
    public partial class RedesignedProgrammeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Employers_EmployerId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Programmes_ProgrammeId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Enrollments_Providers_ProviderId",
                table: "Enrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Beneficiaries_BeneficiaryId",
                table: "Programmes");

            migrationBuilder.DropForeignKey(
                name: "FK_Programmes_Cohorts_CohortId1",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_BeneficiaryId",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Programmes_CohortId1",
                table: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_EmployerId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ProgrammeId",
                table: "Enrollments");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_ProviderId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ActualEndDate",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "BeneficiaryId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "CohortId",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "CohortId1",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "CohortYear",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "CurrentStatus",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "TargetBeneficiaries",
                table: "Programmes");

            migrationBuilder.DropColumn(
                name: "EmployerId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProgrammeId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Enrollments");

            migrationBuilder.AlterColumn<int>(
                name: "DurationMonths",
                table: "Programmes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "CreatedByUserId",
                table: "Programmes",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DurationMonths",
                table: "Programmes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Programmes",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualEndDate",
                table: "Programmes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BeneficiaryId",
                table: "Programmes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CohortId",
                table: "Programmes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CohortId1",
                table: "Programmes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "CohortYear",
                table: "Programmes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentStatus",
                table: "Programmes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "Programmes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Programmes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Programmes",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TargetBeneficiaries",
                table: "Programmes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "EmployerId",
                table: "Enrollments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProgrammeId",
                table: "Enrollments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "ProviderId",
                table: "Enrollments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_BeneficiaryId",
                table: "Programmes",
                column: "BeneficiaryId");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_CohortId1",
                table: "Programmes",
                column: "CohortId1");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EmployerId",
                table: "Enrollments",
                column: "EmployerId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ProgrammeId",
                table: "Enrollments",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ProviderId",
                table: "Enrollments",
                column: "ProviderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Employers_EmployerId",
                table: "Enrollments",
                column: "EmployerId",
                principalTable: "Employers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Programmes_ProgrammeId",
                table: "Enrollments",
                column: "ProgrammeId",
                principalTable: "Programmes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Enrollments_Providers_ProviderId",
                table: "Enrollments",
                column: "ProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Beneficiaries_BeneficiaryId",
                table: "Programmes",
                column: "BeneficiaryId",
                principalTable: "Beneficiaries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Programmes_Cohorts_CohortId1",
                table: "Programmes",
                column: "CohortId1",
                principalTable: "Cohorts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
