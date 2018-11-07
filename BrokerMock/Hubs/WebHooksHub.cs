using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace BrokerMock.Hubs
{
    public class WebHooksHub : Hub
    {
        public async Task SendMessageAsync(string message)
        {
            await Clients.All.SendAsync("RequestCreated", message);
        }
    }
}
