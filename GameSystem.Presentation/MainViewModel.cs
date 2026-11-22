using System.Collections.ObjectModel;
using System.Windows.Input;
using GameSystem.Core.Abstractions;
using GameSystem.Presentation.Catalog;
using GameSystem.Presentation.Console;
using GameSystem.Presentation.Mvvm;

namespace GameSystem.Presentation;

/// <summary>
/// The view model of the main window (the "ViewModel" of MVVM). The window binds to its properties and commands and
/// contains no logic of its own. It connects three things: the output field (lines of the <see cref="GuiConsole"/>),
/// the controls that send commands (input line, buttons, saves) and the catalog of characters.
/// </summary>
public sealed class MainViewModel : ObservableObject, IDisposable
{
    private const int MaxLines = 2000;

    private readonly GuiConsole _console;
    private readonly GameHost _host;
    private readonly Action<Action> _post;

    private string _inputText = string.Empty;
    private string _prompt = GameHost.MainPrompt;
    private string _saveName = string.Empty;
    private string? _selectedSave;
    private string _battleSummary = string.Empty;
    private string _remoteId = string.Empty;
    private string _remoteName = string.Empty;
    private PromptMode _mode = PromptMode.Busy;

    /// <param name="post">
    /// Runs an action on the UI thread. By default the synchronization context that exists when the view model is
    /// created (the window's dispatcher) is used; tests pass a function that just calls the action.
    /// </param>
    public MainViewModel(GameHost host, GuiConsole console, IPortraitProvider? portraits = null, Action<Action>? post = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _post = post ?? CreateDefaultPost();

        Catalog = new CatalogViewModel(portraits, _post);

        SendCommand = new RelayCommand(Send);
        QuickCommand = new RelayCommand(parameter => SubmitCommand(parameter as string));
        SaveGameCommand = new RelayCommand(SaveGame);
        LoadGameCommand = new RelayCommand(LoadGame);
        ContinueGameCommand = new RelayCommand(ContinueGame);
        AddRemoteCommand = new RelayCommand(AddRemote);

        // Subscribe before the game starts, so not a single line is lost.
        _console.LineWritten += OnLineWritten;
        _host.SnapshotReady += OnSnapshotReady;
    }

    public ObservableCollection<ConsoleLine> Lines { get; } = [];

    public ObservableCollection<string> Saves { get; } = [];

    public CatalogViewModel Catalog { get; }

    public ICommand SendCommand { get; }

    /// <summary>Sends the command given as the command parameter (the buttons of the quick bars).</summary>
    public ICommand QuickCommand { get; }

    public ICommand SaveGameCommand { get; }

    public ICommand LoadGameCommand { get; }

    public ICommand ContinueGameCommand { get; }

    /// <summary>Creates a character from the remote database: <c>remote create &lt;id&gt; &lt;name&gt;</c>.</summary>
    public ICommand AddRemoteCommand { get; }

    public string InputText
    {
        get => _inputText;
        set => SetProperty(ref _inputText, value ?? string.Empty);
    }

    /// <summary>What the game asks for now, shown next to the input line: "&gt; ", "[Grosh] &gt; " or the question of a dialog.</summary>
    public string Prompt
    {
        get => _prompt;
        private set => SetProperty(ref _prompt, value);
    }

    public string SaveName
    {
        get => _saveName;
        set
        {
            if (SetProperty(ref _saveName, value ?? string.Empty))
                OnPropertyChanged(nameof(CanSave));
        }
    }

    public string? SelectedSave
    {
        get => _selectedSave;
        set
        {
            if (!SetProperty(ref _selectedSave, value)) return;

            // Choosing a save in the list also fills the name box, so 'Save' overwrites that slot on purpose.
            if (value is not null)
                SaveName = value;

            OnPropertyChanged(nameof(CanLoad));
        }
    }

    /// <summary>Id of a character in the remote database (for example "albedo").</summary>
    public string RemoteId
    {
        get => _remoteId;
        set
        {
            if (SetProperty(ref _remoteId, value ?? string.Empty))
                OnPropertyChanged(nameof(CanAddRemote));
        }
    }

    /// <summary>Name the new character gets in the game; the remote id is used when it is empty.</summary>
    public string RemoteName
    {
        get => _remoteName;
        set => SetProperty(ref _remoteName, value ?? string.Empty);
    }

