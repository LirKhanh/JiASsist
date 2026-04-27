using Microsoft.AspNetCore.SignalR;

public class IssueHub : Hub
{
    public async Task JoinProject(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, projectId);
    }

    public async Task LeaveProject(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, projectId);
    }

    public async Task JoinIssueGroup(string issueId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, issueId);
    }

    public async Task LeaveIssueGroup(string issueId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, issueId);
    }
}