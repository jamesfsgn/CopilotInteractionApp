using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CopilotInteractionApp.Models;
using CopilotInteractionApp.Services;
using Xunit;

namespace CopilotInteractionApp.Tests.Services;

public class GraphInteractionClientTests
{
    private static InteractionQueryOptions BaseOptions(string? userId = "user-1") => new()
    {
        TenantId = "tenant",
        ClientId = "client",
        ClientSecret = "secret",
        UserId = userId ?? string.Empty
    };

    [Fact]
    public async Task GetInteractionsAsync_FollowsNextLink_AcrossMultiplePages()
    {
        var page1 = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction> { new() { Id = "1", InteractionType = "userPrompt" } },
            NextLink = "https://graph.microsoft.com/v1.0/copilot/users/user-1/interactionHistory/getAllEnterpriseInteractions?%24skiptoken=abc"
        });

        var page2 = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction> { new() { Id = "2", InteractionType = "aiResponse" } },
            NextLink = null
        });

        var responses = new Queue<string>(new[] { page1, page2 });
        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responses.Dequeue())
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));

        var result = await client.GetInteractionsAsync(BaseOptions());

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("1", result.Items[0].Interaction.Id);
        Assert.Equal("2", result.Items[1].Interaction.Id);
        Assert.Equal(2, handler.RequestedUrls.Count);
    }

    [Fact]
    public async Task GetInteractionsAsync_TrimsResultsToMaxItems_AndReportsLimitReached()
    {
        var page = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction>
            {
                new() { Id = "1", InteractionType = "userPrompt" },
                new() { Id = "2", InteractionType = "userPrompt" },
                new() { Id = "3", InteractionType = "userPrompt" },
                new() { Id = "4", InteractionType = "userPrompt" },
                new() { Id = "5", InteractionType = "userPrompt" }
            },
            NextLink = null
        });

        var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(page)
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));
        var options = BaseOptions();
        options.MaxItems = 2;

        var result = await client.GetInteractionsAsync(options);

        Assert.Equal(2, result.Items.Count);
        Assert.True(result.LimitReached);
    }

    [Fact]
    public async Task GetInteractionsAsync_RetriesOn429_ThenSucceeds()
    {
        var success = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction> { new() { Id = "1", InteractionType = "userPrompt" } },
            NextLink = null
        });

        var attempt = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            attempt++;
            if (attempt == 1)
            {
                var throttled = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                throttled.Headers.Add("Retry-After", "0");
                return throttled;
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(success) };
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));

        var result = await client.GetInteractionsAsync(BaseOptions());

        Assert.Single(result.Items);
        Assert.Equal(2, handler.RequestedUrls.Count);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GetInteractionsAsync_ExhaustsRetriesOn5xx_AndRecordsPerUserError()
    {
        var handler = new FakeHttpMessageHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("{\"error\":{\"message\":\"boom\"}}")
            };
            response.Headers.Add("Retry-After", "0");
            return response;
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));

        var result = await client.GetInteractionsAsync(BaseOptions());

        Assert.Empty(result.Items);
        Assert.Single(result.Errors);
        Assert.Contains("500", result.Errors[0]);
        Assert.Equal(4, handler.RequestedUrls.Count);
    }

    [Fact]
    public async Task GetInteractionsAsync_FallsBackToClientSideFilter_WhenServerRejectsFilterWith400()
    {
        var matching = new AiInteraction { Id = "match", AppClass = "IPM.SkypeTeams.Message.Copilot.Word", InteractionType = "userPrompt" };
        var nonMatching = new AiInteraction { Id = "no-match", AppClass = "IPM.SkypeTeams.Message.Copilot.Excel", InteractionType = "userPrompt" };

        var unfiltered = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction> { matching, nonMatching },
            NextLink = null
        });

        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            if (callCount == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":{\"message\":\"$filter is not supported\"}}")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(unfiltered) };
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));
        var options = BaseOptions();
        options.AppClass = "IPM.SkypeTeams.Message.Copilot.Word";

        var result = await client.GetInteractionsAsync(options);

        Assert.True(result.UsedClientSideFilterFallback);
        Assert.Single(result.Items);
        Assert.Equal("match", result.Items[0].Interaction.Id);
    }

    [Fact]
    public async Task GetInteractionsAsync_TenantWide_SkipsDisabledAndMissingIdUsers()
    {
        var usersJson = JsonSerializer.Serialize(new GraphCollectionResponse<GraphUser>
        {
            Value = new List<GraphUser>
            {
                new() { Id = "u1", UserPrincipalName = "ada@contoso.com", DisplayName = "Ada", AccountEnabled = true },
                new() { Id = "u2", UserPrincipalName = "disabled@contoso.com", DisplayName = "Disabled", AccountEnabled = false },
                new() { Id = null, UserPrincipalName = "noId@contoso.com", DisplayName = "No Id", AccountEnabled = true }
            },
            NextLink = null
        });

        var interactionsJson = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction> { new() { Id = "1", InteractionType = "userPrompt" } },
            NextLink = null
        });

        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            var body = callCount == 1 ? usersJson : interactionsJson;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));
        var options = BaseOptions(userId: null);
        options.CopilotLicensedOnly = false;

        var result = await client.GetInteractionsAsync(options);

        Assert.Equal(1, result.UsersScanned);
        Assert.Equal(1, result.UsersQueried);
        Assert.Single(result.Items);
        Assert.Equal(2, handler.RequestedUrls.Count);
    }

    [Fact]
    public async Task GetInteractionsAsync_TenantWide_CopilotLicensedOnly_SkipsUnlicensedUsers()
    {
        var usersJson = JsonSerializer.Serialize(new GraphCollectionResponse<GraphUser>
        {
            Value = new List<GraphUser>
            {
                new()
                {
                    Id = "u1",
                    UserPrincipalName = "licensed@contoso.com",
                    AccountEnabled = true,
                    AssignedPlans = new List<AssignedPlan>
                    {
                        new() { ServicePlanId = CopilotLicenseCatalog.GraphGroundedChatServicePlanId, CapabilityStatus = "Enabled" }
                    }
                },
                new()
                {
                    Id = "u2",
                    UserPrincipalName = "unlicensed@contoso.com",
                    AccountEnabled = true,
                    AssignedPlans = new List<AssignedPlan>()
                }
            },
            NextLink = null
        });

        var interactionsJson = JsonSerializer.Serialize(new GraphCollectionResponse<AiInteraction>
        {
            Value = new List<AiInteraction> { new() { Id = "1", InteractionType = "userPrompt" } },
            NextLink = null
        });

        var callCount = 0;
        var handler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            var body = callCount == 1 ? usersJson : interactionsJson;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        });

        using var client = new GraphInteractionClient(handler, _ => Task.FromResult("fake-token"));
        var options = BaseOptions(userId: null);
        options.CopilotLicensedOnly = true;

        var result = await client.GetInteractionsAsync(options);

        Assert.Equal(2, result.UsersScanned);
        Assert.Equal(1, result.UsersWithoutCopilotLicense);
        Assert.Equal(1, result.UsersQueried);
        Assert.Single(result.Items);
    }
}
