using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaInfrastructure.Migrations
{
    public partial class SessionUpdating : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove the old CreatedAt column
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Sessions");

            // Add the new CreatedAt column with DateTime type
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Sessions",
                type: "datetime2",
                nullable: false,
                defaultValue: DateTime.UtcNow); // You can set a default value if needed

            // Update IsActive column
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Sessions",
                type: "bit",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "binary(50)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new CreatedAt column
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Sessions");

            // Add back the old CreatedAt column as a rowversion (timestamp)
            migrationBuilder.AddColumn<byte[]>(
                name: "CreatedAt",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            // Revert IsActive column back to byte[]
            migrationBuilder.AlterColumn<byte[]>(
                name: "IsActive",
                table: "Sessions",
                type: "binary(50)",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit");
        }
    }
}
