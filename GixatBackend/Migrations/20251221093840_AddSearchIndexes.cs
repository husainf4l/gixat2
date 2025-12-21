using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    internal partial class AddSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            
            // Enable pg_trgm extension for trigram-based text search
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            
            // Add GIN indexes for case-insensitive text search in PostgreSQL
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_customers_firstname_gin ON ""Customers"" USING gin (UPPER(""FirstName"") gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_customers_lastname_gin ON ""Customers"" USING gin (UPPER(""LastName"") gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_customers_email_gin ON ""Customers"" USING gin (UPPER(""Email"") gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_customers_phone_gin ON ""Customers"" USING gin (UPPER(""PhoneNumber"") gin_trgm_ops);
                
                CREATE INDEX IF NOT EXISTS idx_cars_make_gin ON ""Cars"" USING gin (UPPER(""Make"") gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_cars_model_gin ON ""Cars"" USING gin (UPPER(""Model"") gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_cars_licenseplate_gin ON ""Cars"" USING gin (UPPER(""LicensePlate"") gin_trgm_ops);
                CREATE INDEX IF NOT EXISTS idx_cars_vin_gin ON ""Cars"" USING gin (UPPER(""VIN"") gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);
            
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS idx_customers_firstname_gin;
                DROP INDEX IF EXISTS idx_customers_lastname_gin;
                DROP INDEX IF EXISTS idx_customers_email_gin;
                DROP INDEX IF EXISTS idx_customers_phone_gin;
                DROP INDEX IF EXISTS idx_cars_make_gin;
                DROP INDEX IF EXISTS idx_cars_model_gin;
                DROP INDEX IF EXISTS idx_cars_licenseplate_gin;
                DROP INDEX IF EXISTS idx_cars_vin_gin;
            ");
        }
    }
}
