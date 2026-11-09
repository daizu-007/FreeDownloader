using System.Text.RegularExpressions;

namespace FreeDownloader.Services;

public sealed record YtDlpOptions
{
    public const int MaxConcurrentFragments = 64;
    public const string DefaultFileNameTemplate = "%(title)s";

    public int ConcurrentFragments { get; init; } = 1;
    public string FileNameTemplate { get; init; } = DefaultFileNameTemplate;
    public bool EmbedThumbnail { get; init; }
    public bool EmbedSubtitles { get; init; }
    public bool EmbedChapters { get; init; }
    public bool EmbedMetadata { get; init; }
    public bool RemoveSponsorSegments { get; init; }

    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    private static readonly Regex TemplateFields = new(@"%%|%\([^()%$]+\)[#0+ \-]*[0-9]*(?:\.[0-9]+)?[diouxXeEfFgGcrsaljhqBUDS]");
    private static readonly Regex EnvironmentVariable = new(@"\$[a-zA-Z0-9_{-]");

    internal static bool IsValidFileNameTemplate(string template)
        => template.IndexOfAny(InvalidFileNameChars) < 0
            && !template.TrimStart().StartsWith('~')
            && !EnvironmentVariable.IsMatch(template)
            && !TemplateFields.Replace(template, string.Empty).Contains('%');
}
