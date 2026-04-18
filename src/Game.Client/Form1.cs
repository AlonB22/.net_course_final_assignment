using System.ComponentModel;
using Game.Client.Controls;
using Game.Client.Forms;
using Game.Client.Replay.Models;
using Game.Client.Replay.Services;
using Game.Client.Services;
using Game.Contracts.Enums;
using Game.Contracts.Models;

namespace Game.Client;

public partial class Form1 : Form
{
    private readonly IGameServerApiClient _gameServerApiClient;
    private readonly GameBoardControl _boardControl = new();
    private readonly AnnotationSurfaceControl _annotationSurface = new();
    private readonly ListBox _eventLog = new();
    private readonly Label _statusValueLabel = new();
    private readonly Label _remainingTimeValueLabel = new();
    private readonly Label _timerSelectionValueLabel = new();
    private readonly Label _selectionValueLabel = new();
    private readonly Label _sessionInfoValueLabel = new();
    private readonly Label _selectedPlayerIdValueLabel = new();
    private readonly Button _seconds2Button = new();
    private readonly Button _seconds5Button = new();
    private readonly Button _seconds10Button = new();
    private readonly Button _seconds15Button = new();
    private readonly Button _clearAnnotationsButton = new();
    private readonly Button _loadSessionButton = new();
    private readonly Button _refreshSessionButton = new();
    private readonly Button _submitMoveButton = new();
    private readonly Button _clearSelectionButton = new();
    private readonly Button _refreshReplayGamesButton = new();
    private readonly Button _playReplayButton = new();
    private readonly Button _stopReplayButton = new();
    private readonly TextBox _sessionIdTextBox = new();
    private readonly ComboBox _humanPlayerComboBox = new();
    private readonly ListBox _replayGamesListBox = new();
    private readonly System.Windows.Forms.Timer _countdownTimer = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new();
    private readonly System.Windows.Forms.Timer _winnerBlinkTimer = new();
    private readonly Panel _headerStrip = new();
    private readonly Panel _connectionIndicator = new();
    private readonly Label _connectionStatusValueLabel = new();
    private readonly Label _turnSummaryValueLabel = new();
    private readonly Panel _boardFrame = new();
    private readonly Panel _annotationFrame = new();
    private readonly Panel _rightSidebar = new();
    private readonly Label _replayDetailsValueLabel = new();
    private readonly Label _replayStatusValueLabel = new();

    private GameSessionDetailsDto? _activeSession;
    private readonly IReplayJournalService _replayJournalService;
    private readonly List<AnnotationStrokeSnapshot> _pendingAnnotationStrokes = [];
    private BoardCell? _selectedSourceCell;
    private BoardCell? _selectedDestinationCell;
    private int _selectedMoveTimeLimitSeconds = 10;
    private int _currentReplayTurnIndex;
    private int _remainingSeconds;
    private bool _winnerBlinkVisible;
    private DateTimeOffset? _winnerBlinkStopsAtUtc;
    private bool _isBusy;
    private bool _countdownTimeoutQueued;
    private bool _isReplayMode;
    private bool _isReplayPlaying;
    private CancellationTokenSource? _replayPlaybackCts;

