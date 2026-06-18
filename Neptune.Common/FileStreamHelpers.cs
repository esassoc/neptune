using System.Buffers;
using System.IO.Pipelines;
using Microsoft.AspNetCore.Http;

namespace Neptune.Common;

public static class FileStreamHelpers
{

    public static async Task<byte[]> StreamToBytes(IFormFile stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}