using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public interface INotification
    {
        void Send(string accountId);
    }

    public class SlackAdapter : INotification
    {
        public void Send(string accountId)
        {
            var slackClient = new SlackClient("my api token");
            var message = $"{accountId} try to login failed";
            slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
        }
    }
}