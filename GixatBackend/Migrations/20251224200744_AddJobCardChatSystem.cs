using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GixatBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddJobCardChatSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobCardComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JobCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    JobItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    AuthorId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsEdited = table.Column<bool>(type: "boolean", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCardComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobCardComments_AspNetUsers_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobCardComments_JobCardComments_ParentCommentId",
                        column: x => x.ParentCommentId,
                        principalTable: "JobCardComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobCardComments_JobCards_JobCardId",
                        column: x => x.JobCardId,
                        principalTable: "JobCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobCardComments_JobItems_JobItemId",
                        column: x => x.JobItemId,
                        principalTable: "JobItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JobCardCommentMentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MentionedUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobCardCommentMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobCardCommentMentions_AspNetUsers_MentionedUserId",
                        column: x => x.MentionedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobCardCommentMentions_JobCardComments_CommentId",
                        column: x => x.CommentId,
                        principalTable: "JobCardComments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobCardCommentMentions_CommentId",
                table: "JobCardCommentMentions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_JobCardCommentMentions_MentionedUserId_IsRead",
                table: "JobCardCommentMentions",
                columns: new[] { "MentionedUserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_JobCardComments_AuthorId",
                table: "JobCardComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_JobCardComments_JobCardId_CreatedAt",
                table: "JobCardComments",
                columns: new[] { "JobCardId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_JobCardComments_JobItemId_CreatedAt",
                table: "JobCardComments",
                columns: new[] { "JobItemId", "CreatedAt" },
                filter: "\"JobItemId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JobCardComments_ParentCommentId",
                table: "JobCardComments",
                column: "ParentCommentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobCardCommentMentions");

            migrationBuilder.DropTable(
                name: "JobCardComments");
        }
    }
}
