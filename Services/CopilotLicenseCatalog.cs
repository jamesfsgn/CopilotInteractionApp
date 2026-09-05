using CopilotInteractionApp.Models;

namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// Identifies the Entra service plans that make up a Microsoft 365 Copilot license.
    /// The interaction export API only returns data for users who hold the
    /// "Microsoft Copilot with Graph-grounded chat" (M365_COPILOT_BUSINESS_CHAT) service plan.
    /// </summary>
    public static class CopilotLicenseCatalog
    {
        /// <summary>Service plan ID of "Microsoft Copilot with Graph-grounded chat".</summary>
        public const string GraphGroundedChatServicePlanId = "3f30311c-6b1e-48a4-ab79-725b469da960";

        private static readonly Dictionary<string, string> Plans = new(StringComparer.OrdinalIgnoreCase)
        {
            [GraphGroundedChatServicePlanId] = "M365_COPILOT_BUSINESS_CHAT",
            ["a62f8878-de10-42f3-b68f-6149a25ceb97"] = "M365_COPILOT_APPS",
            ["931e4a88-a67f-48b5-814f-16a5f1e6028d"] = "M365_COPILOT_INTELLIGENT_SEARCH",
            ["b95945de-b3bd-46db-8437-f2beb6ea2347"] = "M365_COPILOT_TEAMS",
            ["0aedf20c-091d-420b-aadf-30c042609612"] = "M365_COPILOT_SHAREPOINT",
            ["89f1c4c8-0878-40f7-804d-869c9128ab5d"] = "M365_COPILOT_CONNECTORS"
        };

        /// <summary>The Graph $select clause required to evaluate <see cref="HasCopilotLicense"/>.</summary>
        public const string RequiredSelect = "assignedPlans";

        public static bool IsCopilotServicePlan(string? servicePlanId) =>
            !string.IsNullOrWhiteSpace(servicePlanId) && Plans.ContainsKey(servicePlanId);

        /// <summary>
        /// True when the user has at least one enabled Microsoft 365 Copilot service plan.
        /// </summary>
        public static bool HasCopilotLicense(GraphUser user)
        {
            var plans = user.AssignedPlans;
            if (plans is null || plans.Count == 0)
            {
                return false;
            }

            return plans.Any(p => p.IsEnabled && IsCopilotServicePlan(p.ServicePlanId));
        }

        /// <summary>
        /// Names the Copilot service plans a user holds, for display in the Issues pane.
        /// </summary>
        public static IEnumerable<string> CopilotPlanNames(GraphUser user) =>
            (user.AssignedPlans ?? new List<AssignedPlan>())
            .Where(p => p.IsEnabled && IsCopilotServicePlan(p.ServicePlanId))
            .Select(p => Plans[p.ServicePlanId!])
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
