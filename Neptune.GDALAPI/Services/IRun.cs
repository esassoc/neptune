using Neptune.Common;

namespace Neptune.GDALAPI.Services;

public interface IRun
{
    public ProcessUtilityResult Run(List<string> arguments);
}