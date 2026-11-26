using System.Globalization;
using System.Text.Json;
using OrthoFlowAI.Application.Messaging;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OrthoFlowAI.Infrastructure.Pdf;

public sealed record InvoiceLine(string Description, decimal Amount);

/// <summary>Everything the PDF needs, loaded by the caller (tenant-filtered).</summary>
public sealed class InvoicePdfModel
{
    public required Invoice Invoice { get; init; }
    public required Patient Patient { get; init; }
    public Clinic? Clinic { get; init; }
    public string? DoctorName { get; init; }
    public IReadOnlyList<Payment> Payments { get; init; } = Array.Empty<Payment>();
    public IReadOnlyList<Installment> Installments { get; init; } = Array.Empty<Installment>();
}

/// <summary>
/// Renders an invoice (with payments and instalment schedule) as PDF, in Arabic
/// (right-to-left) or English depending on the clinic's language.
/// </summary>
public static class InvoicePdfGenerator
{
    static InvoicePdfGenerator()
    {
        // QuestPDF Community licence: free for businesses under USD 1M annual revenue.
        QuestPDF.Settings.License = LicenseType.Community;

        // The bundled default font (Lato) has no Arabic glyphs. Font files placed next to
        // the app (e.g. a Fonts/*.ttf) are registered automatically; system fonts are used
        // as well (Windows: Segoe UI/Arial, Linux image: fonts-noto-core).
        QuestPDF.Settings.UseSystemFonts = true;
        // Use the first available family from FontFamilies instead of failing.
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    // Fonts tried in order; the first that has Arabic glyphs on the machine is used
    // (Windows: Segoe UI / Arial, Linux containers: Noto from fonts-noto-core).
    private static readonly string[] FontFamilies = { "Segoe UI", "Arial", "Noto Sans Arabic", "Noto Naskh Arabic", "DejaVu Sans" };

    public static byte[] Generate(InvoicePdfModel model)
    {
        var arabic = PatientMessages.IsArabic(model.Clinic?.Language);
        var t = arabic ? Labels.Arabic : Labels.English;
        var invoice = model.Invoice;
        var lines = ParseItems(invoice.ItemsJson, invoice.TotalAmount, t.Treatment);
        var net = invoice.TotalAmount - invoice.Discount + invoice.Tax;
        var remaining = net - invoice.PaidAmount;
        var brand = ParseColor(model.Clinic?.PrimaryColor);

        string Money(decimal v) => PatientMessages.Money(v, arabic);
        string Date(DateTime utc) => PatientMessages.ToLocal(utc).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

        return QuestPDF.Fluent.Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(s => s.FontFamily(FontFamilies).FontSize(10).FontColor(Colors.Grey.Darken4));
                if (arabic) page.ContentFromRightToLeft();

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(Iso(model.Clinic?.Name ?? "")).FontSize(18).Bold().FontColor(brand);
                            if (!string.IsNullOrWhiteSpace(model.Clinic?.Address))
                                c.Item().Text(Iso($"{model.Clinic!.Address}{(string.IsNullOrWhiteSpace(model.Clinic.City) ? "" : " - " + model.Clinic.City)}"));
                            if (!string.IsNullOrWhiteSpace(model.Clinic?.Phone))
                                c.Item().Text(Iso(model.Clinic!.Phone!));
                        });
                        row.ConstantItem(170).AlignRight().Column(c =>
                        {
                            c.Item().Text(t.Invoice).FontSize(20).Bold();
                            c.Item().Text($"{t.Number}: {Iso(invoice.InvoiceNumber)}");
                            c.Item().Text($"{t.Date}: {Date(invoice.CreatedAt)}");
                            if (invoice.DueDate.HasValue) c.Item().Text($"{t.DueDate}: {Date(invoice.DueDate.Value)}");
                        });
                    });
                    col.Item().PaddingVertical(8).LineHorizontal(1.5f).LineColor(brand);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(14);

                    col.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t.BillTo).Bold();
                            c.Item().Text(Iso($"{model.Patient.FirstName} {model.Patient.LastName}"));
                            if (!string.IsNullOrWhiteSpace(model.Patient.Phone)) c.Item().Text(Iso(model.Patient.Phone!));
                        });
                        if (!string.IsNullOrWhiteSpace(model.DoctorName))
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(t.Doctor).Bold();
                                c.Item().Text(Iso(model.DoctorName!));
                            });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text(t.Status).Bold();
                            c.Item().Text(StatusLabel(invoice, remaining, t));
                        });
                    });

                    // Items
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c => { c.ConstantColumn(28); c.RelativeColumn(); c.ConstantColumn(110); });
                        table.Header(h =>
                        {
                            HeaderCell(h.Cell(), "#", brand);
                            HeaderCell(h.Cell(), t.Description, brand);
                            HeaderCell(h.Cell(), t.Amount, brand, right: true);
                        });
                        for (var i = 0; i < lines.Count; i++)
                        {
                            BodyCell(table.Cell(), (i + 1).ToString(CultureInfo.InvariantCulture));
                            BodyCell(table.Cell(), Iso(lines[i].Description));
                            BodyCell(table.Cell(), Money(lines[i].Amount), right: true);
                        }
                    });

                    // Totals
                    col.Item().AlignRight().Width(240).Column(c =>
                    {
                        TotalRow(c, t.Subtotal, Money(invoice.TotalAmount));
                        if (invoice.Discount != 0) TotalRow(c, t.Discount, "- " + Money(invoice.Discount));
                        if (invoice.Tax != 0) TotalRow(c, t.Tax, Money(invoice.Tax));
                        TotalRow(c, t.Total, Money(net), bold: true);
                        TotalRow(c, t.Paid, Money(invoice.PaidAmount));
                        TotalRow(c, t.Remaining, Money(remaining), bold: true, color: remaining > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                    });

                    if (model.Installments.Count > 0)
                    {
                        col.Item().Text(t.InstallmentPlan).FontSize(13).Bold().FontColor(brand);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.ConstantColumn(28); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                            table.Header(h =>
                            {
                                HeaderCell(h.Cell(), "#", brand);
                                HeaderCell(h.Cell(), t.DueDate, brand);
                                HeaderCell(h.Cell(), t.Amount, brand, right: true);
                                HeaderCell(h.Cell(), t.Paid, brand, right: true);
                                HeaderCell(h.Cell(), t.Status, brand);
                            });
                            foreach (var inst in model.Installments.OrderBy(i => i.Number))
                            {
                                BodyCell(table.Cell(), inst.Number.ToString(CultureInfo.InvariantCulture));
                                BodyCell(table.Cell(), Date(inst.DueDate));
                                BodyCell(table.Cell(), Money(inst.Amount), right: true);
                                BodyCell(table.Cell(), Money(inst.PaidAmount), right: true);
                                BodyCell(table.Cell(), InstallmentLabel(inst.Status, t),
                                    color: inst.Status switch
                                    {
                                        InstallmentStatus.Paid => Colors.Green.Darken2,
                                        InstallmentStatus.Overdue => Colors.Red.Darken2,
                                        _ => (Color?)null,
                                    });
                            }
                        });
                    }

                    if (model.Payments.Count > 0)
                    {
                        col.Item().Text(t.Payments).FontSize(13).Bold().FontColor(brand);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                            table.Header(h =>
                            {
                                HeaderCell(h.Cell(), t.Date, brand);
                                HeaderCell(h.Cell(), t.Method, brand);
                                HeaderCell(h.Cell(), t.Amount, brand, right: true);
                            });
                            foreach (var p in model.Payments.OrderBy(p => p.PaymentDate))
                            {
                                BodyCell(table.Cell(), Date(p.PaymentDate));
                                BodyCell(table.Cell(), MethodLabel(p.PaymentMethod, arabic));
                                BodyCell(table.Cell(), Money(p.Amount), right: true);
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                        col.Item().Column(c =>
                        {
                            c.Item().Text(t.Notes).Bold();
                            c.Item().Text(Iso(invoice.Notes!));
                        });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span(t.ThankYou + "  •  ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    x.Span(" / ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    x.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// Wraps text that contains no Arabic letters in Unicode "left-to-right isolate"
    /// marks so that, inside an RTL page, "+201012345678" or "42 Tahrir St." keep their
    /// order instead of being flipped by the bidi algorithm.
    /// </summary>
    public static string Iso(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        foreach (var ch in text)
            if (ch is >= '\u0600' and <= '\u06FF') return text;
        return "\u2066" + text + "\u2069";
    }

    /// <summary>Clinic brand colour from settings ("#1E88E5"); falls back to blue if missing/invalid.</summary>
    private static Color ParseColor(string? hex)
    {
        if (!string.IsNullOrWhiteSpace(hex))
        {
            try { return Color.FromHex(hex.Trim()); }
            catch (Exception) { /* invalid value in clinic settings */ }
        }
        return Color.FromHex("#1E88E5");
    }

    /// <summary>Items are stored as JSON: [{"description": "...", "amount": 1000}, ...].</summary>
    public static IReadOnlyList<InvoiceLine> ParseItems(string? itemsJson, decimal fallbackTotal, string fallbackLabel)
    {
        var result = new List<InvoiceLine>();
        if (!string.IsNullOrWhiteSpace(itemsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(itemsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        var desc = TryGet(el, "description") ?? TryGet(el, "name") ?? fallbackLabel;
                        decimal amount = 0;
                        foreach (var key in new[] { "amount", "price", "total" })
                        {
                            if (TryGetProperty(el, key, out var v) && v.ValueKind == JsonValueKind.Number) { amount = v.GetDecimal(); break; }
                        }
                        if (TryGetProperty(el, "quantity", out var q) && q.ValueKind == JsonValueKind.Number && TryGetProperty(el, "unitPrice", out var up) && up.ValueKind == JsonValueKind.Number)
                            amount = q.GetDecimal() * up.GetDecimal();
                        result.Add(new InvoiceLine(desc, amount));
                    }
                }
            }
            catch (JsonException) { /* fall through to a single line */ }
        }
        if (result.Count == 0) result.Add(new InvoiceLine(fallbackLabel, fallbackTotal));
        return result;
    }

    private static bool TryGetProperty(JsonElement el, string name, out JsonElement value)
    {
        foreach (var p in el.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)) { value = p.Value; return true; }
        }
        value = default;
        return false;
    }

    private static string? TryGet(JsonElement el, string name) =>
        el.ValueKind == JsonValueKind.Object && TryGetProperty(el, name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static void HeaderCell(IContainer cell, string text, Color brand, bool right = false)
    {
        var c = cell.Background(brand).PaddingVertical(5).PaddingHorizontal(6);
        (right ? c.AlignRight() : c).Text(text).FontColor(Colors.White).Bold();
    }

    private static void BodyCell(IContainer cell, string text, bool right = false, Color? color = null)
    {
        var c = cell.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(6);
        var span = (right ? c.AlignRight() : c).Text(text);
        if (color.HasValue) span.FontColor(color.Value);
    }

    private static void TotalRow(ColumnDescriptor c, string label, string value, bool bold = false, Color? color = null)
    {
        c.Item().PaddingVertical(2).Row(r =>
        {
            var l = r.RelativeItem().Text(label);
            var v = r.ConstantItem(110).AlignRight().Text(value);
            if (bold) { l.Bold(); v.Bold(); }
            if (color.HasValue) v.FontColor(color.Value);
        });
    }

    private static string StatusLabel(Invoice invoice, decimal remaining, Labels t) =>
        invoice.Status == InvoiceStatus.Cancelled ? t.Cancelled
        : remaining <= 0 ? t.StatusPaid
        : invoice.PaidAmount > 0 ? t.StatusPartial
        : t.StatusUnpaid;

    private static string InstallmentLabel(InstallmentStatus s, Labels t) => s switch
    {
        InstallmentStatus.Paid => t.StatusPaid,
        InstallmentStatus.PartiallyPaid => t.StatusPartial,
        InstallmentStatus.Overdue => t.Overdue,
        _ => t.Upcoming,
    };

    private static string MethodLabel(PaymentMethod m, bool arabic) => !arabic ? m.ToString() : m.ToString() switch
    {
        "Cash" => "نقدي",
        "CreditCard" => "بطاقة ائتمان",
        "DebitCard" => "بطاقة خصم",
        "BankTransfer" => "تحويل بنكي",
        "Insurance" => "تأمين",
        "Online" => "دفع إلكتروني",
        "Other" => "أخرى",
        var other => other,
    };

    private sealed record Labels(
        string Invoice, string Number, string Date, string DueDate, string BillTo, string Doctor, string Status,
        string Description, string Amount, string Subtotal, string Discount, string Tax, string Total, string Paid,
        string Remaining, string InstallmentPlan, string Payments, string Method, string Notes, string ThankYou,
        string Treatment, string StatusPaid, string StatusPartial, string StatusUnpaid, string Cancelled,
        string Overdue, string Upcoming)
    {
        public static readonly Labels Arabic = new(
            "فاتورة", "رقم الفاتورة", "التاريخ", "تاريخ الاستحقاق", "بيانات المريض", "الطبيب", "الحالة",
            "البيان", "المبلغ", "الإجمالي قبل الخصم", "الخصم", "الضريبة", "الإجمالي", "المدفوع",
            "المتبقي", "جدول الأقساط", "المدفوعات", "طريقة الدفع", "ملاحظات", "شكراً لثقتكم",
            "العلاج", "مدفوعة", "مدفوعة جزئياً", "غير مدفوعة", "ملغاة", "متأخر", "قادم");

        public static readonly Labels English = new(
            "INVOICE", "Invoice #", "Date", "Due date", "Bill to", "Doctor", "Status",
            "Description", "Amount", "Subtotal", "Discount", "Tax", "Total", "Paid",
            "Balance due", "Instalment plan", "Payments", "Method", "Notes", "Thank you for your trust",
            "Treatment", "Paid", "Partially paid", "Unpaid", "Cancelled", "Overdue", "Upcoming");
    }
}
