using System.Text.Json.Serialization;

namespace CopilotInteractionApp.Models
{
    /// <summary>
    /// Subset of the Microsoft Graph aiInteraction resource returned by
    /// GET /copilot/users/{id}/interactionHistory/getAllEnterpriseInteractions
    /// </summary>
    public sealed class AiInteraction
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("sessionId")]
        public string? SessionId { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("appClass")]
        public string? AppClass { get; set; }

        [JsonPropertyName("interactionType")]
        public string? InteractionType { get; set; }

        [JsonPropertyName("conversationType")]
        public string? ConversationType { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("createdDateTime")]
        public DateTimeOffset? CreatedDateTime { get; set; }

        [JsonPropertyName("locale")]
        public string? Locale { get; set; }

        [JsonPropertyName("body")]
        public ItemBody? Body { get; set; }

        [JsonPropertyName("from")]
        public AiInteractionFrom? From { get; set; }

        [JsonPropertyName("contexts")]
        public List<AiInteractionContext>? Contexts { get; set; }

        [JsonPropertyName("attachments")]
        public List<AiInteractionAttachment>? Attachments { get; set; }

        [JsonPropertyName("links")]
        public List<AiInteractionLink>? Links { get; set; }

        [JsonPropertyName("mentions")]
        public List<AiInteractionMention>? Mentions { get; set; }
    }

    public sealed class ItemBody
    {
        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    /// <summary>
    /// Sender of an interaction. Exactly one of these is populated: prompts come from a user,
    /// responses from the Copilot bot application.
    /// </summary>
    public sealed class AiInteractionFrom
    {
        [JsonPropertyName("user")]
        public IdentityInfo? User { get; set; }

        [JsonPropertyName("application")]
        public IdentityInfo? Application { get; set; }

        [JsonPropertyName("device")]
        public IdentityInfo? Device { get; set; }
    }

    public sealed class IdentityInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("tenantId")]
        public string? TenantId { get; set; }
    }

    /// <summary>Where the interaction took place, e.g. a Teams meeting or a document.</summary>
    public sealed class AiInteractionContext
    {
        [JsonPropertyName("contextReference")]
        public string? ContextReference { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("contextType")]
        public string? ContextType { get; set; }
    }

    public sealed class AiInteractionAttachment
    {
        [JsonPropertyName("attachmentId")]
        public string? AttachmentId { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("contentUrl")]
        public string? ContentUrl { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>A resource Copilot grounded its response on, such as a file, meeting or message.</summary>
    public sealed class AiInteractionLink
    {
        [JsonPropertyName("linkUrl")]
        public string? LinkUrl { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("linkType")]
        public string? LinkType { get; set; }
    }

    public sealed class AiInteractionMention
    {
        [JsonPropertyName("mentionId")]
        public int? MentionId { get; set; }

        [JsonPropertyName("mentionText")]
        public string? MentionText { get; set; }
    }

    /// <summary>
    /// Standard Microsoft Graph collection envelope, including the paging link.
    /// </summary>
    public sealed class GraphCollectionResponse<T>
    {
        [JsonPropertyName("value")]
        public List<T>? Value { get; set; }

        /// <summary>URL of the next page, or null when the collection is exhausted.</summary>
        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }
}
