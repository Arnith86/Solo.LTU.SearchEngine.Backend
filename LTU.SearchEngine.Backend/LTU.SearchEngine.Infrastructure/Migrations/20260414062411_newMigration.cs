using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTU.SearchEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Url = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastCrawled = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PageRankScore = table.Column<double>(type: "float", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    HttpStatus = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Terms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Word = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IdfScore = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Terms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageLinks",
                columns: table => new
                {
                    FromPageId = table.Column<int>(type: "int", nullable: false),
                    ToPageId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageLinks", x => new { x.FromPageId, x.ToPageId });
                    table.ForeignKey(
                        name: "FK_PageLinks_Pages_FromPageId",
                        column: x => x.FromPageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                    PageId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: false),
                    TitleFrequency = table.Column<int>(type: "int", nullable: false),
                    HeaderFrequency = table.Column<int>(type: "int", nullable: false),
                    BodyFrequency = table.Column<int>(type: "int", nullable: false),
                    TfWeight = table.Column<double>(type: "float", nullable: false)
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
                        name: "FK_PageWordFrequencies_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PageWordPosition",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageWordPosition", x => new { x.PageId, x.TermId, x.Position });
                    table.ForeignKey(
                        name: "FK_PageWordPosition_Pages_PageId",
                        column: x => x.PageId,
                        principalTable: "Pages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PageWordPosition_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_PageWordFrequencies_TermId",
                table: "PageWordFrequencies",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_PageWordPosition_TermId",
                table: "PageWordPosition",
                column: "TermId");

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
                name: "PageWordPosition");

            migrationBuilder.DropTable(
                name: "Pages");

            migrationBuilder.DropTable(
                name: "Terms");
        }
    }
}
