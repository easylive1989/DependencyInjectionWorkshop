using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class SlackAdapter
    {
        public void Notify(string accountId)
        {
            var slackClient = new SlackClient("my api token");
            var message = $"{accountId} try to login failed";
            slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
        }
    }
}