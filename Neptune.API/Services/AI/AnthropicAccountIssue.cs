using System;
using Anthropic.Exceptions;

namespace Neptune.API.Services.AI;

/// <summary>
/// Classifies Anthropic failures caused by our account rather than the user's request — credit
/// balance exhausted, spend limit reached, API key revoked/forbidden. Callers show users
/// <see cref="UserFacingMessage"/> instead of Anthropic's billing text.
/// </summary>
public static class AnthropicAccountIssue
{
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
                var message = ex.Message ?? string.Empty;
                return message.Contains("credit balance", StringComparison.OrdinalIgnoreCase)
                       || message.Contains("usage limit", StringComparison.OrdinalIgnoreCase);
            default:
                return false;
        }
    }
}
