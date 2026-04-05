using System.Net.Http.Json;
using System.Text.Json;
#if DEBUG
using System.Diagnostics;
#endif

namespace MemoCardGame.Client;

public class GameApiClient
{
    private readonly HttpClient _http;

    public GameApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task WarmUpAsync()
    {
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        using var res = await _http.GetAsync("/healthz");
        res.EnsureSuccessStatusCode();
#if DEBUG
        Console.WriteLine($"[perf] GET /healthz completed in {sw.ElapsedMilliseconds} ms");
#endif
    }

    public async Task<GameStateDto?> StartGameAsync(int boardSize = 4, int? maxAttempts = null, string playMode = "image")
    {
        var body = new { BoardSize = boardSize, MaxAttempts = maxAttempts, PlayMode = playMode };
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        var res = await _http.PostAsJsonAsync("/games", body);
        res.EnsureSuccessStatusCode();
        var state = await res.Content.ReadFromJsonAsync<GameStateDto>();
#if DEBUG
        Console.WriteLine($"[perf] POST /games completed in {sw.ElapsedMilliseconds} ms");
#endif
        return state;
    }

    public async Task<GameStateDto?> GetStateAsync(Guid gameId)
    {
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        var state = await _http.GetFromJsonAsync<GameStateDto>($"/games/{gameId}");
#if DEBUG
        Console.WriteLine($"[perf] GET /games/{gameId} completed in {sw.ElapsedMilliseconds} ms");
#endif
        return state;
    }

    public async Task<GameStateDto?> FlipAsync(Guid gameId, Guid cardId)
    {
        var body = new { CardId = cardId };
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        var res = await _http.PostAsJsonAsync($"/games/{gameId}/flip", body);
        if (!res.IsSuccessStatusCode)
            throw await CreateApiException(res);
        var state = await res.Content.ReadFromJsonAsync<GameStateDto>();
#if DEBUG
        Console.WriteLine($"[perf] POST /games/{gameId}/flip completed in {sw.ElapsedMilliseconds} ms");
#endif
        return state;
    }

    public async Task<GameStateDto?> ResolveAsync(Guid gameId)
    {
#if DEBUG
        var sw = Stopwatch.StartNew();
#endif
        var res = await _http.PostAsync($"/games/{gameId}/resolve", null);
        if (!res.IsSuccessStatusCode)
            throw await CreateApiException(res);
        var state = await res.Content.ReadFromJsonAsync<GameStateDto>();
#if DEBUG
        Console.WriteLine($"[perf] POST /games/{gameId}/resolve completed in {sw.ElapsedMilliseconds} ms");
#endif
        return state;
    }

    public async Task<LeaderboardPercentileDto?> GetLeaderboardPercentileAsync(Guid gameId)
    {
        var res = await _http.GetAsync($"/api/leaderboard/percentile/{gameId}");
        if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<LeaderboardPercentileDto>();
    }

    public async Task<IReadOnlyList<LeaderboardEntryDto>> GetLeaderboardAsync(string mode, int boardSize, int take = 50)
    {
        var url = $"/api/leaderboard/?mode={Uri.EscapeDataString(mode)}&boardSize={boardSize}&take={take}";
        var list = await _http.GetFromJsonAsync<List<LeaderboardEntryDto>>(url);
        return list ?? new List<LeaderboardEntryDto>();
    }

    public async Task<LeaderboardSubmitResponseDto> SubmitLeaderboardAsync(Guid gameId, string displayName)
    {
        var res = await _http.PostAsJsonAsync("/api/leaderboard/submit", new { gameId, displayName });
        var dto = await res.Content.ReadFromJsonAsync<LeaderboardSubmitResponseDto>();
        if (dto is null)
            return new LeaderboardSubmitResponseDto { Ok = false, Message = "Unexpected response from server." };
        if (!res.IsSuccessStatusCode)
            dto.Ok = false;
        return dto;
    }

    private static async Task<ApiException> CreateApiException(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        var message = body;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
            {
                var m = msgProp.GetString();
                if (!string.IsNullOrEmpty(m))
                    message = m;
            }
            if (doc.RootElement.TryGetProperty("error", out var errProp))
            {
                var e = errProp.GetString();
                if (!string.IsNullOrEmpty(e) && message == body)
                    message = e;
            }
        }
        catch
        {
            // Fall back to the raw response body when the payload is not valid JSON.
        }
        return new ApiException(message);
    }
}

public class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

public class GameStateDto
{
    public Guid Id { get; set; }
    public int BoardSize { get; set; }
    public string PlayMode { get; set; } = "image";
    public int Score { get; set; }
    public int MoveCount { get; set; }
    public int? MaxAttempts { get; set; }
    public bool IsFinished { get; set; }
    public bool AllPairsMatched { get; set; }
    public bool HasAttemptsLeft { get; set; }
    public int FlippedCountThisTurn { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public List<CardStateDto> Cards { get; set; } = new();
}

public class LeaderboardPercentileDto
{
    public decimal? Percentile { get; set; }
    public int SampleSize { get; set; }
    public string CohortLabel { get; set; } = "";
}

/// <summary>Leaderboard row from GET /api/leaderboard (JSON matches server <c>LeaderboardEntryDto</c>).</summary>
public class LeaderboardEntryDto
{
    public string DisplayName { get; set; } = "";
    public int Score { get; set; }
    public int MoveCount { get; set; }
    public DateTime SubmittedAt { get; set; }
}

public class LeaderboardSubmitResponseDto
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public bool EntryUpdated { get; set; }
}

public class CardStateDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public bool IsFlipped { get; set; }
    public bool IsMatched { get; set; }
    public int? PairId { get; set; }
}
