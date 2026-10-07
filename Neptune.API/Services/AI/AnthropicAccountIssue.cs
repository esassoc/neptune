using System;
using Anthropic.Exceptions;

namespace Neptune.API.Services.AI;

/// <summary>
/// Classifies Anthropic failures caused by our account rather than the user's request — credit
/// balance exhausted, spend limit reached, API key revoked/forbidden. Callers show users
/// <see cref="UserFacingMessage"/> instead of Anthropic's billing text, and log
/// <see cref="LogMarker"/> in the rendered message for the neptune.tf Datadog monitor.
/// </summary>
public static class AnthropicAccountIssue
{
    public const string LogMarker = "Anthropic account issue";

    public const string UserFacingMessage =
        "AI extraction is temporarily unavailable. The OC Stormwater Tools team has been notified and is working to restore it. " +
        "Please try again later, or continue reviewing the WQMP manually.";

    public static bool IsAccountIssue(Exception ex)
    {
        switch (ex)
        {
            case AnthropicUnauthorizedException:
            case AnthropicForbiddenException:
                return true;
            // Billing failures come back as a 400 invalid_request_error; the message text is the
            // only discriminator ("Your credit balance is too low..." / "...reached your specified
            // API usage limits...").
            case AnthropicBadRequestException:
                return IsBillingMessage(ex.Message);
            // The PDF upload bypasses the SDK (AnthropicFileService), so its failures arrive as
            // this instead of the typed exceptions above. A 404 on POST /v1/files can't be a
            // per-document problem — the route exists for any key with Files API access — so
            // treat it like 401/403 (seen on QA 2026-10-07, req_011Cfo5cKM3hhaXhXzaN3Hhg).
            case AnthropicFileService.AnthropicFileUploadException upload:
                return upload.StatusCode is 401 or 403 or 404 || IsBillingMessage(upload.Message);
            default:
                return false;
        }
    }

    private static bool IsBillingMessage(string message)
    {
        message ??= string.Empty;
        return message.Contains("credit balance", StringComparison.OrdinalIgnoreCase)
               || message.Contains("usage limit", StringComparison.OrdinalIgnoreCase);
    }
}
