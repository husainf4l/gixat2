using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    public partial class DenormalizeCustomerComputedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add denormalized columns
            migrationBuilder.AddColumn<int>(
                name: "ActiveJobCards",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSessionDate",
                table: "Customers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalCars",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalSpent",
                table: "Customers",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TotalVisits",
                table: "Customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Initialize existing customer data
            migrationBuilder.Sql(@"
                UPDATE ""Customers"" c
                SET 
                    ""TotalCars"" = (SELECT COUNT(*) FROM ""Cars"" WHERE ""CustomerId"" = c.""Id""),
                    ""TotalVisits"" = (SELECT COUNT(*) FROM ""GarageSessions"" WHERE ""CustomerId"" = c.""Id""),
                    ""LastSessionDate"" = (SELECT MAX(""CreatedAt"") FROM ""GarageSessions"" WHERE ""CustomerId"" = c.""Id""),
                    ""ActiveJobCards"" = (SELECT COUNT(*) FROM ""JobCards"" WHERE ""CustomerId"" = c.""Id"" AND ""Status"" IN (0, 1)),
                    ""TotalSpent"" = (SELECT COALESCE(SUM(""TotalActualCost""), 0) FROM ""JobCards"" WHERE ""CustomerId"" = c.""Id"" AND ""Status"" = 2);
            ");

            // Trigger function to update customer stats
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_customer_stats()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF TG_TABLE_NAME = 'Cars' THEN
                        IF TG_OP = 'DELETE' THEN
                            UPDATE ""Customers"" 
                            SET ""TotalCars"" = ""TotalCars"" - 1 
                            WHERE ""Id"" = OLD.""CustomerId"";
                            RETURN OLD;
                        ELSE
                            UPDATE ""Customers"" 
                            SET ""TotalCars"" = (SELECT COUNT(*) FROM ""Cars"" WHERE ""CustomerId"" = NEW.""CustomerId"")
                            WHERE ""Id"" = NEW.""CustomerId"";
                            RETURN NEW;
                        END IF;
                    ELSIF TG_TABLE_NAME = 'GarageSessions' THEN
                        IF TG_OP = 'DELETE' THEN
                            UPDATE ""Customers"" 
                            SET 
                                ""TotalVisits"" = ""TotalVisits"" - 1,
                                ""LastSessionDate"" = (SELECT MAX(""CreatedAt"") FROM ""GarageSessions"" WHERE ""CustomerId"" = OLD.""CustomerId"")
                            WHERE ""Id"" = OLD.""CustomerId"";
                            RETURN OLD;
                        ELSE
                            UPDATE ""Customers"" 
                            SET 
                                ""TotalVisits"" = (SELECT COUNT(*) FROM ""GarageSessions"" WHERE ""CustomerId"" = NEW.""CustomerId""),
                                ""LastSessionDate"" = (SELECT MAX(""CreatedAt"") FROM ""GarageSessions"" WHERE ""CustomerId"" = NEW.""CustomerId"")
                            WHERE ""Id"" = NEW.""CustomerId"";
                            RETURN NEW;
                        END IF;
                    ELSIF TG_TABLE_NAME = 'JobCards' THEN
                        IF TG_OP = 'DELETE' THEN
                            UPDATE ""Customers"" 
                            SET 
                                ""ActiveJobCards"" = (SELECT COUNT(*) FROM ""JobCards"" WHERE ""CustomerId"" = OLD.""CustomerId"" AND ""Status"" IN (0, 1)),
                                ""TotalSpent"" = (SELECT COALESCE(SUM(""TotalActualCost""), 0) FROM ""JobCards"" WHERE ""CustomerId"" = OLD.""CustomerId"" AND ""Status"" = 2)
                            WHERE ""Id"" = OLD.""CustomerId"";
                            RETURN OLD;
                        ELSIF TG_OP = 'UPDATE' AND OLD.""Status"" <> NEW.""Status"" THEN
                            UPDATE ""Customers"" 
                            SET 
                                ""ActiveJobCards"" = (SELECT COUNT(*) FROM ""JobCards"" WHERE ""CustomerId"" = NEW.""CustomerId"" AND ""Status"" IN (0, 1)),
                                ""TotalSpent"" = (SELECT COALESCE(SUM(""TotalActualCost""), 0) FROM ""JobCards"" WHERE ""CustomerId"" = NEW.""CustomerId"" AND ""Status"" = 2)
                            WHERE ""Id"" = NEW.""CustomerId"";
                            RETURN NEW;
                        ELSIF TG_OP = 'UPDATE' AND NEW.""Status"" = 2 AND OLD.""TotalActualCost"" <> NEW.""TotalActualCost"" THEN
                            UPDATE ""Customers"" 
                            SET ""TotalSpent"" = (SELECT COALESCE(SUM(""TotalActualCost""), 0) FROM ""JobCards"" WHERE ""CustomerId"" = NEW.""CustomerId"" AND ""Status"" = 2)
                            WHERE ""Id"" = NEW.""CustomerId"";
                            RETURN NEW;
                        ELSE
                            UPDATE ""Customers"" 
                            SET 
                                ""ActiveJobCards"" = (SELECT COUNT(*) FROM ""JobCards"" WHERE ""CustomerId"" = NEW.""CustomerId"" AND ""Status"" IN (0, 1)),
                                ""TotalSpent"" = (SELECT COALESCE(SUM(""TotalActualCost""), 0) FROM ""JobCards"" WHERE ""CustomerId"" = NEW.""CustomerId"" AND ""Status"" = 2)
                            WHERE ""Id"" = NEW.""CustomerId"";
                            RETURN NEW;
                        END IF;
                    END IF;
                END;
                $$ LANGUAGE plpgsql;
            ");

            // Create triggers
            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_customer_stats_cars
                AFTER INSERT OR DELETE ON ""Cars""
                FOR EACH ROW EXECUTE FUNCTION update_customer_stats();
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_customer_stats_sessions
                AFTER INSERT OR DELETE ON ""GarageSessions""
                FOR EACH ROW EXECUTE FUNCTION update_customer_stats();
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_customer_stats_jobcards
                AFTER INSERT OR UPDATE OR DELETE ON ""JobCards""
                FOR EACH ROW EXECUTE FUNCTION update_customer_stats();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_customer_stats_cars ON ""Cars"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_customer_stats_sessions ON ""GarageSessions"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_customer_stats_jobcards ON ""JobCards"";");
            
            // Drop function
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_customer_stats();");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "ActiveJobCards",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "LastSessionDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotalCars",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotalSpent",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "TotalVisits",
                table: "Customers");
        }
    }
}
