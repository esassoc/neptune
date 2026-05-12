using System.Text.Json.Serialization;

namespace Neptune.Common.Recaptcha;

public class GoogleRecaptchaV3Response
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("challenge_ts")]
    public DateTime ChallengeTimestamp { get; set; }
    [JsonPropertyName("hostname")]
    public string HostName { get; set; }
    [JsonPropertyName("score")]
    public double Score { get; set; }
    [JsonPropertyName("action")]
    public string Action { get; set; }
}