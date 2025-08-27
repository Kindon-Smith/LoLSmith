using System.Net;
using System.Text;
using System.Text.Json;

namespace Utils;

public static class ApiResponseValidator
{
    public static void VerifyStatusCode(HttpResponseMessage res)
    {
        if (res.IsSuccessStatusCode) return;

        var status = (int)res.StatusCode;
        var reason = res.ReasonPhrase ?? "Error";

        // Try read body for more detail
        string body = "";
        try { body = res.Content.ReadAsStringAsync().GetAwaiter().GetResult(); } catch { }

        var msg = $"HTTP {(int)res.StatusCode} {reason}{(string.IsNullOrWhiteSpace(body) ? "" : $" | Body: {body}")}";
        throw new HttpRequestException(msg, null, res.StatusCode);
    }
}