using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <summary>
    /// MIGRATION 1 OF 2 — Prepares columns for AES-256-CBC at-rest encryption.
    ///
    /// APPLY ORDER:
    ///   Step 1 → dotnet ef database update AddBeneficiaryEncryption   (this file)
    ///   Step 2 → Run Tools/BackfillEncryption.cs to encrypt existing rows + populate hashes
    ///   Step 3 → dotnet ef database update AddBeneficiaryEncryptionIndex (second migration)
    ///
    /// What this migration does:
    ///   1. Adds IdentifierValueHash nvarchar(64) NULL  — blind index, populated by backfill
    ///   2. Adds EmailHash nvarchar(64) NULL
    ///   3. Drops old unique index on (IdentifierType, IdentifierValue)
    ///   4. Widens encrypted columns to nvarchar(max)
    ///
    /// NOTE: The new unique index is intentionally NOT created here.
    ///       It is created in AddBeneficiaryEncryptionIndex AFTER all rows are backfilled,
    ///       so SQL Server never sees duplicate empty-string hash values.
    /// </summary>
    public partial class AddBeneficiaryEncryption : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── 1. Add blind-index columns (nullable — backfill populates them) ──
            migrationBuilder.AddColumn<string>(
                name: "IdentifierValueHash",
                table: "Beneficiaries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,          // NULL until backfill runs
                defaultValue: null);

            migrationBuilder.AddColumn<string>(
                name: "EmailHash",
                table: "Beneficiaries",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            // ── 2. Drop old unique index (on plaintext IdentifierValue) ──────────
            migrationBuilder.DropIndex(
                name: "IX_Beneficiaries_IdentifierType_IdentifierValue",
                table: "Beneficiaries");

            // ── 3. Widen encrypted columns to nvarchar(max) ──────────────────────
            //    base64(16-byte IV + ciphertext) is always longer than plaintext.
            migrationBuilder.AlterColumn<string>(
                name: "IdentifierValue",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(80)",
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "FirstName",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "LastName",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MobileNumber",
                table: "Beneficiaries",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore encrypted column widths
            migrationBuilder.AlterColumn<string>(
                name: "IdentifierValue", table: "Beneficiaries",
                type: "nvarchar(80)", maxLength: 80, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "FirstName", table: "Beneficiaries",
                type: "nvarchar(120)", maxLength: 120, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "LastName", table: "Beneficiaries",
                type: "nvarchar(120)", maxLength: 120, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email", table: "Beneficiaries",
                type: "nvarchar(256)", maxLength: 256, nullable: true,
                oldClrType: typeof(string), oldType: "nvarchar(max)", oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MobileNumber", table: "Beneficiaries",
                type: "nvarchar(30)", maxLength: 30, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(max)");

            // Restore old unique index on plaintext column
            migrationBuilder.CreateIndex(
                name: "IX_Beneficiaries_IdentifierType_IdentifierValue",
                table: "Beneficiaries",
                columns: new[] { "IdentifierType", "IdentifierValue" },
                unique: true);

            // Remove added columns
            migrationBuilder.DropColumn(name: "IdentifierValueHash", table: "Beneficiaries");
            migrationBuilder.DropColumn(name: "EmailHash", table: "Beneficiaries");
        }
    }
}