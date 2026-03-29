using System.Net.Http.Json;
using System.Diagnostics;
using System.Text.Json;

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
        var sw = Stopwatch.StartNew();
        using var res = await _http.GetAsync("/healthz");
        res.EnsureSuccessStatusCode();
        Console.WriteLine($"[perf] GET /healthz completed in {sw.ElapsedMilliseconds} ms");
    }

    public async Task<GameStateDto?> StartGameAsync(int boardSize = 4, int? maxAttempts = null)
    {
        var body = new { BoardSize = boardSize, MaxAttempts = maxAttempts };
        var sw = Stopwatch.StartNew();
        var res = await _http.PostAsJsonAsync("/games", body);
        res.EnsureSuccessStatusCode();
        var state = await res.Content.ReadFromJsonAsync<GameStateDto>();
        Console.WriteLine($"[perf] POST /games completed in {sw.ElapsedMilliseconds} ms");
        return state;
    }

    public async Task<GameStateDto?> GetStateAsync(Guid gameId)
    {
        var sw = Stopwatch.StartNew();
        var state = await _http.GetFromJsonAsync<GameStateDto>($"/games/{gameId}");
        Console.WriteLine($"[perf] GET /games/{gameId} completed in {sw.ElapsedMilliseconds} ms");
        return state;
    }

    public async Task<GameStateDto?> FlipAsync(Guid gameId, Guid cardId)
    {
        var body = new { CardId = cardId };
        var sw = Stopwatch.StartNew();
        var res = await _http.PostAsJsonAsync($"/games/{gameId}/flip", body);
        if (!res.IsSuccessStatusCode)
            throw await CreateApiException(res);
        var state = await res.Content.ReadFromJsonAsync<GameStateDto>();
        Console.WriteLine($"[perf] POST /games/{gameId}/flip completed in {sw.ElapsedMilliseconds} ms");
        return state;
    }

    public async Task<GameStateDto?> ResolveAsync(Guid gameId)
    {
        var sw = Stopwatch.StartNew();
        var res = await _http.PostAsync($"/games/{gameId}/resolve", null);
        if (!res.IsSuccessStatusCode)
            throw await CreateApiException(res);
        var state = await res.Content.ReadFromJsonAsync<GameStateDto>();
        Console.WriteLine($"[perf] POST /games/{gameId}/resolve completed in {sw.ElapsedMilliseconds} ms");
        return state;
    }

    private static async Task<ApiException> CreateApiException(HttpResponseMessage res)
    {
        var body = await res.Content.ReadAsStringAsync();
        var message = body;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errProp))
                message = errProp.GetString() ?? body;
        }
        catch
        {
            // use raw body
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

public class CardStateDto
{
    public Guid Id { get; set; }
    public int Position { get; set; }
    public bool IsFlipped { get; set; }
    public bool IsMatched { get; set; }
    public int? PairId { get; set; }
}
