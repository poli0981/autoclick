using AutoClick.UI.Localization;

namespace AutoClick.UI.ViewModels;

/// <summary>A combo box choice: its value and the resource key of its (localized) label.</summary>
public sealed record Choice<T>(T Value, string LabelKey)
{
    /// <summary>The localized label (also what screen readers announce).</summary>
    public override string ToString() => L.Get(LabelKey);
}

/// <summary>A drag-and-drop move inside a list: the item at <see cref="From"/> goes to <see cref="To"/>.</summary>
public sealed record ReorderRequest(int From, int To);

/// <summary>A UI language, labelled in that language.</summary>
public sealed record LanguageOption(string Code, string NativeName)
{
    public override string ToString() => NativeName;
}
