using GameSystem.Core.Abstractions;

namespace GameSystem.Core.Models;

/// <summary>Simple displayable message (plain text with a display kind).</summary>
public sealed record Message(string Text, DisplayKind Kind = DisplayKind.Plain) : IDisplayable;
