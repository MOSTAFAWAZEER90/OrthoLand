using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrthoFlowAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelWithEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "reward",
                table: "referrals",
                newName: "referrer_reward");

            migrationBuilder.RenameColumn(
                name: "discount",
                table: "referrals",
                newName: "referrer_discount");

            migrationBuilder.AddColumn<bool>(
                name: "CancelAtPeriodEnd",
                table: "subscriptions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripePriceId",
                table: "subscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeSubscriptionId",
                table: "subscriptions",
                type: "text",
                nullable: true);

            // The model now requires these two columns. Back-fill existing NULLs first so
            // SET NOT NULL succeeds and every status parses as a ReferralStatus value.
            migrationBuilder.Sql("UPDATE referrals SET status = 'Pending' WHERE status IS NULL OR status = '';");
            migrationBuilder.Sql("UPDATE referrals SET referral_code = upper(substr(md5(id::text), 1, 10)) WHERE referral_code IS NULL OR referral_code = '';");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "referrals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "referral_code",
                table: "referrals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "referrals",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "referrals",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "referred_discount",
                table: "referrals",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "DeviceToken",
                table: "patients",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityId",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedEntityType",
                table: "notifications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppointmentId",
                table: "feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAnonymous",
                table: "feedbacks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NPSComment",
                table: "feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sentiment",
                table: "feedbacks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "feedbacks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureData",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureImageUrl",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedById",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadedById",
                table: "documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder1DaySent",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder2HoursSent",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder3DaysSent",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Reminder7DaysSent",
                table: "appointments",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancelAtPeriodEnd",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StripePriceId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "StripeSubscriptionId",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "referrals");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "referrals");

            migrationBuilder.DropColumn(
                name: "referred_discount",
                table: "referrals");

            migrationBuilder.DropColumn(
                name: "DeviceToken",
                table: "patients");

            migrationBuilder.DropColumn(
                name: "RelatedEntityId",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "RelatedEntityType",
                table: "notifications");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "IsAnonymous",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "NPSComment",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "Sentiment",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "SignatureData",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "SignatureImageUrl",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "SignedById",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "UploadedById",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "Reminder1DaySent",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Reminder2HoursSent",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Reminder3DaysSent",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Reminder7DaysSent",
                table: "appointments");

            migrationBuilder.RenameColumn(
                name: "referrer_reward",
                table: "referrals",
                newName: "reward");

            migrationBuilder.RenameColumn(
                name: "referrer_discount",
                table: "referrals",
                newName: "discount");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "referrals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "referral_code",
                table: "referrals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
