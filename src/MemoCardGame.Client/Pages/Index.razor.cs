using Microsoft.AspNetCore.Components;
using MemoCardGame.Client.Shared;

namespace MemoCardGame.Client.Pages;

public partial class Index : ComponentBase
{
    [Inject]
    public GameApiClient Api { get; set; } = default!;

    private static readonly IntSelectOption[] BoardSizeOptions =
    {
        new(4, "4×4 — classic"),
        new(6, "6×6 — challenge"),
        new(8, "8×8 — marathon")
    };

    private static readonly IntSelectOption[] MaxAttemptOptions =
    {
        new(10, "10"),
        new(15, "15"),
        new(20, "20"),
        new(30, "30")
    };

    private static readonly string[] EmojiCodepoints =
    {
        "1f436", "1f431", "1f42d", "1f439", "1f430", "1f98a", "1f43b", "1f43c", "1f428", "1f42f",
        "1f981", "1f42e", "1f437", "1f438", "1f435", "1f414", "1f427", "1f426", "1f424", "1f984",
        "1f434", "1f40d", "1f40e", "1f410", "1f411", "1f412", "1f416", "1f417", "1f418", "1f419",
        "1f41d", "1f41e"
    };

    private Guid? _gameId;
    private GameStateDto? _state;
    private string _error = "";
    private bool _waitingResolve;
    private List<(Guid Id, int PairId)> _mismatchOverlay = new();
    private CancellationTokenSource? _mismatchOverlayCts;
    private int _boardSize = 4;
    private int? _activeBoardSize;
    private bool _useMaxAttempts;
    private int _maxAttempts = 20;

    private const int MismatchDisplayMs = 3000;
    private const string TwemojiBase = "https://cdn.jsdelivr.net/gh/twitter/twemoji@14.0.2/assets/svg";

    private string CurrentTurnHint =>
        _waitingResolve ? "Resolving turn…" :
        _state?.FlippedCountThisTurn switch
        {
            0 => "Pick a card",
            1 => "Pick a second card",
            _ => "Turn complete"
        };

    private async Task StartGame()
    {
        _error = "";
        try
        {
            var res = await Api.StartGameAsync(_boardSize, _useMaxAttempts ? _maxAttempts : null);
            if (res != null)
            {
                _gameId = res.Id;
                _activeBoardSize = res.BoardSize;
                await LoadState();
            }
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private void NewGame()
    {
        _mismatchOverlayCts?.Cancel();
        _mismatchOverlayCts?.Dispose();
        _mismatchOverlayCts = null;
        _mismatchOverlay = new List<(Guid Id, int PairId)>();
        _gameId = null;
        _state = null;
        _activeBoardSize = null;
        _error = "";
    }

    private async Task LoadState()
    {
        if (_gameId == null)
            return;

        _error = "";
        try
        {
            _state = await Api.GetStateAsync(_gameId.Value);
            if (_state != null)
                _activeBoardSize = _state.BoardSize;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private async Task Flip(Guid cardId)
    {
        if (_gameId == null || _state?.IsFinished == true || _waitingResolve)
            return;

        if (_state?.FlippedCountThisTurn == 2)
            return;

        _error = "";
        if (_mismatchOverlay.Count > 0)
        {
            _mismatchOverlayCts?.Cancel();
            _mismatchOverlayCts?.Dispose();
            _mismatchOverlayCts = null;
            _mismatchOverlay = new List<(Guid Id, int PairId)>();
            StateHasChanged();
        }

        try
        {
            _state = await Api.FlipAsync(_gameId.Value, cardId);
            if (_state != null && _state.FlippedCountThisTurn == 2 && !_state.IsFinished)
                _ = ResolveAfterDelayAsync(_state);
        }
        catch (ApiException ex)
        {
            _error = ex.Message;
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
    }

    private async Task ResolveAfterDelayAsync(GameStateDto state)
    {
        _waitingResolve = true;
        try
        {
            await InvokeAsync(StateHasChanged);
            var flippedThisTurn = state.Cards.Where(c => c.IsFlipped && !c.IsMatched).ToList();
            var isMatch = flippedThisTurn.Count == 2 && flippedThisTurn[0].PairId == flippedThisTurn[1].PairId;

            if (_gameId == null)
                return;

            var resolved = await Api.ResolveAsync(_gameId.Value);
            _state = resolved ?? await Api.GetStateAsync(_gameId.Value);

            if (!isMatch && flippedThisTurn.Count == 2)
            {
                _mismatchOverlay = flippedThisTurn.Select(c => (c.Id, c.PairId!.Value)).ToList();
                _mismatchOverlayCts?.Cancel();
                _mismatchOverlayCts?.Dispose();
                _mismatchOverlayCts = new CancellationTokenSource();
                _ = HideMismatchAfterDelayAsync(MismatchDisplayMs, _mismatchOverlayCts.Token);
            }
        }
        catch
        {
            if (_gameId != null)
            {
                try { _state = await Api.GetStateAsync(_gameId.Value); } catch { }
            }
        }
        finally
        {
            _waitingResolve = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task HideMismatchAfterDelayAsync(int ms, CancellationToken ct)
    {
        try
        {
            await Task.Delay(ms, ct);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _mismatchOverlay = new List<(Guid Id, int PairId)>();
        await InvokeAsync(StateHasChanged);
    }

    private static string EmojiImageUrl(int? pairId)
    {
        if (pairId == null)
            return "";

        var i = pairId.Value % EmojiCodepoints.Length;
        return $"{TwemojiBase}/{EmojiCodepoints[i]}.svg";
    }
}
