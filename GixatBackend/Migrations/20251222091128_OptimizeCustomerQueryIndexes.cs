using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeCustomerQueryIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobCards_CustomerId",
                table: "JobCards");

            migrationBuilder.DropIndex(
                name: "IX_GarageSessions_CustomerId",
                table: "GarageSessions");

            migrationBuilder.CreateIndex(
                name: "IX_JobCards_CustomerId_Status",
                table: "JobCards",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_GarageSessions_CustomerId_CreatedAt",
                table: "GarageSessions",
                columns: new[] { "CustomerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_JobCards_CustomerId_Status",
                table: "JobCards");

            migrationBuilder.DropIndex(
                name: "IX_GarageSessions_CustomerId_CreatedAt",
                table: "GarageSessions");

            migrationBuilder.CreateIndex(
                name: "IX_JobCards_CustomerId",
                table: "JobCards",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_GarageSessions_CustomerId",
                table: "GarageSessions",
                column: "CustomerId");
        }
    }
}
