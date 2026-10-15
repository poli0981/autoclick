using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;
using System.Windows.Markup;

namespace AutoClick.UI.Localization;

/// <summary>
/// UI strings from Resources/Strings*.resx. XAML binds to the indexer (through
/// <see cref="LocExtension"/>), so switching the language updates the UI without a restart.
/// </summary>
public sealed class LocalizationService : INotifyPropertyChanged
{
    private readonly ResourceManager _resources = new("AutoClick.UI.Resources.Strings", typeof(LocalizationService).Assembly);

    private LocalizationService()
    {
    }

    public static LocalizationService Instance { get; } = new();

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("en");

    public string this[string key] => _resources.GetString(key, Culture) ?? key;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Raised after the UI language changed.</summary>
    public event EventHandler? LanguageChanged;

    public string Format(string key, params object?[] args) => string.Format(Culture, this[key], args);

    public void SetLanguage(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        if (Equals(culture, Culture))
        {
            return;
        }

        Culture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Culture)));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>Short alias for code: <c>L.Get("Common_Ok")</c>.</summary>
public static class L
{
    public static string Get(string key) => LocalizationService.Instance[key];

    public static string Format(string key, params object?[] args) => LocalizationService.Instance.Format(key, args);
}

/// <summary>XAML: <c>Text="{l:Loc Queue_AddGame}"</c> — a binding that follows language changes.</summary>
[MarkupExtensionReturnType(typeof(object))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension()
    {
    }

    public LocExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]") { Source = LocalizationService.Instance, Mode = BindingMode.OneWay }.ProvideValue(serviceProvider);
}
