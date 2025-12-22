using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <summary>
    /// Adds PostgreSQL triggers to automatically update denormalized customer fields
    /// (TotalCars, TotalVisits, LastSessionDate, ActiveJobCards, TotalSpent) when
    /// related entities (Cars, Sessions, JobCards) are inserted, updated, or deleted.
    /// </summary>
    public partial class AddCustomerDenormalizationTriggers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ====================================================================
            // FUNCTION: Update TotalCars count
            // ====================================================================
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_customer_total_cars()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF (TG_OP = 'DELETE') THEN
                        UPDATE ""Customers""
                        SET ""TotalCars"" = (
                            SELECT COUNT(*)::int 
                            FROM ""Cars"" 
                            WHERE ""CustomerId"" = OLD.""CustomerId""
                        )
                        WHERE ""Id"" = OLD.""CustomerId"";
                        RETURN OLD;
                    ELSIF (TG_OP = 'INSERT') THEN
                        UPDATE ""Customers""
                        SET ""TotalCars"" = (
                            SELECT COUNT(*)::int 
                            FROM ""Cars"" 
                            WHERE ""CustomerId"" = NEW.""CustomerId""
                        )
                        WHERE ""Id"" = NEW.""CustomerId"";
                        RETURN NEW;
                    ELSIF (TG_OP = 'UPDATE' AND OLD.""CustomerId"" <> NEW.""CustomerId"") THEN
                        -- Car moved to different customer
                        UPDATE ""Customers""
                        SET ""TotalCars"" = (
                            SELECT COUNT(*)::int 
                            FROM ""Cars"" 
                            WHERE ""CustomerId"" = OLD.""CustomerId""
                        )
                        WHERE ""Id"" = OLD.""CustomerId"";
                        
                        UPDATE ""Customers""
                        SET ""TotalCars"" = (
                            SELECT COUNT(*)::int 
                            FROM ""Cars"" 
                            WHERE ""CustomerId"" = NEW.""CustomerId""
                        )
                        WHERE ""Id"" = NEW.""CustomerId"";
                        RETURN NEW;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_customer_total_cars
                AFTER INSERT OR UPDATE OR DELETE ON ""Cars""
                FOR EACH ROW
                EXECUTE FUNCTION update_customer_total_cars();
            ");

            // ====================================================================
            // FUNCTION: Update TotalVisits and LastSessionDate
            // ====================================================================
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_customer_session_stats()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF (TG_OP = 'DELETE') THEN
                        UPDATE ""Customers""
                        SET 
                            ""TotalVisits"" = (
                                SELECT COUNT(*)::int 
                                FROM ""GarageSessions"" 
                                WHERE ""CustomerId"" = OLD.""CustomerId""
                            ),
                            ""LastSessionDate"" = (
                                SELECT MAX(""CreatedAt"") 
                                FROM ""GarageSessions"" 
                                WHERE ""CustomerId"" = OLD.""CustomerId""
                            )
                        WHERE ""Id"" = OLD.""CustomerId"";
                        RETURN OLD;
                    ELSIF (TG_OP = 'INSERT') THEN
                        UPDATE ""Customers""
                        SET 
                            ""TotalVisits"" = (
                                SELECT COUNT(*)::int 
                                FROM ""GarageSessions"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId""
                            ),
                            ""LastSessionDate"" = (
                                SELECT MAX(""CreatedAt"") 
                                FROM ""GarageSessions"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId""
                            )
                        WHERE ""Id"" = NEW.""CustomerId"";
                        RETURN NEW;
                    ELSIF (TG_OP = 'UPDATE') THEN
                        -- Update both old and new customer if customer changed
                        IF OLD.""CustomerId"" <> NEW.""CustomerId"" THEN
                            UPDATE ""Customers""
                            SET 
                                ""TotalVisits"" = (
                                    SELECT COUNT(*)::int 
                                    FROM ""GarageSessions"" 
                                    WHERE ""CustomerId"" = OLD.""CustomerId""
                                ),
                                ""LastSessionDate"" = (
                                    SELECT MAX(""CreatedAt"") 
                                    FROM ""GarageSessions"" 
                                    WHERE ""CustomerId"" = OLD.""CustomerId""
                                )
                            WHERE ""Id"" = OLD.""CustomerId"";
                        END IF;
                        
                        UPDATE ""Customers""
                        SET 
                            ""TotalVisits"" = (
                                SELECT COUNT(*)::int 
                                FROM ""GarageSessions"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId""
                            ),
                            ""LastSessionDate"" = (
                                SELECT MAX(""CreatedAt"") 
                                FROM ""GarageSessions"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId""
                            )
                        WHERE ""Id"" = NEW.""CustomerId"";
                        RETURN NEW;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_customer_session_stats
                AFTER INSERT OR UPDATE OR DELETE ON ""GarageSessions""
                FOR EACH ROW
                EXECUTE FUNCTION update_customer_session_stats();
            ");

            // ====================================================================
            // FUNCTION: Update ActiveJobCards and TotalSpent
            // ====================================================================
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION update_customer_jobcard_stats()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF (TG_OP = 'DELETE') THEN
                        UPDATE ""Customers""
                        SET 
                            ""ActiveJobCards"" = (
                                SELECT COUNT(*)::int 
                                FROM ""JobCards"" 
                                WHERE ""CustomerId"" = OLD.""CustomerId"" 
                                AND ""Status"" NOT IN (4, 5)  -- Not Completed or Cancelled
                            ),
                            ""TotalSpent"" = COALESCE((
                                SELECT SUM(""TotalActualCost"") 
                                FROM ""JobCards"" 
                                WHERE ""CustomerId"" = OLD.""CustomerId"" 
                                AND ""Status"" = 4  -- Completed only
                            ), 0)
                        WHERE ""Id"" = OLD.""CustomerId"";
                        RETURN OLD;
                    ELSIF (TG_OP = 'INSERT') THEN
                        UPDATE ""Customers""
                        SET 
                            ""ActiveJobCards"" = (
                                SELECT COUNT(*)::int 
                                FROM ""JobCards"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId"" 
                                AND ""Status"" NOT IN (4, 5)  -- Not Completed or Cancelled
                            ),
                            ""TotalSpent"" = COALESCE((
                                SELECT SUM(""TotalActualCost"") 
                                FROM ""JobCards"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId"" 
                                AND ""Status"" = 4  -- Completed only
                            ), 0)
                        WHERE ""Id"" = NEW.""CustomerId"";
                        RETURN NEW;
                    ELSIF (TG_OP = 'UPDATE') THEN
                        -- Update old customer if customer changed
                        IF OLD.""CustomerId"" <> NEW.""CustomerId"" THEN
                            UPDATE ""Customers""
                            SET 
                                ""ActiveJobCards"" = (
                                    SELECT COUNT(*)::int 
                                    FROM ""JobCards"" 
                                    WHERE ""CustomerId"" = OLD.""CustomerId"" 
                                    AND ""Status"" NOT IN (4, 5)
                                ),
                                ""TotalSpent"" = COALESCE((
                                    SELECT SUM(""TotalActualCost"") 
                                    FROM ""JobCards"" 
                                    WHERE ""CustomerId"" = OLD.""CustomerId"" 
                                    AND ""Status"" = 4
                                ), 0)
                            WHERE ""Id"" = OLD.""CustomerId"";
                        END IF;
                        
                        -- Update new customer (or same customer if status/cost changed)
                        UPDATE ""Customers""
                        SET 
                            ""ActiveJobCards"" = (
                                SELECT COUNT(*)::int 
                                FROM ""JobCards"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId"" 
                                AND ""Status"" NOT IN (4, 5)
                            ),
                            ""TotalSpent"" = COALESCE((
                                SELECT SUM(""TotalActualCost"") 
                                FROM ""JobCards"" 
                                WHERE ""CustomerId"" = NEW.""CustomerId"" 
                                AND ""Status"" = 4
                            ), 0)
                        WHERE ""Id"" = NEW.""CustomerId"";
                        RETURN NEW;
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
            ");

            migrationBuilder.Sql(@"
                CREATE TRIGGER trigger_update_customer_jobcard_stats
                AFTER INSERT OR UPDATE OR DELETE ON ""JobCards""
                FOR EACH ROW
                EXECUTE FUNCTION update_customer_jobcard_stats();
            ");

            // ====================================================================
            // Initialize existing customer denormalized fields
            // ====================================================================
            migrationBuilder.Sql(@"
                UPDATE ""Customers"" c
                SET 
                    ""TotalCars"" = COALESCE((SELECT COUNT(*)::int FROM ""Cars"" WHERE ""CustomerId"" = c.""Id""), 0),
                    ""TotalVisits"" = COALESCE((SELECT COUNT(*)::int FROM ""GarageSessions"" WHERE ""CustomerId"" = c.""Id""), 0),
                    ""LastSessionDate"" = (SELECT MAX(""CreatedAt"") FROM ""GarageSessions"" WHERE ""CustomerId"" = c.""Id""),
                    ""ActiveJobCards"" = COALESCE((SELECT COUNT(*)::int FROM ""JobCards"" WHERE ""CustomerId"" = c.""Id"" AND ""Status"" NOT IN (4, 5)), 0),
                    ""TotalSpent"" = COALESCE((SELECT SUM(""TotalActualCost"") FROM ""JobCards"" WHERE ""CustomerId"" = c.""Id"" AND ""Status"" = 4), 0);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers first (they depend on functions)
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_customer_total_cars ON ""Cars"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_customer_session_stats ON ""GarageSessions"";");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS trigger_update_customer_jobcard_stats ON ""JobCards"";");
            
            // Drop functions
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_customer_total_cars();");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_customer_session_stats();");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_customer_jobcard_stats();");
        }
    }
}
