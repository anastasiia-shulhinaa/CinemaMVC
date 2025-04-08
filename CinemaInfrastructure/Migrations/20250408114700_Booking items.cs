using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaInfrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Bookingitems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            // Drop the old 'BookingDate' column (rowversion)
            migrationBuilder.DropColumn(
                name: "BookingDate",
                table: "Bookings");

            migrationBuilder.AlterColumn<bool>(
                name: "IsPrivateBooking",
                table: "Bookings",
                type: "bit",
                fixedLength: true,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "binary(50)",
                oldFixedLength: true,
                oldMaxLength: 50);

            // Add the new 'BookingDate' column with datetime2
            migrationBuilder.AddColumn<DateTime>(
                name: "BookingDate",
                table: "Bookings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()"); // or set your own default
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new 'BookingDate' column
            migrationBuilder.DropColumn(
                name: "BookingDate",
                table: "Bookings");

            migrationBuilder.AlterColumn<byte[]>(
                name: "IsPrivateBooking",
                table: "Bookings",
                type: "binary(50)",
                fixedLength: true,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldFixedLength: true,
                oldMaxLength: 50);

            // Restore the old 'BookingDate' as rowversion
            migrationBuilder.AddColumn<byte[]>(
                name: "BookingDate",
                table: "Bookings",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

    }
}
