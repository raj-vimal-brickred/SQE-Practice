using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SQE_Practice.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestLogsMiddleware : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QueryString = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DurationMs = table.Column<double>(type: "float", nullable: false),
                    ClientIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RequestBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ErrorType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Path_Timestamp",
                table: "RequestLogs",
                columns: new[] { "Path", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_RequestId",
                table: "RequestLogs",
                column: "RequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_Severity_Timestamp",
                table: "RequestLogs",
                columns: new[] { "Severity", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestLogs");
        }
    }
}
