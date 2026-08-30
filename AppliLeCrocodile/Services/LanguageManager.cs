using System.Collections.Frozen;
using System.Text.RegularExpressions;
using AppliLeCrocodile.Infrastructure.Assets;
using AppliLeCrocodile.Infrastructure.Serialization;

namespace AppliLeCrocodile.Services;

public sealed partial class LanguageManager : ILanguageManager
{
    public const string DefaultLanguage = "Français";

    private const string ConstantsPath = "Assets/Save/constantes.json";
    private const string LanguageDirectory = "Assets/Save/Language";

    private readonly JsonResourceReader resourceReader;
    private readonly SemaphoreSlim loadingGate = new SemaphoreSlim(1, 1);
    private FrozenDictionary<string, string> currentTexts = EmptyDictionary();
    private FrozenDictionary<string, string> defaultTexts = EmptyDictionary();
    private FrozenDictionary<string, string> constants = EmptyDictionary();

    public LanguageManager(IAssetReader assetReader)
    {
        resourceReader = new JsonResourceReader(assetReader);
    }

    public IReadOnlyList<string> AvailableLanguages { get; } = [DefaultLanguage, "English"];

    public string CurrentLanguage { get; private set; } = string.Empty;

    public async Task InitializeAsync(string language, CancellationToken cancellationToken)
    {
        await loadingGate.WaitAsync(cancellationToken);
        try
        {
            ConstantsFileDto constantsFile = await resourceReader.ReadAsync(
                ConstantsPath,
                JsonSerialization.Context.ConstantsFileDto,
                cancellationToken);
            constants = CreateDictionary(constantsFile.Constants, ConstantsPath);

            LanguageFileDto defaultLanguageFile = await ReadLanguageAsync(DefaultLanguage, cancellationToken);
            defaultTexts = CreateDictionary(defaultLanguageFile.Entries, GetLanguagePath(DefaultLanguage));
            ValidateConstants(defaultTexts, DefaultLanguage);

            string selectedLanguage = AvailableLanguages.Contains(language, StringComparer.Ordinal) ? language : DefaultLanguage;
            if (StringComparer.Ordinal.Equals(selectedLanguage, DefaultLanguage))
            {
                currentTexts = defaultTexts;
            }
            else
            {
                LanguageFileDto selectedLanguageFile = await ReadLanguageAsync(selectedLanguage, cancellationToken);
                currentTexts = CreateDictionary(selectedLanguageFile.Entries, GetLanguagePath(selectedLanguage));
                ValidateConstants(currentTexts, selectedLanguage);
            }

            CurrentLanguage = selectedLanguage;
        }
        finally
        {
            loadingGate.Release();
        }
    }

    public async Task SetLanguageAsync(string language, CancellationToken cancellationToken)
    {
        if (!AvailableLanguages.Contains(language, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(language), language, "The requested language is not available.");
        }

        if (StringComparer.Ordinal.Equals(CurrentLanguage, language))
        {
            return;
        }

        await loadingGate.WaitAsync(cancellationToken);
        try
        {
            LanguageFileDto languageFile = await ReadLanguageAsync(language, cancellationToken);
            FrozenDictionary<string, string> languageTexts = CreateDictionary(languageFile.Entries, GetLanguagePath(language));
            ValidateConstants(languageTexts, language);
            currentTexts = languageTexts;
            CurrentLanguage = language;
        }
        finally
        {
            loadingGate.Release();
        }
    }

    public string GetText(string textId)
    {
        if (TryGetText(textId, out string text))
        {
            return text;
        }

        throw new KeyNotFoundException(
            $"Text identifier '{textId}' was not found in language '{CurrentLanguage}' or fallback language '{DefaultLanguage}'.");
    }

    public bool TryGetText(string textId, out string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(textId);

        if (currentTexts.TryGetValue(textId, out string? currentText))
        {
            text = Resolve(currentText, textId);
            return true;
        }

        if (defaultTexts.TryGetValue(textId, out string? defaultText))
        {
            text = Resolve(defaultText, textId);
            return true;
        }

        text = string.Empty;
        return false;
    }

    public bool TryGetConstant(string constantId, out string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(constantId);

        if (constants.TryGetValue(constantId, out string? constantValue))
        {
            value = constantValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private async Task<LanguageFileDto> ReadLanguageAsync(string language, CancellationToken cancellationToken)
    {
        string path = GetLanguagePath(language);
        LanguageFileDto languageFile = await resourceReader.ReadAsync(path, JsonSerialization.Context.LanguageFileDto, cancellationToken);

        if (!StringComparer.Ordinal.Equals(languageFile.Language, language))
        {
            throw new InvalidDataException(
                $"Language asset '{path}' declares language '{languageFile.Language}' instead of '{language}'.");
        }

        return languageFile;
    }

    private string Resolve(string sourceText, string textId)
    {
        return ConstantPattern().Replace(sourceText, match =>
        {
            string constantId = match.Groups[1].Value;
            if (TryGetConstant(constantId, out string value))
            {
                return value;
            }

            throw new InvalidDataException($"Text identifier '{textId}' references unknown constant '{constantId}'.");
        });
    }

    private void ValidateConstants(IReadOnlyDictionary<string, string> texts, string language)
    {
        foreach (KeyValuePair<string, string> text in texts)
        {
            foreach (Match match in ConstantPattern().Matches(text.Value))
            {
                string constantId = match.Groups[1].Value;
                if (!constants.ContainsKey(constantId))
                {
                    throw new InvalidDataException(
                        $"Text identifier '{text.Key}' in language '{language}' references unknown constant '{constantId}'.");
                }
            }
        }
    }

    private static FrozenDictionary<string, string> CreateDictionary(TextEntryDto[] entries, string sourcePath)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>(entries.Length, StringComparer.Ordinal);
        foreach (TextEntryDto entry in entries)
        {
            AddEntry(dictionary, entry.Key, entry.Value, sourcePath);
        }

        return dictionary.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static FrozenDictionary<string, string> CreateDictionary(ConstantEntryDto[] entries, string sourcePath)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>(entries.Length, StringComparer.Ordinal);
        foreach (ConstantEntryDto entry in entries)
        {
            AddEntry(dictionary, entry.Key, entry.Value, sourcePath);
        }

        return dictionary.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static void AddEntry(Dictionary<string, string> dictionary, string key, string value, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidDataException($"Asset '{sourcePath}' contains an empty identifier.");
        }

        if (!dictionary.TryAdd(key, value))
        {
            throw new InvalidDataException($"Asset '{sourcePath}' contains duplicate identifier '{key}'.");
        }
    }

    private static string GetLanguagePath(string language)
    {
        return $"{LanguageDirectory}/{language}/texts.json";
    }

    private static FrozenDictionary<string, string> EmptyDictionary()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal).ToFrozenDictionary(StringComparer.Ordinal);
    }

    [GeneratedRegex(@"<([\w\d]+?)>", RegexOptions.CultureInvariant)]
    private static partial Regex ConstantPattern();
}
