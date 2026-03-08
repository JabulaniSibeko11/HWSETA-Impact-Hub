using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HWSETA_Impact_Hub.Migrations
{
    /// <inheritdoc />
    public partial class addColumnsTobeneficiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_AdminChatProfiles_AdminChatProfileId",
                table: "ConversationMessages");

            migrationBuilder.AddColumn<Guid>(
                name: "BeneficiaryFormInviteId",
                table: "ConversationMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FormPublishId",
                table: "ConversationMessages",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFormShareMessage",
                table: "ConversationMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryOnUtc",
                table: "BeneficiaryFormInvites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentOnUtc",
                table: "BeneficiaryFormInvites",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BeneficiaryFormInvites",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_BeneficiaryFormInviteId",
                table: "ConversationMessages",
                column: "BeneficiaryFormInviteId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMessages_FormPublishId",
                table: "ConversationMessages",
                column: "FormPublishId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_AdminChatProfiles_AdminChatProfileId",
                table: "ConversationMessages",
                column: "AdminChatProfileId",
                principalTable: "AdminChatProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_BeneficiaryFormInvites_BeneficiaryFormInviteId",
                table: "ConversationMessages",
                column: "BeneficiaryFormInviteId",
                principalTable: "BeneficiaryFormInvites",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_FormPublishes_FormPublishId",
                table: "ConversationMessages",
                column: "FormPublishId",
                principalTable: "FormPublishes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_AdminChatProfiles_AdminChatProfileId",
                table: "ConversationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_BeneficiaryFormInvites_BeneficiaryFormInviteId",
                table: "ConversationMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMessages_FormPublishes_FormPublishId",
                table: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_BeneficiaryFormInviteId",
                table: "ConversationMessages");

            migrationBuilder.DropIndex(
                name: "IX_ConversationMessages_FormPublishId",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "BeneficiaryFormInviteId",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "FormPublishId",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "IsFormShareMessage",
                table: "ConversationMessages");

            migrationBuilder.DropColumn(
                name: "ExpiryOnUtc",
                table: "BeneficiaryFormInvites");

            migrationBuilder.DropColumn(
                name: "SentOnUtc",
                table: "BeneficiaryFormInvites");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BeneficiaryFormInvites");

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMessages_AdminChatProfiles_AdminChatProfileId",
                table: "ConversationMessages",
                column: "AdminChatProfileId",
                principalTable: "AdminChatProfiles",
                principalColumn: "Id");
        }
    }
}
