using System.Collections.Generic;
using System.Linq;
using CopilotInteractionApp.Models;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class CopilotLicenseCatalogTests
{
    [Fact]
    public void HasCopilotLicense_NoAssignedPlans_ReturnsFalse()
    {
        var user = new GraphUser { AssignedPlans = null };

        Assert.False(CopilotLicenseCatalog.HasCopilotLicense(user));
    }

    [Fact]
    public void HasCopilotLicense_EmptyAssignedPlans_ReturnsFalse()
    {
        var user = new GraphUser { AssignedPlans = new List<AssignedPlan>() };

        Assert.False(CopilotLicenseCatalog.HasCopilotLicense(user));
    }

    [Fact]
    public void HasCopilotLicense_EnabledCopilotPlan_ReturnsTrue()
    {
        var user = new GraphUser
        {
            AssignedPlans = new List<AssignedPlan>
            {
                new() { ServicePlanId = CopilotLicenseCatalog.GraphGroundedChatServicePlanId, CapabilityStatus = "Enabled" }
            }
        };

        Assert.True(CopilotLicenseCatalog.HasCopilotLicense(user));
    }

    [Fact]
    public void HasCopilotLicense_DisabledCopilotPlan_ReturnsFalse()
    {
        var user = new GraphUser
        {
            AssignedPlans = new List<AssignedPlan>
            {
                new() { ServicePlanId = CopilotLicenseCatalog.GraphGroundedChatServicePlanId, CapabilityStatus = "Disabled" }
            }
        };

        Assert.False(CopilotLicenseCatalog.HasCopilotLicense(user));
    }

    [Fact]
    public void HasCopilotLicense_UnrelatedEnabledPlan_ReturnsFalse()
    {
        var user = new GraphUser
        {
            AssignedPlans = new List<AssignedPlan>
            {
                new() { ServicePlanId = "00000000-0000-0000-0000-000000000000", CapabilityStatus = "Enabled" }
            }
        };

        Assert.False(CopilotLicenseCatalog.HasCopilotLicense(user));
    }

    [Fact]
    public void IsCopilotServicePlan_NullOrWhitespace_ReturnsFalse()
    {
        Assert.False(CopilotLicenseCatalog.IsCopilotServicePlan(null));
        Assert.False(CopilotLicenseCatalog.IsCopilotServicePlan("   "));
    }

    [Fact]
    public void CopilotPlanNames_ReturnsDistinctEnabledCopilotPlanNamesOnly()
    {
        var user = new GraphUser
        {
            AssignedPlans = new List<AssignedPlan>
            {
                new() { ServicePlanId = CopilotLicenseCatalog.GraphGroundedChatServicePlanId, CapabilityStatus = "Enabled" },
                new() { ServicePlanId = CopilotLicenseCatalog.GraphGroundedChatServicePlanId, CapabilityStatus = "Enabled" },
                new() { ServicePlanId = "a62f8878-de10-42f3-b68f-6149a25ceb97", CapabilityStatus = "Disabled" },
                new() { ServicePlanId = "00000000-0000-0000-0000-000000000000", CapabilityStatus = "Enabled" }
            }
        };

        var names = CopilotLicenseCatalog.CopilotPlanNames(user).ToList();

        Assert.Equal(new[] { "M365_COPILOT_BUSINESS_CHAT" }, names);
    }
}
