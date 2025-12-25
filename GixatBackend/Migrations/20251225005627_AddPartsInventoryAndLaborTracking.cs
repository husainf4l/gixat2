using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPartsInventoryAndLaborTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QuantityInStock = table.Column<decimal>(type: "numeric", nullable: false),
                    MinimumStockLevel = table.Column<decimal>(type: "numeric", nullable: false),
                    CostPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    SellingPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryItems_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LaborEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TechnicianId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HoursWorked = table.Column<decimal>(type: "numeric", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric", nullable: false),
                    LaborType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsActual = table.Column<bool>(type: "boolean", nullable: false),
                    IsBillable = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborEntries_AspNetUsers_TechnicianId",
                        column: x => x.TechnicianId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaborEntries_JobItems_JobItemId",
                        column: x => x.JobItemId,
                        principalTable: "JobItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobItemParts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Discount = table.Column<decimal>(type: "numeric", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsActual = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobItemParts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobItemParts_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobItemParts_JobItems_JobItemId",
                        column: x => x.JobItemId,
                        principalTable: "JobItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_OrganizationId_Category_IsActive",
                table: "InventoryItems",
                columns: new[] { "OrganizationId", "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_OrganizationId_IsActive_QuantityInStock",
                table: "InventoryItems",
                columns: new[] { "OrganizationId", "IsActive", "QuantityInStock" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_OrganizationId_PartNumber",
                table: "InventoryItems",
                columns: new[] { "OrganizationId", "PartNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobItemParts_InventoryItemId",
                table: "JobItemParts",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JobItemParts_JobItemId",
                table: "JobItemParts",
                column: "JobItemId");

            migrationBuilder.CreateIndex(
                name: "IX_JobItemParts_JobItemId_IsActual",
                table: "JobItemParts",
                columns: new[] { "JobItemId", "IsActual" });

            migrationBuilder.CreateIndex(
                name: "IX_LaborEntries_JobItemId",
                table: "LaborEntries",
                column: "JobItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborEntries_JobItemId_IsActual",
                table: "LaborEntries",
                columns: new[] { "JobItemId", "IsActual" });

            migrationBuilder.CreateIndex(
                name: "IX_LaborEntries_TechnicianId",
                table: "LaborEntries",
                column: "TechnicianId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborEntries_TechnicianId_StartTime_EndTime",
                table: "LaborEntries",
                columns: new[] { "TechnicianId", "StartTime", "EndTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobItemParts");

            migrationBuilder.DropTable(
                name: "LaborEntries");

            migrationBuilder.DropTable(
                name: "InventoryItems");
        }
    }
}
