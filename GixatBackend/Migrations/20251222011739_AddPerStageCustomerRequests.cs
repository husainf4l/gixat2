using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    internal sealed partial class AddPerStageCustomerRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InspectionRequests",
                table: "GarageSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntakeRequests",
                table: "GarageSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestDriveRequests",
                table: "GarageSessions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InspectionRequests",
                table: "GarageSessions");

            migrationBuilder.DropColumn(
                name: "IntakeRequests",
                table: "GarageSessions");

            migrationBuilder.DropColumn(
                name: "TestDriveRequests",
                table: "GarageSessions");
        }
    }
}
