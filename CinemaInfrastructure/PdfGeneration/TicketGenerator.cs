using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Threading.Tasks;

public class TicketGenerator
{
    public static async Task<byte[]> GenerateBookingTicket(CinemaDomain.Model.Booking booking)
    {
        // 1. Generate QR Code
        var qrData = $"BookingId:{booking.Id};UserId:{booking.UserId};BookingDate:{booking.BookingDate:yyyy-MM-dd HH:mm}";
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(10); // Reduced from 15 to 10 for smaller QR code

        // 2. Create PDF
        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(15); // Reduced margin from 20 to 15
                page.PageColor("#fff9e6"); // Light gold background
                page.DefaultTextStyle(x => x.FontSize(11).FontColor("#800020")); // Reduced font size from 12 to 11

                // Header: Movie Title
                page.Header()
                    .PaddingBottom(8) // Reduced from 10 to 8
                    .Text(booking.Session?.Movie?.Title ?? "N/A")
                    .Bold()
                    .FontSize(16) // Reduced from 18 to 16
                    .FontColor(booking.IsPrivateBooking ? "#660018" : "#800020") // Darker maroon for private
                    .AlignCenter();

                // Content: QR Code at Top, Details at Bottom
                page.Content()
                    .Column(column =>
                    {
                        // QR Code at Top
                        column.Item()
                            .PaddingBottom(8) // Reduced from 10 to 8
                            .AlignCenter()
                            .Width(200)
                            .Height(200)
                            .Image(qrCodeBytes);

                        // Ticket Details at Bottom
                        column.Item()
                            .Background("#f9f2e7") // Light gold background
                            .BorderLeft(3, Unit.Point)
                            .BorderColor("#d4af37") // Gold border
                            .Padding(8) // Reduced from 10 to 8
                            .Column(details =>
                            {
                                details.Item().Text(text =>
                                {
                                    text.Span("Кінотеатр: ").Bold();
                                    text.Span($"{booking.Session?.Schedule?.Hall?.Cinema?.Name ?? "N/A"}");
                                });
                                details.Item().Text(text =>
                                {
                                    text.Span("Зал: ").Bold();
                                    text.Span($"{booking.Session?.Schedule?.Hall?.Name ?? "N/A"}");
                                });
                                details.Item().Text(text =>
                                {
                                    text.Span("Розклад: ").Bold();
                                    text.Span($"{booking.Session?.Schedule?.StartTime.ToString("dd.MM.yyyy HH:mm") ?? "N/A"}");
                                });

                                // Seats for Ticket Bookings
                                if (!booking.IsPrivateBooking && booking.SessionSeats?.Any() == true)
                                {
                                    details.Item().PaddingTop(4).Text("Місця:").Bold(); // Reduced from 5 to 4
                                    foreach (var seat in booking.SessionSeats)
                                    {
                                        details.Item()
                                            .PaddingLeft(8) // Reduced from 10 to 8
                                            .PaddingVertical(2)
                                            .Background("#800020") // Maroon background
                                            .PaddingHorizontal(4) // Reduced from 5 to 4
                                            .Text($"Ряд {seat.Seat.RowNumber} Місце {seat.Seat.SeatNumber}")
                                            .Bold()
                                            .FontColor(Colors.White); // White text
                                    }
                                }

                                details.Item().PaddingTop(4).Text(text => // Reduced from 5 to 4
                                {
                                    text.Span("Ціна: ").Bold();
                                    text.Span($"{(booking.IsPrivateBooking ? booking.PrivateBookingPrice?.ToString("C", new System.Globalization.CultureInfo("uk-UA")) : booking.SessionSeats?.Sum(ss => ss.Price).ToString("C", new System.Globalization.CultureInfo("uk-UA")) ?? "N/A")}");
                                });
                            });
                    });

                // Footer
                page.Footer()
                    .AlignCenter()
                    .Text("Скануйте QR-код для перевірки квитка")
                    .FontSize(9) // Reduced from 10 to 9
                    .FontColor("#800020");
            });
        })
        .GeneratePdf();

        return pdfBytes;
    }
}