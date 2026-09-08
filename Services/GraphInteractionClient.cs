using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using CopilotInteractionApp.Models;

namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// Options used to call the Copilot interaction export API.
    /// </summary>
    public sealed class InteractionQueryOptions
    {
        /// <summary>Directory (tenant) ID or domain name, e.g. contoso.onmicrosoft.com.</summary>
        public string TenantId { get; set; } = string.Empty;

        /// <summary>Application (client) ID of the Entra app registration.</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>Client secret value. Held in memory only; never persisted.</summary>
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>
        /// User object ID or UPN. When empty, every user in scope is queried.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// When <see cref="UserId"/> is empty, restricts the pull to users who hold a
        /// Microsoft 365 Copilot service plan instead of every enabled user in the tenant.
        /// </summary>
        public bool CopilotLicensedOnly { get; set; } = true;

        /// <summary>Targets /beta when true, /v1.0 otherwise.</summary>
        public bool UseBeta { get; set; }

        /// <summary>Graph page size ($top). 100 is the value Microsoft recommends for this API.</summary>
        public int Top { get; set; } = 100;

        /// <summary>
        /// Lower bound for the createdDateTime filter. The API requires both boundaries, so this is
        /// only applied together with <see cref="ToDateUtc"/>. The comparison is exclusive (gt).
        /// </summary>
        public DateTimeOffset? FromDateUtc { get; set; }

        /// <summary>Upper bound for the createdDateTime filter. Exclusive (lt).</summary>
        public DateTimeOffset? ToDateUtc { get; set; }

        /// <summary>Raw Graph appClass to filter on, or null for all apps.</summary>
        public string? AppClass { get; set; }

        /// <summary>Overall cap on the number of interactions returned across all users.</summary>
        public int MaxItems { get; set; } = 1000;
    }

    public sealed class InteractionFetchResult
    {
        /// <summary>Interactions collected, each tagged with the user they belong to.</summary>
        public List<UserInteraction> Items { get; } = new();

        /// <summary>Per-user failures that were skipped so the run could continue.</summary>
        public List<string> Errors { get; } = new();

        /// <summary>Users the interaction API was successfully called for.</summary>
        public int UsersQueried { get; set; }

        /// <summary>True when collection stopped early because the MaxItems cap was reached.</summary>
        public bool LimitReached { get; set; }

        /// <summary>Total enabled users seen while enumerating the tenant.</summary>
        public int UsersScanned { get; set; }

        /// <summary>Users skipped because they do not hold a Microsoft 365 Copilot service plan.</summary>
        public int UsersWithoutCopilotLicense { get; set; }

        /// <summary>True when Graph rejected the server-side $filter and it was applied locally instead.</summary>
        public bool UsedClientSideFilterFallback { get; set; }
    }

    /// <summary>
    /// A non-success response from Microsoft Graph, carrying the status code so callers can react to it.
    /// </summary>
    public sealed class GraphRequestException : Exception
    {
        public GraphRequestException(HttpStatusCode statusCode, string message) : base(message) =>
            StatusCode = statusCode;

        public HttpStatusCode StatusCode { get; }
    }

    /// <summary>
    /// An interaction paired with the user it was retrieved for. The API response does not
    /// identify the user, so the association has to be carried alongside it.
    /// </summary>
    public sealed class UserInteraction
    {
        public required GraphUser User { get; init; }
        public required AiInteraction Interaction { get; init; }
    }

    /// <summary>
    /// Calls the Microsoft Graph aiInteractionHistory: getAllEnterpriseInteractions API.
    /// The API only supports application (app-only) permission AiEnterpriseInteraction.Read.All.
    /// </summary>
    public sealed class GraphInteractionClient : IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;
        private readonly Func<CancellationToken, Task<string>>? _tokenProvider;
        private ClientSecretCredential? _credential;
        private string? _credentialKey;

        public GraphInteractionClient() : this(new HttpClientHandler(), tokenProvider: null)
        {
        }

        /// <summary>
        /// Test-only seam: substitutes a fake HTTP handler and, optionally, a fake token
        /// provider so tests never make a real network call or need real Entra credentials.
        /// </summary>
        internal GraphInteractionClient(HttpMessageHandler handler, Func<CancellationToken, Task<string>>? tokenProvider)
        {
            _http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
            _tokenProvider = tokenProvider;
        }

        /// <summary>
        /// Runs a full pull: acquires a token, resolves the user scope (a single user, or every
        /// enabled tenant user optionally narrowed to Copilot license holders), then retrieves and
        /// pages each user's interactions. Per-user failures are collected rather than thrown.
        /// </summary>
        /// <param name="progress">Receives human-readable status updates for the status bar.</param>
        /// <exception cref="ArgumentException">A required credential is missing.</exception>
        /// <exception cref="OperationCanceledException">The caller cancelled the run.</exception>
        public async Task<InteractionFetchResult> GetInteractionsAsync(
            InteractionQueryOptions options,
            IProgress<string>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (string.IsNullOrWhiteSpace(options.TenantId)) throw new ArgumentException("Tenant ID is required.");
            if (string.IsNullOrWhiteSpace(options.ClientId)) throw new ArgumentException("Client ID is required.");
            if (string.IsNullOrWhiteSpace(options.ClientSecret)) throw new ArgumentException("Client secret is required.");

            progress?.Report("Acquiring token...");
            await GetTokenAsync(options, cancellationToken).ConfigureAwait(false);

            var result = new InteractionFetchResult();
            IReadOnlyList<GraphUser> users;

            if (!string.IsNullOrWhiteSpace(options.UserId))
            {
                users = new[] { new GraphUser { Id = options.UserId.Trim(), UserPrincipalName = options.UserId.Trim() } };
            }
            else
            {
                progress?.Report("Enumerating tenant users...");
                var allUsers = await GetUsersAsync(options, progress, cancellationToken).ConfigureAwait(false);
                result.UsersScanned = allUsers.Count;

                if (options.CopilotLicensedOnly)
                {
                    var licensed = allUsers.Where(CopilotLicenseCatalog.HasCopilotLicense).ToList();
                    result.UsersWithoutCopilotLicense = allUsers.Count - licensed.Count;
                    users = licensed;
                    progress?.Report(
                        $"{licensed.Count} of {allUsers.Count} user(s) have a Copilot license. Retrieving interactions...");
                }
                else
                {
                    users = allUsers;
                    progress?.Report($"Found {allUsers.Count} user(s). Retrieving interactions...");
                }
            }

            var userIndex = 0;
            foreach (var user in users)
            {
                cancellationToken.ThrowIfCancellationRequested();
                userIndex++;

                if (result.Items.Count >= options.MaxItems)
                {
                    result.LimitReached = true;
                    break;
                }

                var label = user.DisplayLabel;
                progress?.Report($"[{userIndex}/{users.Count}] {label} - {result.Items.Count} interaction(s) so far...");

                try
                {
                    var remaining = options.MaxItems - result.Items.Count;
                    var (interactions, usedFallback) = await GetInteractionsForUserAsync(options, user, remaining, cancellationToken)
                        .ConfigureAwait(false);

                    result.UsedClientSideFilterFallback |= usedFallback;
                    result.UsersQueried++;
                    foreach (var interaction in interactions)
                    {
                        result.Items.Add(new UserInteraction { User = user, Interaction = interaction });
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"{label}: {ex.Message}");
                }
            }

            if (result.Items.Count >= options.MaxItems)
            {
                result.LimitReached = true;
            }

            return result;
        }

        /// <summary>
        /// Retrieves one user's interactions. Some tenants reject the server-side $filter on this
        /// function; when that happens the request is retried unfiltered and the filter is applied locally
        /// so a date or app selection never fails the whole run.
        /// </summary>
        private async Task<(List<AiInteraction> Items, bool UsedFallback)> GetInteractionsForUserAsync(
            InteractionQueryOptions options,
            GraphUser user,
            int maxItems,
            CancellationToken cancellationToken)
        {
            var userId = user.Id ?? user.UserPrincipalName ?? string.Empty;
            var hasFilter = HasServerFilter(options);

            try
            {
                var items = await FetchInteractionPagesAsync(
                    BuildInitialUrl(options, userId, applyFilter: true),
                    options, maxItems, predicate: null, maxPages: int.MaxValue, cancellationToken)
                    .ConfigureAwait(false);

                return (items, false);
            }
            catch (GraphRequestException ex) when (hasFilter && ex.StatusCode == HttpStatusCode.BadRequest)
            {
                var items = await FetchInteractionPagesAsync(
                    BuildInitialUrl(options, userId, applyFilter: false),
                    options, maxItems, BuildClientSideFilter(options), UnfilteredFallbackPageLimit, cancellationToken)
                    .ConfigureAwait(false);

                return (items, true);
            }
        }

        /// <summary>Page limit applied when scanning unfiltered results during the $filter fallback.</summary>
        private const int UnfilteredFallbackPageLimit = 50;

        private async Task<List<AiInteraction>> FetchInteractionPagesAsync(
            string url,
            InteractionQueryOptions options,
            int maxItems,
            Func<AiInteraction, bool>? predicate,
            int maxPages,
            CancellationToken cancellationToken)
        {
            var results = new List<AiInteraction>();
            var pages = 0;

            while (!string.IsNullOrEmpty(url) && results.Count < maxItems && pages < maxPages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pages++;

                var content = await SendAsync(url, options, cancellationToken).ConfigureAwait(false);
                var payload = JsonSerializer.Deserialize<GraphCollectionResponse<AiInteraction>>(content, JsonOptions);

                if (payload?.Value is { Count: > 0 })
                {
                    results.AddRange(predicate is null ? payload.Value : payload.Value.Where(predicate));
                }

                url = payload?.NextLink;
            }

            if (results.Count > maxItems)
            {
                results.RemoveRange(maxItems, results.Count - maxItems);
            }

            return results;
        }

        private static bool HasServerFilter(InteractionQueryOptions options) =>
            !string.IsNullOrWhiteSpace(options.AppClass) ||
            (options.FromDateUtc.HasValue && options.ToDateUtc.HasValue);

        private static Func<AiInteraction, bool> BuildClientSideFilter(InteractionQueryOptions options) => interaction =>
        {
            if (!string.IsNullOrWhiteSpace(options.AppClass) &&
                !string.Equals(interaction.AppClass, options.AppClass.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (options.FromDateUtc.HasValue && options.ToDateUtc.HasValue)
            {
                var created = interaction.CreatedDateTime;
                if (created is null || created <= options.FromDateUtc || created >= options.ToDateUtc)
                {
                    return false;
                }
            }

            return true;
        };

        private async Task<IReadOnlyList<GraphUser>> GetUsersAsync(
            InteractionQueryOptions options,
            IProgress<string>? progress,
            CancellationToken cancellationToken)
        {
            var select = "id,displayName,userPrincipalName,accountEnabled";
            if (options.CopilotLicensedOnly)
            {
                select += "," + CopilotLicenseCatalog.RequiredSelect;
            }

            var url = $"https://graph.microsoft.com/v1.0/users?$select={select}&$top=999";

            return await GetUserPagesAsync(url, options, progress, "Enumerating tenant users", cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IReadOnlyList<GraphUser>> GetUserPagesAsync(
            string url,
            InteractionQueryOptions options,
            IProgress<string>? progress,
            string progressLabel,
            CancellationToken cancellationToken)
        {
            var users = new List<GraphUser>();
            var nextUrl = url;

            while (!string.IsNullOrEmpty(nextUrl))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = await SendAsync(nextUrl, options, cancellationToken).ConfigureAwait(false);
                var payload = JsonSerializer.Deserialize<GraphCollectionResponse<GraphUser>>(content, JsonOptions);

                if (payload?.Value is { Count: > 0 })
                {
                    users.AddRange(payload.Value.Where(u => u.AccountEnabled != false && !string.IsNullOrEmpty(u.Id)));
                }

                nextUrl = payload?.NextLink;
                progress?.Report($"{progressLabel}... {users.Count} found.");
            }

            return users;
        }

        /// <summary>
        /// Issues an authenticated GET and retries on throttling (429) and transient 5xx responses.
        /// </summary>
        private async Task<string> SendAsync(string url, InteractionQueryOptions options, CancellationToken cancellationToken)
        {
            const int maxAttempts = 4;

            for (var attempt = 1; ; attempt++)
            {
                var token = await GetTokenAsync(options, cancellationToken).ConfigureAwait(false);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                using var response = await _http
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);

                var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return content;
                }

                var isTransient = response.StatusCode == HttpStatusCode.TooManyRequests ||
                                  (int)response.StatusCode >= 500;

                if (isTransient && attempt < maxAttempts)
                {
                    var delay = response.Headers.RetryAfter?.Delta
                                ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw new GraphRequestException(
                    response.StatusCode,
                    $"Graph request failed ({(int)response.StatusCode} {response.ReasonPhrase}). {Truncate(ExtractErrorMessage(content), 500)}");
            }
        }

        private async Task<string> GetTokenAsync(InteractionQueryOptions options, CancellationToken cancellationToken)
        {
            if (_tokenProvider is not null)
            {
                return await _tokenProvider(cancellationToken).ConfigureAwait(false);
            }

            var key = $"{options.TenantId}|{options.ClientId}|{options.ClientSecret.GetHashCode()}";
            if (_credential is null || _credentialKey != key)
            {
                _credential = new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
                _credentialKey = key;
            }

            var context = new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" });
            var accessToken = await _credential.GetTokenAsync(context, cancellationToken).ConfigureAwait(false);
            return accessToken.Token;
        }

        private static string BuildInitialUrl(InteractionQueryOptions options, string userId, bool applyFilter)
        {
            var version = options.UseBeta ? "beta" : "v1.0";
            var baseUrl =
                $"https://graph.microsoft.com/{version}/copilot/users/{Uri.EscapeDataString(userId)}" +
                "/interactionHistory/getAllEnterpriseInteractions";

            var query = new List<string>
            {
                $"$top={Math.Clamp(options.Top, 1, 1000)}"
            };

            var filters = new List<string>();
            if (applyFilter && !string.IsNullOrWhiteSpace(options.AppClass))
            {
                filters.Add($"appClass eq '{options.AppClass.Trim().Replace("'", "''")}'");
            }

            // The API requires both boundaries when filtering on createdDateTime.
            if (applyFilter && options.FromDateUtc.HasValue && options.ToDateUtc.HasValue)
            {
                filters.Add($"createdDateTime gt {Format(options.FromDateUtc.Value)}");
                filters.Add($"createdDateTime lt {Format(options.ToDateUtc.Value)}");
            }

            if (filters.Count > 0)
            {
                query.Add("$filter=" + Uri.EscapeDataString(string.Join(" and ", filters)));
            }

            return baseUrl + "?" + string.Join("&", query);
        }

        private static string ExtractErrorMessage(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            try
            {
                using var document = JsonDocument.Parse(content);
                if (document.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? content;
                }
            }
            catch (JsonException)
            {
                // Fall through and return the raw payload.
            }

            return content;
        }

        private static string Format(DateTimeOffset value) =>
            value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

        private static string Truncate(string value, int max) =>
            string.IsNullOrEmpty(value) || value.Length <= max ? value : value[..max] + "...";

        public void Dispose() => _http.Dispose();
    }
}