    public Form1(IGameServerApiClient gameServerApiClient, IReplayJournalService replayJournalService)
    {
        _gameServerApiClient = gameServerApiClient;
        _replayJournalService = replayJournalService;
        InitializeComponent();
        InitializeShell();
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BoardSnapshotDto? CurrentBoard
    {
        get => _activeSession?.Board;
        private set
        {
            if (value is null)
            {
                return;
            }

            _activeSession = _activeSession is null
                ? new GameSessionDetailsDto(new SessionSummaryDto(Guid.Empty, DateTime.UtcNow, null, SessionStatus.Pending, GameOutcome.None, _selectedMoveTimeLimitSeconds), [], value, Guid.Empty)
                : _activeSession with { Board = value };

            _boardControl.SetSnapshot(value);
            UpdateTopLabels();
            RefreshActionAvailability();
        }
    }

    public void SetBoardSnapshot(BoardSnapshotDto snapshot)
    {
        CurrentBoard = snapshot;
        AddStatus($"Board updated. Turn: {snapshot.CurrentTurn}, status: {snapshot.Status}, outcome: {snapshot.Outcome}.");
    }

    public void SetConnectionStatus(string message, Color? accentColor = null)
    {
        _connectionStatusValueLabel.Text = message;
        _connectionIndicator.BackColor = accentColor ?? Color.FromArgb(57, 98, 160);
    }

    public void SetRemainingSeconds(int seconds)
    {
        _remainingSeconds = Math.Max(0, seconds);
        _remainingTimeValueLabel.Text = _remainingSeconds.ToString("00");
        _countdownTimer.Enabled = _remainingSeconds > 0;
    }

    public void StartMoveCountdown(int? seconds = null)
    {
        _countdownTimeoutQueued = false;
        SetRemainingSeconds(Math.Max(0, seconds ?? _selectedMoveTimeLimitSeconds));
        _countdownTimer.Start();
        AddStatus($"Countdown started at {_remainingSeconds} seconds.");
    }

    public void StopMoveCountdown()
    {
        _countdownTimer.Stop();
    }

    public void BeginWinnerBlink(PlayerSide winnerSide)
    {
        _winnerBlinkVisible = true;
        _winnerBlinkStopsAtUtc = DateTimeOffset.UtcNow.AddSeconds(5);
        _boardControl.SetWinnerBlink(winnerSide, true);
        _winnerBlinkTimer.Start();
    }

    public void StopWinnerBlink()
    {
        _winnerBlinkTimer.Stop();
        _winnerBlinkVisible = false;
        _winnerBlinkStopsAtUtc = null;
        _boardControl.SetWinnerBlink(null, false);
    }

    private void InitializeShell()
    {
        SuspendLayout();

        BackColor = Color.FromArgb(16, 23, 38);
        ForeColor = Color.Gainsboro;
        MinimumSize = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "DotNet Final Assignment Client";

        _countdownTimer.Interval = 1000;
        _countdownTimer.Tick += CountdownTimerOnTick;
        _animationTimer.Interval = 16;
        _animationTimer.Tick += AnimationTimerOnTick;
        _winnerBlinkTimer.Interval = 350;
        _winnerBlinkTimer.Tick += WinnerBlinkTimerOnTick;

        ConfigureHeader();
        ConfigurePanels();
        Controls.Add(BuildRootLayout());

        _boardControl.SetPlaceholderMessage("Load a session to render the board.");
        _boardControl.CellClicked += BoardControlOnCellClicked;
        _boardControl.ReadOnly = false;
        _annotationSurface.BackColor = Color.FromArgb(248, 249, 252);
        _annotationSurface.StrokeCommitted += AnnotationSurfaceOnStrokeCommitted;
        _annotationSurface.ReadOnly = false;

        CurrentBoard = new BoardSnapshotDto([], PlayerSide.Human, SessionStatus.Pending, GameOutcome.None);
        SetRemainingSeconds(_selectedMoveTimeLimitSeconds);
        SetConnectionStatus("Disconnected", Color.FromArgb(87, 94, 110));
        AddStatus("Client shell ready.");
        _ = RefreshReplayGamesAsync();

        ResumeLayout(true);
    }

    private void ConfigureHeader()
    {
        _headerStrip.Dock = DockStyle.Top;
        _headerStrip.Height = 88;
        _headerStrip.Padding = new Padding(18, 14, 18, 12);
        _headerStrip.BackColor = Color.FromArgb(20, 29, 49);

        var title = new Label { AutoSize = true, Text = "Game Client Shell", Font = new Font("Segoe UI", 18F, FontStyle.Bold), ForeColor = Color.White, Location = new Point(18, 18) };
        var subtitle = new Label { AutoSize = true, Text = "WinForms board renderer, move timers, and async API integration.", Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(190, 197, 211), Location = new Point(20, 50) };
        _connectionIndicator.Size = new Size(14, 14);
        _connectionIndicator.Location = new Point(962, 21);
        var connectionLabel = new Label { AutoSize = true, Text = "Connection", Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(190, 197, 211), Location = new Point(986, 15) };
        _connectionStatusValueLabel.AutoSize = true;
        _connectionStatusValueLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _connectionStatusValueLabel.ForeColor = Color.White;
        _connectionStatusValueLabel.Location = new Point(986, 33);
        _turnSummaryValueLabel.AutoSize = true;
        _turnSummaryValueLabel.Font = new Font("Segoe UI", 9.25F);
        _turnSummaryValueLabel.ForeColor = Color.FromArgb(220, 224, 233);
        _turnSummaryValueLabel.Location = new Point(986, 52);
        _statusValueLabel.AutoSize = true;
        _statusValueLabel.Font = new Font("Segoe UI", 8.5F);
        _statusValueLabel.ForeColor = Color.FromArgb(235, 235, 235);
        _statusValueLabel.Location = new Point(986, 71);
        _headerStrip.Controls.AddRange([title, subtitle, _connectionIndicator, connectionLabel, _connectionStatusValueLabel, _turnSummaryValueLabel, _statusValueLabel]);
    }

    private void ConfigurePanels()
    {
        _rightSidebar.Dock = DockStyle.Fill;
        _boardFrame.Dock = DockStyle.Fill;
        _boardFrame.BackColor = Color.FromArgb(19, 26, 43);
        _boardFrame.Padding = new Padding(16);
        _annotationFrame.Dock = DockStyle.Fill;
        _annotationFrame.BackColor = Color.FromArgb(24, 34, 55);
        _annotationFrame.Padding = new Padding(16, 0, 0, 0);
    }

    private Control BuildRootLayout()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.Controls.Add(_headerStrip, 0, 0);
        root.SetColumnSpan(_headerStrip, 2);
        root.Controls.Add(BuildBoardColumn(), 0, 1);
        root.Controls.Add(BuildSidebarColumn(), 1, 1);
        return root;
    }

