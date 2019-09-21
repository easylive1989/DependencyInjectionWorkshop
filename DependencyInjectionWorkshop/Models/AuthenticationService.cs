using System;
using System.Net.Http;
using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao;
        private readonly Sha256Adapter _sha256Adapter;

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _sha256Adapter = new Sha256Adapter();
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var httpClient = new HttpClient() {BaseAddress = new Uri("http://joey.com/")};

            var isLocked = GetIsLocked(accountId, httpClient);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(accountId);

            var hashedPassword = _sha256Adapter.GetHashedPassword(password);

            var currentOtp = GetCurrentOtp(accountId, httpClient);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                ResetFailCount(accountId, httpClient);

                return true;
            }
            else
            {
                AddFailCount(accountId, httpClient);

                LogFailCount(accountId, httpClient);

                Notify(accountId);

                return false;
            }
        }

        private static bool GetIsLocked(string accountId, HttpClient httpClient)
        {
            var isLockedResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/IsLocked", new StringContent(accountId)).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLocked;
        }

        private static void Notify(string accountId)
        {
            var slackClient = new SlackClient("my api token");
            var message = $"{accountId} try to login failed";
            slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
        }

        private static void LogFailCount(string accountId, HttpClient httpClient)
        {
            var failedCount = GetFailCount(accountId, httpClient);

            LogMessage($"accountId:{accountId} failed times:{failedCount}");
        }

        private static void LogMessage(string message)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }

        private static int GetFailCount(string accountId, HttpClient httpClient)
        {
            var failedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        private static void AddFailCount(string accountId, HttpClient httpClient)
        {
            var addFailedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        private static void ResetFailCount(string accountId, HttpClient httpClient)
        {
            var resetResponse = httpClient.PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetResponse.EnsureSuccessStatusCode();
        }

        private static string GetCurrentOtp(string accountId, HttpClient httpClient)
        {
            var response = httpClient.PostAsJsonAsync("api/otps", accountId).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }

            var currentOtp = response.Content.ReadAsAsync<string>().Result;
            return currentOtp;
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}