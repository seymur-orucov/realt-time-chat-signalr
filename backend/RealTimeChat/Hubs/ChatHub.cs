using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using RealTimeChat.Models;

namespace RealTimeChat.Hubs;

public interface IChatClient
{
    public Task ReceiveMessage(string userName, string message);
}

public class ChatHub : Hub<IChatClient>
{
    // Dictionary to store active users
    public static readonly Dictionary<string, UserConnection> ActiveUsers = new Dictionary<string, UserConnection>();

    public async Task JoinChat(UserConnection connection)
    {
        // Add the connection to the group
        await Groups.AddToGroupAsync(Context.ConnectionId, connection.ChatRoom);

        // Store connection data in the ActiveUsers dictionary
        ActiveUsers[Context.ConnectionId] = connection;

        await Clients
            .Group(connection.ChatRoom)
            .ReceiveMessage("Admin", $"{connection.UserName} присоединился к чату");
    }

    public async Task SendMessage(string message)
    {
        // Retrieve connection information from ActiveUsers dictionary
        if (ActiveUsers.TryGetValue(Context.ConnectionId, out var connection))
        {
            await Clients
                .Group(connection.ChatRoom)
                .ReceiveMessage(connection.UserName, message);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Retrieve and remove the user connection from ActiveUsers dictionary
        if (ActiveUsers.TryGetValue(Context.ConnectionId, out var connection))
        {
            ActiveUsers.Remove(Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, connection.ChatRoom);

            await Clients
                .Group(connection.ChatRoom)
                .ReceiveMessage("Admin", $"{connection.UserName} покинул чат");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
