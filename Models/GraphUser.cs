using System.Text.Json.Serialization;

namespace CopilotInteractionApp.Models
{
    /// <summary>
    /// Minimal projection of a Microsoft Graph user.
    /// </summary>
    public sealed class GraphUser
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }

        [JsonPropertyName("accountEnabled")]
        public bool? AccountEnabled { get; set; }

        [JsonPropertyName("assignedPlans")]
        public List<AssignedPlan>? AssignedPlans { get; set; }

        public string DisplayLabel =>
            DisplayName ?? UserPrincipalName ?? Id ?? "(unknown user)";
    }

    /// <summary>
    /// A single service plan assigned to a user (from the user's assignedPlans collection).
    /// </summary>
    public sealed class AssignedPlan
    {
        [JsonPropertyName("servicePlanId")]
        public string? ServicePlanId { get; set; }

        [JsonPropertyName("service")]
        public string? Service { get; set; }

        [JsonPropertyName("capabilityStatus")]
        public string? CapabilityStatus { get; set; }

        public bool IsEnabled =>
            string.Equals(CapabilityStatus, "Enabled", StringComparison.OrdinalIgnoreCase);
    }
}
