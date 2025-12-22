using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "EF Core migration")]
    public partial class AddLookupItemIndexes : Migration
    {
        private static readonly string[] CategoryIsActiveParentIdColumns = ["Category", "IsActive", "ParentId"];
        private static readonly string[] ParentIdIsActiveColumns = ["ParentId", "IsActive"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_LookupItems_ParentId",
                table: "LookupItems");

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_Category_IsActive_ParentId",
                table: "LookupItems",
                columns: CategoryIsActiveParentIdColumns);

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_ParentId_IsActive",
                table: "LookupItems",
                columns: ParentIdIsActiveColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropIndex(
                name: "IX_LookupItems_Category_IsActive_ParentId",
                table: "LookupItems");

            migrationBuilder.DropIndex(
                name: "IX_LookupItems_ParentId_IsActive",
                table: "LookupItems");

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_ParentId",
                table: "LookupItems",
                column: "ParentId");
        }
    }
}