    /// <summary>One line about the battle in progress (empty when there is none).</summary>
    public string BattleSummary
    {
        get => _battleSummary;
        private set
        {
            if (SetProperty(ref _battleSummary, value))
                OnPropertyChanged(nameof(HasBattle));
        }
    }

    public bool HasBattle => _battleSummary.Length > 0;

    public PromptMode Mode
    {
        get => _mode;
        private set
        {
            if (!SetProperty(ref _mode, value)) return;

            OnPropertyChanged(nameof(CanSend));
            OnPropertyChanged(nameof(CanSave));
            OnPropertyChanged(nameof(CanLoad));
            OnPropertyChanged(nameof(CanContinue));
            OnPropertyChanged(nameof(CanAddRemote));
            OnPropertyChanged(nameof(IsMain));
            OnPropertyChanged(nameof(IsBattle));
        }
    }

    public bool CanSend => _mode != PromptMode.Busy;

    public bool IsMain => _mode == PromptMode.Main;

    public bool IsBattle => _mode == PromptMode.Battle;

    /// <summary>A game can be saved from the main prompt and in the middle of a battle.</summary>
    public bool CanSave => (_mode is PromptMode.Main or PromptMode.Battle) && !string.IsNullOrWhiteSpace(_saveName);

    public bool CanLoad => _mode == PromptMode.Main && _selectedSave is not null;

    public bool CanAddRemote => _mode == PromptMode.Main && !string.IsNullOrWhiteSpace(_remoteId);

    public bool CanContinue => _mode == PromptMode.Main && Saves.Count > 0;

    public void Dispose()
    {
        _console.LineWritten -= OnLineWritten;
        _host.SnapshotReady -= OnSnapshotReady;
    }

    private void Send()
    {
        string text = InputText;
        InputText = string.Empty;

        // An empty line is a valid answer to a dialog question, so it is sent as well.
        Submit(text);
    }

    private void SubmitCommand(string? command)
    {
        if (!string.IsNullOrWhiteSpace(command))
            Submit(command);
    }

    /// <summary>
    /// Gives the game one line and switches to Busy until the game asks for the next one, so a double click on
    /// a button cannot send the same action twice.
    /// </summary>
    private void Submit(string line)
    {
        if (!CanSend) return;

        _console.Submit(line);
        Mode = PromptMode.Busy;
    }

    private void SaveGame()
    {
        // The parser splits a line at spaces, so a save name cannot contain them.
        string name = string.Join('_', SaveName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (name.Length > 0 && CanSave)
            Submit($"save {name}");
    }

    private void LoadGame()
    {
        if (SelectedSave is { } name && CanLoad)
            Submit($"load {name}");
    }

    private void ContinueGame()
    {
        if (!CanContinue) return;

        Submit(SelectedSave is { } name ? $"continue {name}" : "continue");
    }

    private void AddRemote()
    {
        string id = RemoteId.Trim();
        string name = string.Join('_', (string.IsNullOrWhiteSpace(RemoteName) ? id : RemoteName).Split(' ', StringSplitOptions.RemoveEmptyEntries));

        if (id.Length > 0 && !id.Contains(' ') && CanAddRemote)
            Submit($"remote create {id} {name}");
    }

    private void OnLineWritten(ConsoleLine line) => _post(() =>
    {
        Lines.Add(line);
        while (Lines.Count > MaxLines)
            Lines.RemoveAt(0);
    });

    private void OnSnapshotReady(GameSnapshot snapshot) => _post(() =>
    {
        Prompt = snapshot.Prompt;
        BattleSummary = snapshot.BattleSummary;

        if (!Saves.SequenceEqual(snapshot.Saves))
        {
            string? keep = SelectedSave is { } s && snapshot.Saves.Contains(s) ? s : null;
            Saves.Clear();
            foreach (string name in snapshot.Saves)
                Saves.Add(name);

            _selectedSave = keep;
            OnPropertyChanged(nameof(SelectedSave));
            OnPropertyChanged(nameof(CanLoad));
            OnPropertyChanged(nameof(CanContinue));
        }

        Catalog.Update(snapshot.Cards);
        Mode = snapshot.Mode;
    });

    private static Action<Action> CreateDefaultPost()
    {
        var context = SynchronizationContext.Current;
        return context is null ? action => action() : action => context.Post(_ => action(), null);
    }
}
