using System.Collections.ObjectModel;
using GameSystem.Core.Abstractions;
using GameSystem.Presentation.Mvvm;

namespace GameSystem.Presentation.Catalog;

/// <summary>
/// The catalog of characters: a list of cards. It keeps the cards in step with the snapshots of the game and
/// loads the portraits in the background (once per character: later cards take the picture from a cache).
/// All members must be called on the UI thread.
/// </summary>
public sealed class CatalogViewModel : ObservableObject
{
    private readonly IPortraitProvider? _portraits;
    private readonly Action<Action> _post;
    private readonly Dictionary<string, byte[]> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _requested = new(StringComparer.OrdinalIgnoreCase);

    // The server catalog has dozens of cards: the portraits are asked for a few at a time, not all at once.
    private readonly SemaphoreSlim _downloads = new(4);

    public CatalogViewModel(IPortraitProvider? portraits, Action<Action> post)
    {
        _portraits = portraits;
        _post = post ?? throw new ArgumentNullException(nameof(post));
    }

    public ObservableCollection<CharacterCardViewModel> Cards { get; } = [];

    public bool IsEmpty => Cards.Count == 0;

    /// <summary>Cards of every character the remote database offers (the downloaded catalog).</summary>
    public ObservableCollection<ServerCharacterCardViewModel> ServerCards { get; } = [];

    public bool ServerIsEmpty => ServerCards.Count == 0;

    /// <summary>Makes the list of server cards equal to <paramref name="cards"/>; nothing is touched when it is already so.</summary>
    public void UpdateServer(IReadOnlyList<ServerCharacterCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        if (ServerCards.Count == cards.Count && ServerCards.Zip(cards).All(pair => pair.First.Card == pair.Second))
            return;

        ServerCards.Clear();
        List<string> toLoad = [];
        foreach (var card in cards)
        {
            var view = new ServerCharacterCardViewModel(card);
            if (_cache.TryGetValue(card.Id, out var bytes))
                view.PortraitBytes = bytes;
            else
                toLoad.Add(card.Id);

            ServerCards.Add(view);
        }

        foreach (string id in toLoad)
            StartLoading(id);

        OnPropertyChanged(nameof(ServerIsEmpty));
    }

    /// <summary>Makes the list of cards equal to <paramref name="cards"/>, touching as little as possible.</summary>
    public void Update(IReadOnlyList<CharacterCard> cards)
    {
        ArgumentNullException.ThrowIfNull(cards);

        bool sameCharacters = Cards.Count == cards.Count
            && Cards.Zip(cards).All(pair => pair.First.Card.Name == pair.Second.Name && pair.First.Card.RemoteId == pair.Second.RemoteId);

        if (sameCharacters)
        {
            // Same characters: only their numbers changed (health, effects). Update in place, so the list does not jump.
            foreach (var (view, card) in Cards.Zip(cards))
            {
                if (view.Card != card)
                    view.Card = card;
            }

            return;
        }

        Cards.Clear();
        List<string> toLoad = [];
        foreach (var card in cards)
        {
            var view = new CharacterCardViewModel(card);
            if (card.RemoteId is { } remoteId)
            {
                if (_cache.TryGetValue(remoteId, out var bytes))
                    view.PortraitBytes = bytes;
                else
                    toLoad.Add(remoteId);
            }

            Cards.Add(view);
        }

        // Start loading only after every card is in the list, so a portrait that arrives at once finds its cards.
        foreach (string remoteId in toLoad)
            StartLoading(remoteId);

        OnPropertyChanged(nameof(IsEmpty));
    }

    private void StartLoading(string remoteId)
    {
        if (_portraits is null || !_requested.Add(remoteId))
            return;

        _ = LoadAsync(remoteId);
    }

    private async Task LoadAsync(string remoteId)
    {
        byte[]? bytes = null;
        await _downloads.WaitAsync();
        try
        {
            bytes = await _portraits!.GetPortraitAsync(remoteId);
        }
        catch (Exception)
        {
            // A portrait is decoration: any failure just leaves the placeholder.
        }
        finally
        {
            _downloads.Release();
        }

        if (bytes is null)
            return;

        _post(() =>
        {
            _cache[remoteId] = bytes;
            foreach (var view in Cards.Where(c => string.Equals(c.Card.RemoteId, remoteId, StringComparison.OrdinalIgnoreCase)))
                view.PortraitBytes = bytes;

            foreach (var view in ServerCards.Where(c => string.Equals(c.Card.Id, remoteId, StringComparison.OrdinalIgnoreCase)))
                view.PortraitBytes = bytes;
        });
    }
}
