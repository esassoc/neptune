using System.Net.Http.Json;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.Common.Email;
using Neptune.Common.Services.GDAL;

namespace Neptune.Common.Services;

public class OverlayAPIService
{
    /// <summary>
    /// A HttpClient is registered in the Startup.cs file for this service.
    /// That is where the BaseUrl is set from the projects Configuration.
    /// </summary>
    private readonly HttpClient _httpClient;

    private readonly SitkaSmtpClientService _sitkaSmtpClient;
    private readonly ILogger<OverlayAPIService> _logger;
    private readonly SendGridConfiguration _sendGridConfiguration;

    public OverlayAPIService(ILogger<OverlayAPIService> logger, HttpClient httpClient, SitkaSmtpClientService sitkaSmtpClient, IOptions<SendGridConfiguration> options)
    {
        _logger = logger;
        _httpClient = httpClient;
        _sitkaSmtpClient = sitkaSmtpClient;
        _sendGridConfiguration = options.Value;
    }

    public async Task GenerateTGUs(GenerateTrashGeneratingUnitRequestDto generateLoadGeneratingUnitRequestDto)
    {
        _logger.LogInformation("Sending request to Overlay API");
        var response = await _httpClient.PostAsJsonAsync("/overlay/generate-tgus", generateLoadGeneratingUnitRequestDto);
        await GenerateImpl(response, "Neptune OverlayAPIService - Generate TGU Failed");
    }

    private async Task GenerateImpl(HttpResponseMessage response, string subject)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            _logger.LogError(message);
            var mailMessage = new MailMessage
            {
                Subject = subject,
                Body = $"Details: <br /><br />{message}",
                IsBodyHtml = true
            };

            mailMessage.To.Add(new MailAddress(_sendGridConfiguration.SitkaSupportEmail));

            await _sitkaSmtpClient.Send(mailMessage);
            throw new Exception(subject);
        }
    }

    public async Task GenerateLGUs(GenerateLoadGeneratingUnitRequestDto generateLoadGeneratingUnitRequestDto)
    {
        _logger.LogInformation("Sending request to Overlay API");
        var response = await _httpClient.PostAsJsonAsync("/overlay/generate-lgus", generateLoadGeneratingUnitRequestDto);
        await GenerateImpl(response, $"Neptune OverlayAPIService - Generate LGU Failed{(generateLoadGeneratingUnitRequestDto.LoadGeneratingUnitRefreshAreaID.HasValue ? $" - LoadGeneratingUnitRefreshAreaID {generateLoadGeneratingUnitRequestDto.LoadGeneratingUnitRefreshAreaID.Value}" : "")}");
    }

    public async Task GeneratePLGUs(GenerateProjectLoadGeneratingUnitRequestDto generateProjectLoadGeneratingUnitRequestDto)
    {
        _logger.LogInformation("Sending request to Overlay API");
        var response = await _httpClient.PostAsJsonAsync("/overlay/generate-plgus", generateProjectLoadGeneratingUnitRequestDto);
        await GenerateImpl(response, $"Neptune OverlayAPIService - Generate PLGU Failed - ProjectID {generateProjectLoadGeneratingUnitRequestDto.ProjectID}");
    }
}