using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LTU.SearchEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CheckRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PageLinks_Pages_PageId",
                table: "PageLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PageWordFrequencies_Pages_PageId1",
                table: "PageWordFrequencies");

            migrationBuilder.DropForeignKey(
                name: "FK_PageWordFrequencies_Terms_TermId1",
                table: "PageWordFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_PageWordFrequencies_PageId1",
                table: "PageWordFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_PageWordFrequencies_TermId1",
                table: "PageWordFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_PageLinks_PageId",
                table: "PageLinks");

            migrationBuilder.DropColumn(
                name: "PageId1",
                table: "PageWordFrequencies");

            migrationBuilder.DropColumn(
                name: "TermId1",
                table: "PageWordFrequencies");

            migrationBuilder.DropColumn(
                name: "PageId",
                table: "PageLinks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PageId1",
                table: "PageWordFrequencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TermId1",
                table: "PageWordFrequencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PageId",
                table: "PageLinks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageWordFrequencies_PageId1",
                table: "PageWordFrequencies",
                column: "PageId1");

            migrationBuilder.CreateIndex(
                name: "IX_PageWordFrequencies_TermId1",
                table: "PageWordFrequencies",
                column: "TermId1");

            migrationBuilder.CreateIndex(
                name: "IX_PageLinks_PageId",
                table: "PageLinks",
                column: "PageId");

            migrationBuilder.AddForeignKey(
                name: "FK_PageLinks_Pages_PageId",
                table: "PageLinks",
                column: "PageId",
                principalTable: "Pages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PageWordFrequencies_Pages_PageId1",
                table: "PageWordFrequencies",
                column: "PageId1",
                principalTable: "Pages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PageWordFrequencies_Terms_TermId1",
                table: "PageWordFrequencies",
                column: "TermId1",
                principalTable: "Terms",
                principalColumn: "Id");
        }
    }
}
