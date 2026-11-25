using Microsoft.JSInterop;
using MudBlazor;
using OrthoFlowAI.Domain.Common;

namespace OrthoFlowAI.Web.Services;

/// <summary>Shape of GET /api/whatsapp/... (defined in the API project).</summary>
public class WhatsAppMessageDto
{
    public string PatientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ClickToChatUrl { get; set; }
    public bool CanSendAutomatically { get; set; }
}

/// <summary>Invoice PDF download and WhatsApp actions shared by the Invoices and Appointments pages.</summary>
public class PatientActions(IApiService api, IJSRuntime js, ISnackbar snackbar)
{
    public async Task DownloadInvoicePdfAsync(string invoiceId, string invoiceNumber)
    {
        await using var stream = await api.GetStreamAsync($"api/invoices/{invoiceId}/pdf");
        if (stream is null)
        {
            snackbar.Add("تعذّر تحميل الفاتورة", Severity.Error);
            return;
        }
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        await js.InvokeVoidAsync("orthoflowDownload", $"invoice-{invoiceNumber}.pdf",
            Convert.ToBase64String(ms.ToArray()), "application/pdf");
    }

    /// <summary>
    /// Sends automatically when a WhatsApp provider is configured; otherwise opens
    /// WhatsApp (wa.me) with the message ready so the receptionist just presses send.
    /// </summary>
    /// <param name="resource">"appointments" or "invoices".</param>
    public async Task WhatsAppAsync(string resource, string id)
    {
        var preview = await api.GetAsync<ApiResponse<WhatsAppMessageDto>>($"api/whatsapp/{resource}/{id}");
        var data = preview?.Data;
        if (preview?.Success != true || data is null)
        {
            snackbar.Add(preview?.Message ?? "مفيش رسالة واتساب للعنصر ده", Severity.Warning);
            return;
        }

        if (data.CanSendAutomatically)
        {
            var sent = await api.PostAsync<ApiResponse>($"api/whatsapp/{resource}/{id}/send");
            if (sent?.Success == true)
            {
                snackbar.Add($"اتبعتت رسالة واتساب لـ {data.PatientName}", Severity.Success);
                return;
            }
            snackbar.Add(sent?.Message ?? "الإرسال التلقائي فشل، هيتفتح واتساب", Severity.Warning);
        }

        if (string.IsNullOrEmpty(data.ClickToChatUrl))
        {
            snackbar.Add("رقم موبايل المريض مش صحيح", Severity.Error);
            return;
        }
        await js.InvokeVoidAsync("orthoflowOpen", data.ClickToChatUrl);
    }
}
