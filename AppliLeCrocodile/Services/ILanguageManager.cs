namespace AppliLeCrocodile.Services;

public interface ILanguageManager
{
    IReadOnlyList<string> AvailableLanguages { get; }

    string CurrentLanguage { get; }

    Task InitializeAsync(string language, CancellationToken cancellationToken);

    Task SetLanguageAsync(string language, CancellationToken cancellationToken);

    string GetText(string textId);

    bool TryGetText(string textId, out string text);

    bool TryGetConstant(string constantId, out string value);
}
