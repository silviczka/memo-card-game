using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MemoCardGame.Client.Shared;

namespace MemoCardGame.Client.Pages;

public partial class Index : ComponentBase
{
    [Inject]
    public GameApiClient Api { get; set; } = default!;
    [Inject]
    public IJSRuntime Js { get; set; } = default!;

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
    private static readonly string[] AnimalAudioFiles =
    {
        "bird.mp3", "cat.mp3", "cockatiel.mp3", "dog.mp3",
        "hen.mp3", "peacock.mp3", "sheep.mp3", "volture.mp3"
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
    private Task? _warmupTask;
    private bool _audioMode;
    private bool _showAudioColorHints;

    private static readonly string[] AudioHintColors4 =
    {
        "#FF0000", "#FF8C00", "#FFFF00", "#7CFC00",
        "#00FFFF", "#1E90FF", "#8A2BE2", "#FF00FF"
    };

    private static readonly string[] AudioHintColors6 =
    {
        "#FF6B6B", "#FFB86C", "#E7FF6B",
        "#5EE27A", "#44E2FF", "#8F7CFF"
    };

    private static readonly string[] AudioHintShapes6 =
    {
        "circle", "triangle", "square"
    };

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

    private IReadOnlyList<IntSelectOption> CurrentBoardSizeOptions =>
        BoardRules.StartOptionsForMode(_audioMode ? "audio" : "image");

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            _warmupTask = WarmUpApiAsync();

        return Task.CompletedTask;
    }

    private async Task StartGame()
    {
        _error = "";
        try
        {
            if (_warmupTask is { IsCompleted: false })
                await _warmupTask;

            var playMode = _audioMode ? "audio" : "image";
            var state = await Api.StartGameAsync(_boardSize, _useMaxAttempts ? _maxAttempts : null, playMode);
            if (state != null)
            {
                _state = state;
                _gameId = state.Id;
                _activeBoardSize = state.BoardSize;
                try
                {
                    await Js.InvokeVoidAsync("memoCookies.set", CookieKeys.LastCohort, $"{playMode}:{state.BoardSize}", CookieKeys.DefaultDays);
                }
                catch
                {
                    // Saving the last-played cohort is optional; keep the game flow even if cookies are unavailable.
                }
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
            await PlayCardAudioAsync(cardId, _state);
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

    private async Task WarmUpApiAsync()
    {
        try
        {
            await Api.WarmUpAsync();
        }
        catch
        {
            // Warm-up is best-effort only. The real request path will still surface any errors.
        }
    }

    private Task OnAudioModeChanged(bool audioMode)
    {
        _audioMode = audioMode;

        var maxAudio = BoardRules.MaxSizeForMode("audio");
        if (_audioMode && _boardSize > maxAudio)
            _boardSize = maxAudio;

        return Task.CompletedTask;
    }

    private Task OnAudioHintsChanged(ChangeEventArgs e)
    {
        _showAudioColorHints = e.Value is bool b && b;
        return Task.CompletedTask;
    }

    private string PairColorHint(int? pairId)
    {
        if (pairId == null)
            return "#64748b";

        var boardSize = _state?.BoardSize ?? _activeBoardSize ?? _boardSize;
        var colors = boardSize switch
        {
            4 => AudioHintColors4,
            6 => AudioHintColors6,
            _ => AudioHintColors6
        };

        return colors[pairId.Value % colors.Length];
    }

    private string PairShapeHint(int? pairId)
    {
        if (pairId == null)
            return "circle";

        var boardSize = _state?.BoardSize ?? _activeBoardSize ?? _boardSize;
        if (boardSize != 6)
            return "circle";

        var normalizedPairId = Math.Abs(pairId.Value % (AudioHintColors6.Length * AudioHintShapes6.Length));
        return AudioHintShapes6[normalizedPairId / AudioHintColors6.Length];
    }

    private async Task PlayCardAudioAsync(Guid cardId, GameStateDto? state)
    {
        if (!_audioMode || state == null)
            return;

        var card = state.Cards.FirstOrDefault(c => c.Id == cardId);
        if (card?.PairId == null)
            return;

        var url = AudioFileUrl(card.PairId.Value, state.BoardSize);
        if (string.IsNullOrEmpty(url))
            return;

        try
        {
            await Js.InvokeVoidAsync("memoAudio.play", url);
        }
        catch
        {
            // Audio is best-effort and should never block gameplay.
        }
    }

    private static string AudioFileUrl(int pairId, int boardSize)
    {
        if (boardSize == 4)
        {
            var index = pairId % AnimalAudioFiles.Length;
            return $"/audio/animals/{AnimalAudioFiles[index]}";
        }

        if (boardSize == 6)
        {
            var index = (pairId % 18) + 1;
            return $"/audio/fx/{index}.mp3";
        }

        return "";
    }

    private static string EmojiImageUrl(int? pairId)
    {
        if (pairId == null)
            return "";

        var i = pairId.Value % EmojiCodepoints.Length;
        return $"{TwemojiBase}/{EmojiCodepoints[i]}.svg";
    }
}
