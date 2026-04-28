using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Neptune.API.Services.AI;

public enum PromptTemplate
{
    ExtractWqmpFields,
    ExtractParcels,
    ExtractQuickBMPs,
    ExtractSourceControlBMPs
}

public interface IPromptTemplateService
{
    string Render(PromptTemplate template, object model, string version = "v1");
    string? GetRawTemplate(PromptTemplate template, string version = "v1");
}

public sealed class PromptTemplateService : IPromptTemplateService
{
    private readonly string _promptsDirectory;
    private readonly ConcurrentDictionary<string, string> _templateCache = new();
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)\}\}", RegexOptions.Compiled);

    public PromptTemplateService(string? promptsDirectory = null)
    {
        _promptsDirectory = promptsDirectory
            ?? Path.Combine(AppContext.BaseDirectory, "Services", "AI", "Prompts");
    }

    public string Render(PromptTemplate template, object model, string version = "v1")
    {
        var templateContent = GetRawTemplate(template, version)
            ?? throw new InvalidOperationException($"Prompt template '{template}_{version}' not found at {_promptsDirectory}.");

        var values = ExtractValues(model);
        return PlaceholderRegex.Replace(templateContent, match =>
        {
            var key = match.Groups[1].Value;
            return values.TryGetValue(key, out var value) ? value : match.Value;
        });
    }

    public string? GetRawTemplate(PromptTemplate template, string version = "v1")
    {
        var cacheKey = $"{template}_{version}";
        var result = _templateCache.GetOrAdd(cacheKey, _ => LoadTemplate(template.ToString(), version));
        return string.IsNullOrEmpty(result) ? null : result;
    }

    private string LoadTemplate(string templateName, string version)
    {
        var fileName = $"{templateName}_{version}.md";
        var filePath = Path.Combine(_promptsDirectory, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : string.Empty;
    }

    private static Dictionary<string, string> ExtractValues(object model)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (model is IDictionary<string, object> dict)
        {
            foreach (var kvp in dict)
                result[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
        }
        else if (model is IDictionary<string, string> stringDict)
        {
            foreach (var kvp in stringDict)
                result[kvp.Key] = kvp.Value ?? string.Empty;
        }
        else
        {
            foreach (var prop in model.GetType().GetProperties())
            {
                var value = prop.GetValue(model);
                result[prop.Name] = value?.ToString() ?? string.Empty;
            }
        }

        return result;
    }
}
