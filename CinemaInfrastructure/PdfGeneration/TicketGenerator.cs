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
        var qrCodeBytes = qrCode.GetGraphic(20);

        // 2. Create PDF
        var pdfBytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(20);
                page.PageColor("#fff9e6"); // Light gold background
                page.DefaultTextStyle(x => x.FontSize(12).FontColor("#800020")); // Maroon text

                // Header: Movie Title
                page.Header()
                    .PaddingBottom(10)
                    .Text(booking.Session?.Movie?.Title ?? "N/A")
                    .Bold()
                    .FontSize(18)
                    .FontColor(booking.IsPrivateBooking ? "#660018" : "#800020") // Darker maroon for private
                    .AlignCenter();

                // Content: QR Code at Top, Details at Bottom
                page.Content()
                    .Column(column =>
                    {
                        // QR Code at Top
                        column.Item()
                            .PaddingBottom(15)
                            .AlignCenter()
                            .Image(qrCodeBytes);

                        // Ticket Details at Bottom
                        column.Item()
                            .Background("#f9f2e7") // Light gold background
                            .BorderLeft(3, Unit.Point)
                            .BorderColor("#d4af37") // Gold border
                            .Padding(10)
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
                                    details.Item().PaddingTop(5).Text("Місця:").Bold();
                                    foreach (var seat in booking.SessionSeats)
                                    {
                                        details.Item()
                                            .PaddingLeft(10)
                                            .PaddingVertical(2)
                                            .Background("#800020") // Maroon background
                                            .PaddingHorizontal(5)
                                            .Text($"Ряд {seat.Seat.RowNumber} Місце {seat.Seat.SeatNumber}")
                                            .Bold()
                                            .FontColor(Colors.White); // White text
                                    }
                                }

                                details.Item().PaddingTop(5).Text(text =>
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
                    .FontSize(10)
                    .FontColor("#800020");
            });
        })
        .GeneratePdf();

        return pdfBytes;
    }
}