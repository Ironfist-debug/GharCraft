using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GharCraft.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhoneOtpRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhoneOtpRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    OtpCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOtpRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOtpRecords_ExpiresAt",
                table: "PhoneOtpRecords",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOtpRecords_PhoneNumber",
                table: "PhoneOtpRecords",
                column: "PhoneNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneOtpRecords");
        }
    }
}
