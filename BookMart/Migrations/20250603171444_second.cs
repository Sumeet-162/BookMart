using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookMart.Migrations
{
    /// <inheritdoc />
    public partial class second : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2023, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9748));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9896));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9897));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9898));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9899));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9900));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9900));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9901));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9902));

            migrationBuilder.UpdateData(
                table: "Genres",
                keyColumn: "GenreID",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2025, 6, 3, 17, 8, 59, 678, DateTimeKind.Utc).AddTicks(9903));
        }
    }
}
