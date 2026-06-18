using System.Net.Http.Headers;

namespace Neptune.Common.Services.GDAL;

public class GdbInputToGdbRequestDto
{
    public GdbInput GdbInput { get; set; }
    public string GdbName { get; set; }
}