    private Control BuildBoardColumn()
    {
        var column = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(18, 18, 10, 18) };
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 72F));
        column.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));

        var boardGroup = CreateGroupCard("Board Surface", "8x4 matrix renderer with cell hit testing");
        boardGroup.Controls.Add(_boardControl);

        var annotationGroup = CreateGroupCard("Annotation Surface", "Freehand drawing canvas with clear/reset support");
        annotationGroup.Controls.Add(BuildAnnotationTools());
        annotationGroup.Controls.Add(_annotationSurface);

        column.Controls.Add(boardGroup, 0, 0);
        column.Controls.Add(annotationGroup, 0, 1);
        return column;
    }

    private Control BuildSidebarColumn()
    {
        var sidebar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(10, 18, 18, 18) };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 235F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 150F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 220F));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        sidebar.Controls.Add(BuildSessionCard(), 0, 0);
        sidebar.Controls.Add(BuildTimerCard(), 0, 1);
        sidebar.Controls.Add(BuildActionCard(), 0, 2);
        sidebar.Controls.Add(BuildReplayCard(), 0, 3);
        sidebar.Controls.Add(BuildLogCard(), 0, 4);
        return sidebar;
    }

    private Control BuildSessionCard()
    {
        var card = CreateGroupCard("Session", "Load a session id and choose a registered human player");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(12, 10, 12, 12) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        grid.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = true, Text = "Session id", ForeColor = Color.FromArgb(220, 224, 233), Font = new Font("Segoe UI", 9F) }, 0, 0);
        grid.SetColumnSpan(grid.Controls[^1], 2);

        _sessionIdTextBox.Dock = DockStyle.Fill;
        _sessionIdTextBox.PlaceholderText = "Paste the session id from the website";
        _sessionIdTextBox.BackColor = Color.FromArgb(13, 18, 30);
        _sessionIdTextBox.ForeColor = Color.WhiteSmoke;
        _sessionIdTextBox.BorderStyle = BorderStyle.FixedSingle;
        grid.Controls.Add(_sessionIdTextBox, 0, 1);

        _loadSessionButton.Text = "Load Session";
        _loadSessionButton.Dock = DockStyle.Fill;
        _loadSessionButton.Click += async (_, _) => await LoadSessionAsync();

        _refreshSessionButton.Text = "Refresh";
        _refreshSessionButton.Dock = DockStyle.Fill;
        _refreshSessionButton.Click += async (_, _) => await RefreshSessionAsync();

        var buttonRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        buttonRow.Controls.Add(_loadSessionButton, 0, 0);
        buttonRow.Controls.Add(_refreshSessionButton, 1, 0);
        grid.Controls.Add(buttonRow, 1, 1);

        grid.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = true, Text = "Human player", ForeColor = Color.FromArgb(220, 224, 233), Font = new Font("Segoe UI", 9F) }, 0, 2);
        grid.SetColumnSpan(grid.Controls[^1], 2);

        _humanPlayerComboBox.Dock = DockStyle.Fill;
        _humanPlayerComboBox.DropDownStyle = ComboBoxStyle.DropDown;
        _humanPlayerComboBox.BackColor = Color.FromArgb(13, 18, 30);
        _humanPlayerComboBox.ForeColor = Color.WhiteSmoke;
        _humanPlayerComboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        _humanPlayerComboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        _humanPlayerComboBox.SelectedIndexChanged += (_, _) => UpdateSelectionLabels();
        _humanPlayerComboBox.TextChanged += (_, _) => UpdateSelectionLabels();
        grid.Controls.Add(_humanPlayerComboBox, 0, 3);
        grid.SetColumnSpan(_humanPlayerComboBox, 2);

        _selectedPlayerIdValueLabel.Text = "Human player id: none";
        _selectedPlayerIdValueLabel.Dock = DockStyle.Fill;
        _selectedPlayerIdValueLabel.ForeColor = Color.FromArgb(240, 196, 72);
        _selectedPlayerIdValueLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);

        _sessionInfoValueLabel.Text = "Session not loaded";
        _sessionInfoValueLabel.Dock = DockStyle.Fill;
        _sessionInfoValueLabel.ForeColor = Color.FromArgb(197, 205, 220);
        _sessionInfoValueLabel.Font = new Font("Segoe UI", 8.5F);

        grid.Controls.Add(_selectedPlayerIdValueLabel, 0, 4);
        grid.Controls.Add(_sessionInfoValueLabel, 1, 4);
        card.Controls.Add(grid);
        return card;
    }

    private Control BuildTimerCard()
    {
        var card = CreateGroupCard("Move Timer", "Choose the server move window before play starts");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(12, 10, 12, 12) };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

        grid.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = true, Text = "Selected window", ForeColor = Color.FromArgb(220, 224, 233), Font = new Font("Segoe UI", 9F) }, 0, 0);
        grid.SetColumnSpan(grid.Controls[^1], 2);
        _timerSelectionValueLabel.AutoSize = true;
        _timerSelectionValueLabel.Text = $"{_selectedMoveTimeLimitSeconds} seconds";
        _timerSelectionValueLabel.ForeColor = Color.White;
        _timerSelectionValueLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        grid.Controls.Add(_timerSelectionValueLabel, 0, 1);
        grid.SetColumnSpan(_timerSelectionValueLabel, 2);

        grid.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = true, Text = "Remaining", ForeColor = Color.FromArgb(220, 224, 233), Font = new Font("Segoe UI", 9F) }, 0, 2);
        _remainingTimeValueLabel.AutoSize = true;
        _remainingTimeValueLabel.Text = "00";
        _remainingTimeValueLabel.ForeColor = Color.FromArgb(240, 196, 72);
        _remainingTimeValueLabel.Font = new Font("Segoe UI", 20F, FontStyle.Bold);
        grid.Controls.Add(_remainingTimeValueLabel, 1, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        ConfigureTimerButton(_seconds2Button, "2", 2);
        ConfigureTimerButton(_seconds5Button, "5", 5);
        ConfigureTimerButton(_seconds10Button, "10", 10);
        ConfigureTimerButton(_seconds15Button, "15", 15);
        buttons.Controls.AddRange([_seconds2Button, _seconds5Button, _seconds10Button, _seconds15Button]);
        grid.Controls.Add(buttons, 0, 3);
        grid.SetColumnSpan(buttons, 2);
        card.Controls.Add(grid);
        return card;
    }

    private Control BuildActionCard()
    {
        var card = CreateGroupCard("Actions", "Select a source piece, then a destination");
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12, 10, 12, 12) };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        _submitMoveButton.Text = "Submit Move";
        _submitMoveButton.Dock = DockStyle.Fill;
        _submitMoveButton.Click += async (_, _) => await SubmitSelectedMoveAsync(false);

        _clearSelectionButton.Text = "Clear Selection";
        _clearSelectionButton.Dock = DockStyle.Fill;
        _clearSelectionButton.Click += (_, _) => ClearBoardSelection();

        _clearAnnotationsButton.Text = "Clear Annotation Surface";
        _clearAnnotationsButton.Dock = DockStyle.Fill;
        _clearAnnotationsButton.Click += (_, _) => _annotationSurface.ClearCanvas();

        _selectionValueLabel.Text = "Selection: none";
        _selectionValueLabel.Dock = DockStyle.Fill;
        _selectionValueLabel.ForeColor = Color.FromArgb(197, 205, 220);

        panel.Controls.Add(_submitMoveButton, 0, 0);
        panel.Controls.Add(_clearSelectionButton, 0, 1);
        panel.Controls.Add(_clearAnnotationsButton, 0, 2);
        panel.Controls.Add(_selectionValueLabel, 0, 3);
        card.Controls.Add(panel);
        return card;
    }

    private Control BuildLogCard()
    {
        var card = CreateGroupCard("Session Log", "User interactions and API results are echoed here");
        _eventLog.Dock = DockStyle.Fill;
        _eventLog.BackColor = Color.FromArgb(13, 18, 30);
        _eventLog.ForeColor = Color.WhiteSmoke;
        _eventLog.BorderStyle = BorderStyle.FixedSingle;
        _eventLog.IntegralHeight = false;
        _eventLog.Items.Add("Ready.");
        card.Controls.Add(_eventLog);
        return card;
    }

    private Control BuildReplayCard()
    {
        var card = CreateGroupCard("Local Replays", "Select a completed game and play it back read-only");
        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(12, 10, 12, 12) };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 24F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 102F));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var title = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Text = "Choose a local game from the replay database.",
            ForeColor = Color.FromArgb(220, 224, 233),
            Font = new Font("Segoe UI", 8.75F)
        };

        _replayGamesListBox.Dock = DockStyle.Fill;
        _replayGamesListBox.BackColor = Color.FromArgb(13, 18, 30);
        _replayGamesListBox.ForeColor = Color.WhiteSmoke;
        _replayGamesListBox.BorderStyle = BorderStyle.FixedSingle;
        _replayGamesListBox.IntegralHeight = false;
        _replayGamesListBox.SelectedIndexChanged += (_, _) => UpdateReplaySelectionLabels();

        var buttonRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));
        buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F));

        _refreshReplayGamesButton.Text = "Refresh";
        _refreshReplayGamesButton.Dock = DockStyle.Fill;
        _refreshReplayGamesButton.Click += async (_, _) => await RefreshReplayGamesAsync();

        _playReplayButton.Text = "Play Selected";
        _playReplayButton.Dock = DockStyle.Fill;
        _playReplayButton.Click += async (_, _) => await PlaySelectedReplayAsync();

        _stopReplayButton.Text = "Stop";
        _stopReplayButton.Dock = DockStyle.Fill;
        _stopReplayButton.Click += (_, _) => StopReplayPlayback();

        buttonRow.Controls.Add(_refreshReplayGamesButton, 0, 0);
        buttonRow.Controls.Add(_playReplayButton, 1, 0);
        buttonRow.Controls.Add(_stopReplayButton, 2, 0);

        _replayStatusValueLabel.Dock = DockStyle.Fill;
        _replayStatusValueLabel.ForeColor = Color.FromArgb(197, 205, 220);
        _replayStatusValueLabel.Font = new Font("Segoe UI", 8.5F);
        _replayStatusValueLabel.Text = "No replay selected.";

        _replayDetailsValueLabel.Dock = DockStyle.Fill;
        _replayDetailsValueLabel.ForeColor = Color.FromArgb(240, 196, 72);
        _replayDetailsValueLabel.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
        _replayDetailsValueLabel.Text = "Idle.";

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        footer.RowStyles.Add(new RowStyle(SizeType.Absolute, 18F));
        footer.Controls.Add(_replayStatusValueLabel, 0, 0);
        footer.Controls.Add(_replayDetailsValueLabel, 0, 1);

        grid.Controls.Add(title, 0, 0);
        grid.Controls.Add(_replayGamesListBox, 0, 1);
        grid.Controls.Add(buttonRow, 0, 2);
        grid.Controls.Add(footer, 0, 3);
        card.Controls.Add(grid);
        return card;
    }

    private Control BuildAnnotationTools()
    {
        var toolBar = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(0, 0, 0, 8) };
        toolBar.Controls.Add(new Label { Dock = DockStyle.Left, AutoSize = true, Text = "Drag with the mouse to sketch annotations.", ForeColor = Color.FromArgb(210, 214, 226), Font = new Font("Segoe UI", 8.75F) });
        var clearButton = new Button { Text = "Reset", Dock = DockStyle.Right, Width = 88, BackColor = Color.FromArgb(44, 58, 88), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        clearButton.FlatAppearance.BorderColor = Color.FromArgb(88, 101, 140);
        clearButton.Click += (_, _) => _annotationSurface.ClearCanvas();
        toolBar.Controls.Add(clearButton);
        return toolBar;
    }

    private Control CreateGroupCard(string title, string subtitle)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(24, 34, 55), Margin = new Padding(0, 0, 0, 12) };
        var header = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(31, 42, 66), Padding = new Padding(14, 10, 14, 8) };
        header.Controls.Add(new Label { Dock = DockStyle.Top, AutoSize = true, Text = title, ForeColor = Color.White, Font = new Font("Segoe UI", 11.5F, FontStyle.Bold) });
        header.Controls.Add(new Label { Dock = DockStyle.Bottom, AutoSize = true, Text = subtitle, ForeColor = Color.FromArgb(196, 202, 214), Font = new Font("Segoe UI", 8.5F) });
        card.Controls.Add(header);
        return card;
    }

    private void ConfigureTimerButton(Button button, string label, int seconds)
    {
        button.Text = label;
        button.Width = 52;
        button.Height = 32;
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.Margin = new Padding(0, 0, 8, 0);
        button.FlatAppearance.BorderColor = Color.FromArgb(88, 101, 140);
        button.BackColor = seconds == _selectedMoveTimeLimitSeconds ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        button.Click += (_, _) => SelectTimerWindow(seconds);
    }

    private void SelectTimerWindow(int seconds)
    {
        _selectedMoveTimeLimitSeconds = seconds;
        _timerSelectionValueLabel.Text = $"{seconds} seconds";
        _seconds2Button.BackColor = seconds == 2 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        _seconds5Button.BackColor = seconds == 5 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        _seconds10Button.BackColor = seconds == 10 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        _seconds15Button.BackColor = seconds == 15 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        SetRemainingSeconds(seconds);
        AddStatus($"Move timer window set to {seconds} seconds.");
    }

    private void UpdateTopLabels()
    {
        var board = CurrentBoard;
        if (board is null)
        {
            _turnSummaryValueLabel.Text = "No board loaded";
            _sessionInfoValueLabel.Text = "Session not loaded";
            return;
        }

        _turnSummaryValueLabel.Text = $"{board.Status} | {board.CurrentTurn} to move | {board.Outcome}";
        _sessionInfoValueLabel.Text = _activeSession is null
            ? "Session not loaded"
            : $"Session {_activeSession.Session.SessionId:D} | limit {_activeSession.Session.MoveTimeLimitSeconds}";
        UpdateSelectionLabels();
    }

    private void UpdateSelectionLabels()
    {
        if (_humanPlayerComboBox.SelectedItem is SessionPlayerItem player)
        {
            _selectedPlayerIdValueLabel.Text = $"Human player id: {player.PlayerId:D}";
            return;
        }

        if (Guid.TryParse(_humanPlayerComboBox.Text.Trim(), out var playerId))
        {
            _selectedPlayerIdValueLabel.Text = $"Human player id: {playerId:D}";
            return;
        }

        _selectedPlayerIdValueLabel.Text = "Human player id: none";
    }

    private void RefreshActionAvailability()
    {
        var board = CurrentBoard;
        var inProgress = board is not null && board.Status == SessionStatus.InProgress && board.CurrentTurn == PlayerSide.Human && !_isReplayMode;
        _refreshSessionButton.Enabled = _activeSession is not null && !_isBusy && !_isReplayPlaying;
        _submitMoveButton.Enabled = _activeSession is not null && inProgress && !_isBusy && !_isReplayPlaying;
        _loadSessionButton.Enabled = !_isBusy && !_isReplayPlaying;
        _clearSelectionButton.Enabled = !_isBusy && !_isReplayMode && !_isReplayPlaying;
        _humanPlayerComboBox.Enabled = _activeSession is not null && !_isBusy && !_isReplayMode && !_isReplayPlaying;
        _sessionIdTextBox.Enabled = !_isBusy && !_isReplayPlaying;
        _seconds2Button.Enabled = !_isBusy && !_isReplayMode && !_isReplayPlaying;
        _seconds5Button.Enabled = !_isBusy && !_isReplayMode && !_isReplayPlaying;
        _seconds10Button.Enabled = !_isBusy && !_isReplayMode && !_isReplayPlaying;
        _seconds15Button.Enabled = !_isBusy && !_isReplayMode && !_isReplayPlaying;
        _refreshReplayGamesButton.Enabled = !_isBusy && !_isReplayPlaying;
        _playReplayButton.Enabled = !_isBusy && !_isReplayPlaying && _replayGamesListBox.SelectedItem is ReplayGameListItem;
        _stopReplayButton.Enabled = _isReplayPlaying;
        _replayGamesListBox.Enabled = !_isBusy && !_isReplayPlaying;
        _boardControl.ReadOnly = _isReplayMode;
        _annotationSurface.ReadOnly = _isReplayMode || _isReplayPlaying;
    }

    private void ClearBoardSelection()
    {
        _selectedSourceCell = null;
        _selectedDestinationCell = null;
        _boardControl.ClearSelection();
        _selectionValueLabel.Text = "Selection: none";
        AddStatus("Selection cleared.");
    }

    private async Task LoadSessionAsync()
    {
        if (_isBusy)
        {
            AddStatus("Wait for the current operation to finish.");
            return;
        }

        ExitReplayMode();

        if (!ResolveRequestedSessionId(out var sessionId))
        {
            SetConnectionStatus("Missing session id", Color.FromArgb(209, 86, 70));
            AddStatus("Enter a valid session id before loading.");
            return;
        }

        await RunBusyOperationAsync($"Loading session {sessionId:D}...", async token =>
        {
            var details = await _gameServerApiClient.GetSessionAsync(sessionId, token);
            ApplySessionDetails(details);
            _currentReplayTurnIndex = await _replayJournalService.EnsureSessionAsync(details, token);
            _pendingAnnotationStrokes.Clear();
            await RefreshReplayGamesAsync(token);
            SetConnectionStatus("Session loaded", Color.FromArgb(76, 175, 80));
            AddStatus($"Loaded session {details.Session.SessionId:D}.");
        });
    }

    private async Task RefreshSessionAsync()
    {
        if (_activeSession is null)
        {
            await LoadSessionAsync();
            return;
        }

        _sessionIdTextBox.Text = _activeSession.Session.SessionId.ToString("D");
        await LoadSessionAsync();
    }

    private async Task SubmitSelectedMoveAsync(bool timeoutTriggered)
    {
        if (_isBusy || _activeSession is null || CurrentBoard is null || _isReplayMode)
        {
            return;
        }

        if (CurrentBoard.Status != SessionStatus.InProgress)
        {
            AddStatus("The current session is already complete.");
            return;
        }

        if (!timeoutTriggered && CurrentBoard.CurrentTurn != PlayerSide.Human)
        {
            AddStatus("It is not the human turn.");
            return;
        }

        if (!timeoutTriggered && (_selectedSourceCell is null || _selectedDestinationCell is null))
        {
            AddStatus("Select a source piece and destination cell first.");
            return;
        }

        if (!TryResolveHumanPlayerId(out var humanPlayerId))
        {
            AddStatus("Choose a registered human player before submitting.");
            return;
        }

        var fromCell = _selectedSourceCell ?? new BoardCell(0, 0);
        var toCell = _selectedDestinationCell ?? new BoardCell(0, 0);
        var remainingSeconds = timeoutTriggered ? 0 : Math.Max(0, _remainingSeconds);
        StopMoveCountdown();

        await RunBusyOperationAsync(timeoutTriggered ? "Submitting timeout..." : "Submitting move...", async token =>
        {
            var resolution = await _gameServerApiClient.SubmitMoveAsync(
                new MoveSubmissionDto(
                    _activeSession.Session.SessionId,
                    humanPlayerId,
                    fromCell.Row,
                    fromCell.Column,
                    toCell.Row,
                    toCell.Column,
                    remainingSeconds),
                token);

            await AnimateResolutionAsync(resolution);
            ApplyTurnResolution(resolution);
            if (resolution.ResultCode is MoveResultCode.Success or MoveResultCode.TimedOut)
            {
                try
                {
                    await _replayJournalService.RecordTurnAsync(_activeSession, _currentReplayTurnIndex, resolution, _pendingAnnotationStrokes.ToList(), token);
                    _currentReplayTurnIndex++;
                    _pendingAnnotationStrokes.Clear();
                    await RefreshReplayGamesAsync(token);
                }
                catch (Exception ex)
                {
                    AddStatus($"Replay save failed: {ex.Message}");
                }
            }
            AddStatus(resolution.Message);
        });
    }

    private async Task AnimateResolutionAsync(TurnResolutionDto resolution, CancellationToken cancellationToken = default)
    {
        var duration = TimeSpan.FromMilliseconds(650);
        if (resolution.HumanMove is not null)
        {
            _boardControl.BeginPieceAnimation(resolution.Board, resolution.HumanMove, duration);
            _animationTimer.Start();
            await Task.Delay(duration + TimeSpan.FromMilliseconds(120), cancellationToken);
        }

        if (resolution.ServerMove is not null)
        {
            _boardControl.BeginPieceAnimation(resolution.Board, resolution.ServerMove, duration);
            _animationTimer.Start();
            await Task.Delay(duration + TimeSpan.FromMilliseconds(120), cancellationToken);
        }
    }

    private void ApplySessionDetails(GameSessionDetailsDto details)
    {
        _activeSession = details;
        _sessionIdTextBox.Text = details.Session.SessionId.ToString("D");
        _selectedMoveTimeLimitSeconds = details.Session.MoveTimeLimitSeconds;
        UpdateTimerSelectionButtons();
        PopulatePlayers(details);
        CurrentBoard = details.Board;
        ClearBoardSelection();

        if (!_isReplayMode && details.Board.Status == SessionStatus.InProgress && details.Board.CurrentTurn == PlayerSide.Human)
        {
            StartMoveCountdown(details.Session.MoveTimeLimitSeconds);
        }
        else if (!_isReplayMode)
        {
            StopMoveCountdown();
        }

        if (details.Board.Status == SessionStatus.Completed || details.Board.Outcome != GameOutcome.None)
        {
            BeginWinnerBlink(ResolveWinnerSide(details.Board, details.Board.Outcome));
        }
    }

    private void ApplyTurnResolution(TurnResolutionDto resolution)
    {
        _activeSession = new GameSessionDetailsDto(
            resolution.Session,
            _activeSession?.Players ?? [],
            resolution.Board,
            _activeSession?.PrimaryHumanPlayerId ?? Guid.Empty);

        CurrentBoard = resolution.Board;
        ClearBoardSelection();
        UpdateTimerSelectionButtons(resolution.Session.MoveTimeLimitSeconds);

        if (resolution.Session.Status == SessionStatus.Completed || resolution.Board.Outcome != GameOutcome.None)
        {
            StopMoveCountdown();
            BeginWinnerBlink(ResolveWinnerSide(resolution.Board, resolution.Session.Outcome));
            return;
        }

        if (!_isReplayMode && resolution.Board.CurrentTurn == PlayerSide.Human)
        {
            StartMoveCountdown(resolution.Session.MoveTimeLimitSeconds);
        }
        else if (!_isReplayMode)
        {
            StopMoveCountdown();
        }
    }

    private void PopulatePlayers(GameSessionDetailsDto details)
    {
        _humanPlayerComboBox.BeginUpdate();
        _humanPlayerComboBox.Items.Clear();

        foreach (var player in details.Players.Select(player => new SessionPlayerItem(player, player.PlayerId == details.PrimaryHumanPlayerId)))
        {
            _humanPlayerComboBox.Items.Add(player);
        }

        var selected = _humanPlayerComboBox.Items.OfType<SessionPlayerItem>().FirstOrDefault(item => item.IsPrimary)
            ?? _humanPlayerComboBox.Items.OfType<SessionPlayerItem>().FirstOrDefault();
        _humanPlayerComboBox.SelectedItem = selected;
        _humanPlayerComboBox.EndUpdate();
        UpdateSelectionLabels();
    }

    private void UpdateTimerSelectionButtons(int? seconds = null)
    {
        var value = seconds ?? _selectedMoveTimeLimitSeconds;
        _timerSelectionValueLabel.Text = $"{value} seconds";
        _seconds2Button.BackColor = value == 2 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        _seconds5Button.BackColor = value == 5 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        _seconds10Button.BackColor = value == 10 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
        _seconds15Button.BackColor = value == 15 ? Color.FromArgb(89, 124, 204) : Color.FromArgb(44, 58, 88);
    }

    private bool ResolveRequestedSessionId(out Guid sessionId)
    {
        if (_activeSession is not null)
        {
            _sessionIdTextBox.Text = _activeSession.Session.SessionId.ToString("D");
        }

        return Guid.TryParse(_sessionIdTextBox.Text.Trim(), out sessionId);
    }

    private bool TryResolveHumanPlayerId(out Guid humanPlayerId)
    {
        if (_humanPlayerComboBox.SelectedItem is SessionPlayerItem selectedPlayer)
        {
            humanPlayerId = selectedPlayer.PlayerId;
            return true;
        }

        return Guid.TryParse(_humanPlayerComboBox.Text.Trim(), out humanPlayerId);
    }

    private void BoardControlOnCellClicked(object? sender, BoardCellClickedEventArgs e)
    {
        if (_isReplayMode)
        {
            AddStatus("Replay mode is read-only.");
            return;
        }

        if (_activeSession is null || CurrentBoard is null)
        {
            AddStatus("Load a session first.");
            return;
        }

        if (CurrentBoard.Status != SessionStatus.InProgress)
        {
            AddStatus("The current session is already complete.");
            return;
        }

        if (CurrentBoard.CurrentTurn != PlayerSide.Human)
        {
            AddStatus("It is not the human turn.");
            return;
        }

        var clicked = new BoardCell(e.Row, e.Column);
        if (_selectedSourceCell is null)
        {
            if (!TryGetPieceAt(clicked.Row, clicked.Column, out var piece) || piece.Side != PlayerSide.Human)
            {
                AddStatus("Select one of the human pieces as the source.");
                return;
            }

            _selectedSourceCell = clicked;
            _selectionValueLabel.Text = $"Selection: {CellName(clicked.Row, clicked.Column)}";
            _boardControl.SetSelection(_boardControl.CellBounds[clicked.Row, clicked.Column], null);
            AddStatus($"Source selected at {CellName(clicked.Row, clicked.Column)}.");
            return;
        }

        _selectedDestinationCell = clicked;
        _selectionValueLabel.Text = $"Selection: {CellName(_selectedSourceCell.Row, _selectedSourceCell.Column)} -> {CellName(clicked.Row, clicked.Column)}";
        _boardControl.SetSelection(
            _boardControl.CellBounds[_selectedSourceCell.Row, _selectedSourceCell.Column],
            _boardControl.CellBounds[clicked.Row, clicked.Column]);
        AddStatus($"Destination selected at {CellName(clicked.Row, clicked.Column)}.");
        _ = SubmitSelectedMoveAsync(false);
    }

    private bool TryGetPieceAt(int row, int column, out BoardPieceDto piece)
    {
        piece = CurrentBoard?.Pieces.FirstOrDefault(candidate => !candidate.IsCaptured && candidate.Row == row && candidate.Column == column)!;
        return piece is not null;
    }

    private void CountdownTimerOnTick(object? sender, EventArgs e)
    {
        if (_isReplayMode)
        {
            StopMoveCountdown();
            return;
        }

        if (_remainingSeconds <= 0)
        {
            _countdownTimer.Stop();
            if (!_countdownTimeoutQueued && CurrentBoard is not null && CurrentBoard.Status == SessionStatus.InProgress && CurrentBoard.CurrentTurn == PlayerSide.Human)
            {
                _countdownTimeoutQueued = true;
                _ = SubmitSelectedMoveAsync(true);
            }
            return;
        }

        _remainingSeconds--;
        _remainingTimeValueLabel.Text = _remainingSeconds.ToString("00");
        if (_remainingSeconds == 0)
        {
            _countdownTimer.Stop();
            AddStatus("Countdown reached zero.");
            if (!_countdownTimeoutQueued && CurrentBoard is not null && CurrentBoard.Status == SessionStatus.InProgress && CurrentBoard.CurrentTurn == PlayerSide.Human)
            {
                _countdownTimeoutQueued = true;
                _ = SubmitSelectedMoveAsync(true);
            }
        }
    }

    private void AnimationTimerOnTick(object? sender, EventArgs e)
    {
        _boardControl.AdvanceAnimationFrame(0);
        if (!_boardControl.IsAnimating)
        {
            _animationTimer.Stop();
        }
    }

    private void WinnerBlinkTimerOnTick(object? sender, EventArgs e)
    {
        if (_winnerBlinkStopsAtUtc is not null && DateTimeOffset.UtcNow >= _winnerBlinkStopsAtUtc)
        {
            StopWinnerBlink();
            return;
        }

        _winnerBlinkVisible = !_winnerBlinkVisible;
        _boardControl.ToggleWinnerBlink(_winnerBlinkVisible);
    }

    private async Task RunBusyOperationAsync(string connectionMessage, Func<CancellationToken, Task> action)
    {
        if (_isBusy)
        {
            AddStatus("Wait for the current operation to finish.");
            return;
        }

        _isBusy = true;
        RefreshActionAvailability();
        SetConnectionStatus(connectionMessage, Color.FromArgb(240, 196, 72));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            await action(cts.Token);
        }
        catch (KeyNotFoundException)
        {
            SetConnectionStatus("Session not found", Color.FromArgb(209, 86, 70));
            AddStatus("The requested session id was not found on the server.");
        }
        catch (HttpRequestException ex)
        {
            SetConnectionStatus("Server error", Color.FromArgb(209, 86, 70));
            AddStatus(ex.Message);
        }
        catch (OperationCanceledException)
        {
            SetConnectionStatus("Request timed out", Color.FromArgb(209, 86, 70));
            AddStatus("The request timed out before the server responded.");
        }
        catch (Exception ex)
        {
            SetConnectionStatus("Client error", Color.FromArgb(209, 86, 70));
            AddStatus(ex.Message);
        }
        finally
        {
            _isBusy = false;
            RefreshActionAvailability();
        }
    }

    private async Task RefreshReplayGamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var selectedSessionId = (_replayGamesListBox.SelectedItem as ReplayGameListItem)?.SessionId;
            var games = await _replayJournalService.GetGameSummariesAsync(cancellationToken);

            _replayGamesListBox.BeginUpdate();
            _replayGamesListBox.Items.Clear();
            foreach (var game in games)
            {
                _replayGamesListBox.Items.Add(game);
            }
            _replayGamesListBox.EndUpdate();

            if (selectedSessionId is not null)
            {
                var sameGame = _replayGamesListBox.Items.OfType<ReplayGameListItem>().FirstOrDefault(game => game.SessionId == selectedSessionId.Value);
                if (sameGame is not null)
                {
                    _replayGamesListBox.SelectedItem = sameGame;
                }
            }

            UpdateReplaySelectionLabels();
        }
        catch (Exception ex)
        {
            _replayStatusValueLabel.Text = "Replay database not ready.";
            AddStatus(ex.Message);
        }
    }

    private void UpdateReplaySelectionLabels()
    {
        if (_replayGamesListBox.SelectedItem is ReplayGameListItem replayGame)
        {
            _replayStatusValueLabel.Text = $"Selected replay: {replayGame.SessionId:D}";
            _replayDetailsValueLabel.Text = $"{replayGame.Status} | {replayGame.Outcome} | {replayGame.TurnCount} turns";
        }
        else
        {
            _replayStatusValueLabel.Text = "No replay selected.";
            _replayDetailsValueLabel.Text = "Idle.";
        }

        RefreshActionAvailability();
    }

    private async Task PlaySelectedReplayAsync()
    {
        if (_isBusy || _isReplayPlaying)
        {
            return;
        }

        if (_replayGamesListBox.SelectedItem is not ReplayGameListItem replayGame)
        {
            AddStatus("Choose a local replay first.");
            return;
        }

        var playback = await _replayJournalService.GetPlaybackAsync(replayGame.ReplayGameId);
        if (playback is null)
        {
            AddStatus("The selected replay could not be loaded.");
            return;
        }

        _replayPlaybackCts?.Cancel();
        _replayPlaybackCts?.Dispose();
        _replayPlaybackCts = new CancellationTokenSource();

        _isReplayMode = true;
        _isReplayPlaying = true;
        _boardControl.ReadOnly = true;
        _annotationSurface.ReadOnly = true;
        RefreshActionAvailability();
        SetConnectionStatus("Playing replay", Color.FromArgb(240, 196, 72));

        try
        {
            _activeSession = playback.InitialDetails;
            _currentReplayTurnIndex = playback.Turns.Count;
            ApplySessionDetails(playback.InitialDetails);

            foreach (var turn in playback.Turns.OrderBy(turn => turn.TurnIndex))
            {
                _replayPlaybackCts.Token.ThrowIfCancellationRequested();

                var strokesForTurn = playback.Strokes
                    .Where(stroke => stroke.TurnIndex <= turn.TurnIndex)
                    .Select(stroke => stroke.Stroke);
                _annotationSurface.LoadStrokes(strokesForTurn);

                await AnimateResolutionAsync(turn.Resolution, _replayPlaybackCts.Token);
                ApplyTurnResolution(turn.Resolution);
                await Task.Delay(TimeSpan.FromMilliseconds(1200), _replayPlaybackCts.Token);
            }

            SetConnectionStatus("Replay complete", Color.FromArgb(76, 175, 80));
            AddStatus($"Finished replay for session {playback.Game.SessionId:D}.");
        }
        catch (OperationCanceledException)
        {
            SetConnectionStatus("Replay stopped", Color.FromArgb(87, 94, 110));
            AddStatus("Replay playback stopped.");
        }
        catch (Exception ex)
        {
            SetConnectionStatus("Replay error", Color.FromArgb(209, 86, 70));
            AddStatus(ex.Message);
        }
        finally
        {
            _isReplayPlaying = false;
            _replayPlaybackCts?.Dispose();
            _replayPlaybackCts = null;
            RefreshActionAvailability();
        }
    }

    private void StopReplayPlayback()
    {
        _replayPlaybackCts?.Cancel();
    }

    private void ExitReplayMode()
    {
        _isReplayMode = false;
        _isReplayPlaying = false;
        _boardControl.ReadOnly = false;
        _annotationSurface.ReadOnly = false;
        _replayPlaybackCts?.Cancel();
        RefreshActionAvailability();
    }

    private void AnnotationSurfaceOnStrokeCommitted(object? sender, AnnotationStrokeCommittedEventArgs e)
    {
        if (_isReplayMode || _activeSession is null)
        {
            return;
        }

        var stroke = new AnnotationStrokeSnapshot(
            _currentReplayTurnIndex,
            _pendingAnnotationStrokes.Count,
            e.StrokeColor.ToArgb(),
            e.StrokeWidth,
            e.Points.Select(point => new AnnotationPointSnapshot(point.X, point.Y)).ToArray());

        _pendingAnnotationStrokes.Add(stroke);
        _statusValueLabel.Text = $"Captured annotation stroke #{stroke.StrokeIndex + 1} for turn {_currentReplayTurnIndex + 1}.";
    }

    private void AddStatus(string message)
    {
        if (InvokeRequired)
        {
            BeginInvoke((MethodInvoker)(() => AddStatus(message)));
            return;
        }

        _statusValueLabel.Text = message;
        _eventLog.Items.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");
        if (_eventLog.Items.Count > 100)
        {
            _eventLog.Items.RemoveAt(_eventLog.Items.Count - 1);
        }
    }

    private static string CellName(int row, int column)
        => $"{(char)('A' + column)}{row + 1}";

    private static PlayerSide ResolveWinnerSide(BoardSnapshotDto board, GameOutcome outcome)
        => outcome switch
        {
            GameOutcome.HumanVictory => PlayerSide.Human,
            GameOutcome.ServerVictory => PlayerSide.Server,
            GameOutcome.HumanTimeoutLoss => PlayerSide.Server,
            GameOutcome.ReachedBackRankVictory => board.CurrentTurn == PlayerSide.Human ? PlayerSide.Server : PlayerSide.Human,
            GameOutcome.BlockedOpponentVictory => board.CurrentTurn == PlayerSide.Human ? PlayerSide.Server : PlayerSide.Human,
            _ => board.CurrentTurn == PlayerSide.Human ? PlayerSide.Server : PlayerSide.Human
        };

    private sealed record BoardCell(int Row, int Column);
}
