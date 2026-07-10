using Microsoft.Extensions.Options;
using Neptune.Common;

namespace Neptune.QGISAPI.Services
{
    public class QgisService
    {
        private readonly ILogger<QgisService> _logger;
        private readonly QGISAPIConfiguration _configuration;

        public QgisService(ILogger<QgisService> logger, IOptions<QGISAPIConfiguration> configuration)
        {
            _logger = logger;
            _configuration = configuration.Value;
        }

        public ProcessUtilityResult Run(List<string> arguments)
        {
            const string exeFileName = "python3";
            var timeoutMs = (int)TimeSpan.FromMinutes(_configuration.QgisProcessTimeoutMinutes).TotalMilliseconds;
            var processUtilityResult = ProcessUtility.ShellAndWaitImpl(null, exeFileName, arguments, true, timeoutMs, new Dictionary<string, string>(), _logger);
            if (processUtilityResult.ReturnCode != 0)
            {
                var argumentsAsString = ProcessUtility.ConjoinCommandLineArguments(arguments);
                var fullProcessAndArguments =
                    $"{ProcessUtility.EncodeArgumentForCommandLine(exeFileName)} {argumentsAsString}";
                var errorMessage =
                    $"Process \"{exeFileName}\" returned with exit code {processUtilityResult.ReturnCode}, expected exit code 0.\r\n\r\nStdErr and StdOut:\r\n{processUtilityResult.StdOutAndStdErr}\r\n\r\nProcess Command Line:\r\n{fullProcessAndArguments}";
                throw new ApplicationException(errorMessage);
            }
            return processUtilityResult;
        }
    }
}
