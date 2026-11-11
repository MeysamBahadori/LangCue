using System.Text.Json;
using System.Text.Json.Serialization;

namespace LangCue;

internal enum IndicatorPosition
{
    Above,
    Ahead,
    Behind,
    Below
}

internal sealed record IndicatorSettings
{
    public IndicatorPosition Position { get; init; } = IndicatorPosition.Above;
    public int Gap { get; init; } = 14;
    public int BackgroundOpacity { get; init; } = 78;
}

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LangCue",
        "settings.json");

    public static IndicatorSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new IndicatorSettings();
            }

            IndicatorSettings? settings = JsonSerializer.Deserialize<IndicatorSettings>(
                File.ReadAllText(FilePath), JsonOptions);

            if (settings is not null &&
                Enum.IsDefined(settings.Position) &&
                settings.Gap is >= 0 and <= 80 &&
                settings.BackgroundOpacity is >= 40 and <= 100)
            {
                return settings;
            }
        }
        catch (Exception exception) when (exception is IOException or
                                         UnauthorizedAccessException or JsonException)
        {
            // Invalid settings must not prevent the utility from starting.
        }

        return new IndicatorSettings();
    }

    public static void Save(IndicatorSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, JsonOptions));
    }
}
