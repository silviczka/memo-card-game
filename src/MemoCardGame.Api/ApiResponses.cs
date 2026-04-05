namespace MemoCardGame.Api;

internal static class ApiResponses
{
    /// <summary>JSON body for HTTP 400 responses: <c>{ "error": "..." }</c>.</summary>
    public static object Error(string message) => new { error = message };
}
