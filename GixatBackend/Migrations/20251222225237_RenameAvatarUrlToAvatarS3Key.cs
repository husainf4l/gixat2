using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    public partial class RenameAvatarUrlToAvatarS3Key : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new column for S3 keys
            migrationBuilder.AddColumn<string>(
                name: "AvatarS3Key",
                table: "AspNetUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            // Step 2: Migrate data - extract S3 keys from URLs
            // This handles URLs in format: https://bucket.s3.region.amazonaws.com/key or https://bucket/key
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers""
                SET ""AvatarS3Key"" = 
                    CASE 
                        WHEN ""AvatarUrl"" IS NULL THEN NULL
                        WHEN ""AvatarUrl"" LIKE 'http%' THEN 
                            -- Extract path after domain, remove leading slash
                            SUBSTRING(""AvatarUrl"" FROM '(?:https?://[^/]+/)(.+)')
                        ELSE ""AvatarUrl""
                    END
                WHERE ""AvatarUrl"" IS NOT NULL;
            ");

            // Step 3: Drop old column
            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Downgrade will lose the full URL format, only S3 keys can be restored
            migrationBuilder.RenameColumn(
                name: "AvatarS3Key",
                table: "AspNetUsers",
                newName: "AvatarUrl");
        }
    }
}
