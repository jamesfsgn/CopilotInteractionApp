namespace CopilotInteractionApp.Services
{
    /// <summary>
    /// A selectable Copilot app filter. <see cref="Value"/> is the raw Graph appClass
    /// and <see cref="Name"/> is what the user sees.
    /// </summary>
    public sealed record AppClassOption(string Value, string Name)
    {
        public override string ToString() => Name;
    }

    /// <summary>
    /// Maps Graph <c>appClass</c> values such as IPM.SkypeTeams.Message.Copilot.BizChat
    /// to friendly product names.
    /// </summary>
    public static class AppClassCatalog
    {
        private const string Prefix = "IPM.SkypeTeams.Message.Copilot.";

        public static readonly AppClassOption All = new(string.Empty, "All apps");

        private static readonly AppClassOption[] Known =
        {
            new(Prefix + "BizChat", "Microsoft 365 Copilot Chat"),
            new(Prefix + "Teams", "Copilot in Teams"),
            new(Prefix + "Word", "Copilot in Word"),
            new(Prefix + "Excel", "Copilot in Excel"),
            new(Prefix + "PowerPoint", "Copilot in PowerPoint"),
            new(Prefix + "Outlook", "Copilot in Outlook"),
            new(Prefix + "OneNote", "Copilot in OneNote"),
            new(Prefix + "Loop", "Copilot in Loop"),
            new(Prefix + "Whiteboard", "Copilot in Whiteboard"),
            new(Prefix + "Stream", "Copilot in Stream"),
            new(Prefix + "Designer", "Copilot in Designer"),
            new(Prefix + "SharePoint", "Copilot in SharePoint"),
            new(Prefix + "Planner", "Copilot in Planner"),
            new(Prefix + "OneDrive", "Copilot in OneDrive")
        };

        public static IReadOnlyList<AppClassOption> Options { get; } =
            new[] { All }.Concat(Known).ToList();

        /// <summary>
        /// Returns a friendly product name for an appClass value, falling back to a readable
        /// form of the raw value when the app is not in the catalog.
        /// </summary>
        public static string FriendlyName(string? appClass)
        {
            if (string.IsNullOrWhiteSpace(appClass))
            {
                return string.Empty;
            }

            var known = Known.FirstOrDefault(o =>
                string.Equals(o.Value, appClass, StringComparison.OrdinalIgnoreCase));

            if (known is not null)
            {
                return known.Name;
            }

            var trimmed = appClass.Trim();
            if (trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                var suffix = trimmed[Prefix.Length..];
                return string.IsNullOrEmpty(suffix) ? trimmed : $"Copilot in {suffix}";
            }

            return trimmed;
        }

        /// <summary>
        /// Resolves a persisted appClass back to its combo box option, falling back to
        /// <see cref="All"/> when the value is empty or no longer in the catalog.
        /// </summary>
        public static AppClassOption Find(string? appClass) =>
            Options.FirstOrDefault(o => string.Equals(o.Value, appClass, StringComparison.OrdinalIgnoreCase)) ?? All;
    }
}
