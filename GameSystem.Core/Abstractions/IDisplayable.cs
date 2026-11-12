namespace GameSystem.Core.Abstractions;

/// <summary>Style hint for the displayer.</summary>
public enum DisplayKind
{
    Plain,
    Title,
    Info,
    Success,
    Warning,
    Error
}

/// <summary>Something that can be shown to the user.</summary>
public interface IDisplayable
{
    string Text { get; }
    DisplayKind Kind => DisplayKind.Plain;
}
