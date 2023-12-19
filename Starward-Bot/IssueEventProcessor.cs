using Microsoft.Extensions.Caching.Memory;
using Octokit;
using Octokit.Webhooks;
using Octokit.Webhooks.Events;
using Octokit.Webhooks.Events.IssueComment;
using Octokit.Webhooks.Events.Issues;
using Octokit.Webhooks.Models;
using System.Text.RegularExpressions;

namespace Starward_Bot;

internal class IssueEventProcessor : WebhookEventProcessor
{


    private readonly IMemoryCache _memory;

    private GitHubClient _client;


#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    public IssueEventProcessor(IMemoryCache memory)
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
    {
        _memory = memory;
    }



    private async Task EnsureAppClient()
    {
        if (_memory.TryGetValue("GithubClient", out GitHubClient? client))
        {
            _client = client!;
            return;
        }
        client = await GithubUtil.CreateGithubClient();
        _memory.Set("GithubClient", client, DateTimeOffset.Now + TimeSpan.FromSeconds(10));
        _client = client;
    }




    protected override async Task ProcessIssuesWebhookAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        await EnsureAppClient();
        if (action == IssuesAction.Opened || action == IssuesAction.Reopened)
        {
            await OnIssueOpenedAsync(headers, issuesEvent, action);
        }

        if (action == IssuesAction.Labeled)
        {
            await OnIssueLabeledAsync(headers, issuesEvent, action);
        }
    }




    private async Task OnIssueOpenedAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        if ((action == IssuesAction.Opened || action == IssuesAction.Reopened) && issuesEvent.Repository is { FullName: "Scighost/Starward" })
        {
            bool close = false;
            string title = issuesEvent.Issue.Title;
            string? body = issuesEvent.Issue.Body;

            if (!close && string.IsNullOrWhiteSpace(title))
            {
                close = true;
            }
            if (!close && string.IsNullOrWhiteSpace(body))
            {
                close = true;
            }
            if (!close && string.IsNullOrWhiteSpace(Regex.Match(title, @"(\[.+\])?(.+)").Groups[2].Value))
            {
                close = true;
            }

            if (close)
            {
                var issueUpdate = new IssueUpdate { Title = title };
                issueUpdate.AddLabel("invalid");
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _client.Issue.Comment.Create(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, "This issue would be closed for no title or content.");
                await _client.Issue.Update(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, issueUpdate);
            }
        }
    }




    private async Task OnIssueLabeledAsync(WebhookHeaders headers, IssuesEvent issuesEvent, IssuesAction action)
    {
        if (action == IssuesAction.Labeled && issuesEvent.Repository is { FullName: "Scighost/Starward" })
        {
            if (issuesEvent.Issue.State?.Value is IssueState.Closed)
            {
                return;
            }
            if (issuesEvent.Issue.Labels.Any(x => x.Name is "invalid"))
            {
                var issue = await _client.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _client.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "This issue would be closed for something invalid.");
                await _client.Issue.Update(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, issueUpdate);
            }
            if (issuesEvent.Issue.Labels.Any(x => x.Name is "duplicate"))
            {
                var issue = await _client.Issue.Get("Scighost", "Starward", (int)issuesEvent.Issue.Number);
                var issueUpdate = issue.ToUpdate();
                issueUpdate.State = ItemState.Closed;
                issueUpdate.StateReason = ItemStateReason.NotPlanned;
                await _client.Issue.Comment.Create("Scighost", "Starward", (int)issuesEvent.Issue.Number, "This issue would be closed for duplicate.");
                await _client.Issue.Update(issuesEvent.Repository.Id, (int)issuesEvent.Issue.Number, issueUpdate);
            }
        }
    }




    protected override async Task ProcessIssueCommentWebhookAsync(WebhookHeaders headers, IssueCommentEvent issueCommentEvent, IssueCommentAction action)
    {
        await EnsureAppClient();
        if (action == IssueCommentAction.Deleted && issueCommentEvent.Repository is { FullName: "Scighost/Starward" })
        {
            if (issueCommentEvent.Sender?.Login is "Scighost")
            {
                return;
            }
            string body = $"""
                > @{issueCommentEvent.Comment.User.Login} deleted the following comment
                > published at {issueCommentEvent.Comment.CreatedAt:yyyy-MM-dd HH:mm:ss zzz}
                > updated at {issueCommentEvent.Comment.UpdatedAt:yyyy-MM-dd HH:mm:ss zzz}
                
                {issueCommentEvent.Comment.Body}
                """;
            await _client.Issue.Comment.Create("Scighost", "Starward", (int)issueCommentEvent.Issue.Number, body);
        }
    }




}
