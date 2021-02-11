using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CustomerMock.Hubs
{
    public class WebHooksHub : Hub
    {
        public async Task SendMessageAsync(string message)
        {
            await Clients.All.SendAsync("OrderCreated", message);
        }
    }
}
