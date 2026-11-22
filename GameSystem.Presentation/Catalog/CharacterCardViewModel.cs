using GameSystem.Presentation.Mvvm;

namespace GameSystem.Presentation.Catalog;

/// <summary>One card of the catalog in the window: the data of the card plus its portrait, which arrives later.</summary>
public sealed class CharacterCardViewModel(CharacterCard card) : ObservableObject
{
    private CharacterCard _card = card ?? throw new ArgumentNullException(nameof(card));
    private byte[]? _portraitBytes;

    public CharacterCard Card
    {
        get => _card;
        set => SetProperty(ref _card, value ?? throw new ArgumentNullException(nameof(value)));
    }

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
