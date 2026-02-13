using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTU.SearchEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    LastCrawled = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PageRankScore = table.Column<double>(type: "REAL", nullable: false),
                    ContentHash = table.Column<string>(type: "TEXT", nullable: false),
                    WordCount = table.Column<int>(type: "INTEGER", nullable: false),
                    HttpStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Word = table.Column<string>(type: "TEXT", nullable: false),
                    IdfScore = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageLinks",
                columns: table => new
                {
                    FromPageId = table.Column<int>(type: "INTEGER", nullable: false),
                    ToPageId = table.Column<int>(type: "INTEGER", nullable: false),
                    PageId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageLinks", x => new { x.FromPageId, x.ToPageId });
                    table.ForeignKey(
                        name: "FK_PageLinks_Pages_FromPageId",
                        column: x => x.FromPageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PageLinks_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PageLinks_Pages_ToPageId",
                        column: x => x.ToPageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PageWordFrequencies",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "INTEGER", nullable: false),
                    TermId = table.Column<int>(type: "INTEGER", nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    TfWeight = table.Column<double>(type: "REAL", nullable: false),
                    PageId1 = table.Column<int>(type: "INTEGER", nullable: true),
                    TermId1 = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageWordFrequencies", x => new { x.PageId, x.TermId });
                    table.ForeignKey(
                        name: "FK_PageWordFrequencies_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageWordFrequencies_Pages_PageId1",
                        column: x => x.PageId1,
                        principalTable: "Pages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PageWordFrequencies_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageWordFrequencies_Terms_TermId1",
                        column: x => x.TermId1,
                        principalTable: "Terms",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageLinks_PageId",
                table: "PageLinks",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_PageLinks_ToPageId",
                table: "PageLinks",
                column: "ToPageId");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Url",
                table: "Pages",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageWordFrequencies_PageId1",
                table: "PageWordFrequencies",
                column: "PageId1");

            migrationBuilder.CreateIndex(
                name: "IX_PageWordFrequencies_TermId",
                table: "PageWordFrequencies",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_PageWordFrequencies_TermId1",
                table: "PageWordFrequencies",
                column: "TermId1");

            migrationBuilder.CreateIndex(
                name: "IX_Terms_Word",
                table: "Terms",
                column: "Word",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageLinks");

            migrationBuilder.DropTable(
                name: "PageWordFrequencies");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "Terms");
        }
    }
}
