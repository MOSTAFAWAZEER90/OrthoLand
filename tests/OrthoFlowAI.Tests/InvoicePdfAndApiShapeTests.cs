using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Application.DTOs.Common;
using OrthoFlowAI.Domain.Common;
using OrthoFlowAI.Domain.Entities;
using OrthoFlowAI.Domain.Enums;
using OrthoFlowAI.Infrastructure.Pdf;
using OrthoFlowAI.Web.Services;

namespace OrthoFlowAI.Tests;

public class InvoicePdfTests
{
    private static InvoicePdfModel Model(string language) => new()
    {
        Invoice = new Invoice
        {
            InvoiceNumber = "INV-1", TotalAmount = 45_000m, PaidAmount = 25_000m, Discount = 0, Tax = 0,
            ItemsJson = "[{\"description\":\"Damon brackets\",\"amount\":25000},{\"description\":\"متابعة\",\"amount\":20000}]",
            CreatedAt = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
        },
        Patient = new Patient { FirstName = "Omar", LastName = "Khaled", Phone = "+201055551001" },
        Clinic = new Clinic { Name = "Smile", Language = language, PrimaryColor = "not-a-colour" },
        Installments = new[]
        {
            new Installment { Number = 1, Amount = 10_000m, PaidAmount = 10_000m, Status = InstallmentStatus.Paid, DueDate = DateTime.UtcNow },
            new Installment { Number = 2, Amount = 10_000m, Status = InstallmentStatus.Overdue, DueDate = DateTime.UtcNow },
        },
        Payments = new[] { new Payment { Amount = 25_000m, PaymentMethod = PaymentMethod.Cash, PaymentDate = DateTime.UtcNow } },
    };

    [Theory]
    [InlineData("ar")]
    [InlineData("en")]
    public void Generates_a_pdf_in_both_languages(string language)
    {
        var bytes = InvoicePdfGenerator.Generate(Model(language));

        Assert.True(bytes.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void Arabic_invoice_uses_the_embedded_arabic_font()
    {
        // The font ships inside the assembly, so Arabic renders even on servers without Arabic fonts.
        var bytes = InvoicePdfGenerator.Generate(Model("ar"));

        Assert.Contains("NotoSansArabic", Encoding.Latin1.GetString(bytes));
    }

    [Fact]
    public void Items_json_is_parsed_and_falls_back_to_a_single_line()
    {
        var lines = InvoicePdfGenerator.ParseItems("[{\"Description\":\"A\",\"Amount\":100},{\"name\":\"B\",\"price\":50}]", 0, "x");
        Assert.Equal(new[] { ("A", 100m), ("B", 50m) }, lines.Select(l => (l.Description, l.Amount)));

        var fallback = InvoicePdfGenerator.ParseItems("not json", 999m, "Treatment");
        Assert.Equal(("Treatment", 999m), (fallback.Single().Description, fallback.Single().Amount));
    }

    [Fact]
    public void Latin_text_is_isolated_but_arabic_is_left_alone()
    {
        Assert.Equal("⁦+201012345678⁩", InvoicePdfGenerator.Iso("+201012345678"));
        Assert.Equal("عيادة", InvoicePdfGenerator.Iso("عيادة"));
    }
}

/// <summary>
/// The Web's pages expect several response shapes; ApiService adapts the API's
/// { success, data } envelope to them. These pin that behaviour.
/// </summary>
public class ApiResponseShapeTests
{
    private static ApiService Service() => new(null!, NullLogger<ApiService>.Instance, null!);

    private const string Paged = """
        {"success":true,"message":"ok","data":{"items":[{"id":"1","invoiceNumber":"INV-1"},{"id":"2","invoiceNumber":"INV-2"}],
         "totalCount":7,"pageNumber":2,"pageSize":2}}
        """;

    [Fact]
    public void Paged_data_is_flattened_for_PaginatedApiResponse()
    {
        var r = Service().DeserializeResponse<PaginatedApiResponse<InvoiceResponse>>(Paged)!;

        Assert.True(r.Success);
        Assert.Equal(new[] { "INV-1", "INV-2" }, r.Data.Select(i => i.InvoiceNumber));
        Assert.Equal(7, r.TotalCount);
        Assert.Equal(2, r.PageNumber);
    }

    [Fact]
    public void Envelope_is_kept_for_ApiResponse_types()
    {
        var r = Service().DeserializeResponse<ApiResponse<PagedResult<InvoiceResponse>>>(Paged)!;

        Assert.Equal(2, r.Data!.Items.Count);
        Assert.Equal(7, r.Data.TotalCount);
    }

    [Fact]
    public void Bare_dto_is_unwrapped_from_the_envelope()
    {
        var r = Service().DeserializeResponse<InvoiceResponse>("""{"success":true,"data":{"id":"9","invoiceNumber":"INV-9"}}""")!;

        Assert.Equal("INV-9", r.InvoiceNumber);
    }

    [Fact]
    public void Non_envelope_json_is_read_as_is()
    {
        var r = Service().DeserializeResponse<InvoiceResponse>("""{"id":"3","invoiceNumber":"INV-3"}""")!;

        Assert.Equal("INV-3", r.InvoiceNumber);
    }
}
