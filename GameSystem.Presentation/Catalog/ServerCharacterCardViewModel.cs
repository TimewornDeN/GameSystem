using GameSystem.Presentation.Mvvm;

namespace GameSystem.Presentation.Catalog;

/// <summary>One card of the server catalog in the window: the data of the card plus its portrait, which arrives later.</summary>
public sealed class ServerCharacterCardViewModel(ServerCharacterCard card) : ObservableObject
{
    private byte[]? _portraitBytes;

    public ServerCharacterCard Card { get; } = card ?? throw new ArgumentNullException(nameof(card));

    /// <summary>The picture of the character; null while it is loading or when there is none (a placeholder is drawn then).</summary>
    public byte[]? PortraitBytes
    {
        get => _portraitBytes;
        set
        {
            if (SetProperty(ref _portraitBytes, value))
                OnPropertyChanged(nameof(HasPortrait));
        }
    }

    public bool HasPortrait => _portraitBytes is not null;
}
