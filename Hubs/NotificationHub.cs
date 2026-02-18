using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Saffrat.Hubs
{
    [Authorize(Roles = "admin,staff,waiter,deliveryman")]
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            var userRole = Context.User.FindFirstValue(ClaimTypes.Role);
            Groups.AddToGroupAsync(Context.ConnectionId, userRole);
            return base.OnConnectedAsync();
        }
    }
}
