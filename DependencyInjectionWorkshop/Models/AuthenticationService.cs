using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao;
        private readonly Sha256Adapter _sha256Adapter;
        private readonly OtpService _otpService;
        private readonly SlackAdapter _slackAdapter;

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _sha256Adapter = new Sha256Adapter();
            _otpService = new OtpService();
            _slackAdapter = new SlackAdapter();
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var isLocked = GetIsLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(accountId);

            var hashedPassword = _sha256Adapter.GetHashedPassword(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                ResetFailCount(accountId);

                return true;
            }
            else
            {
                AddFailCount(accountId);

                LogFailCount(accountId);

                _slackAdapter.Notify(accountId);

                return false;
            }
        }

        private static bool GetIsLocked(string accountId)
        {
            var isLockedResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}.PostAsJsonAsync("api/failedCounter/IsLocked", new StringContent(accountId)).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLocked;
        }

        private static void LogFailCount(string accountId)
        {
            var failedCount = GetFailCount(accountId, new HttpClient() {BaseAddress = new Uri("http://joey.com/")});

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

        private static void AddFailCount(string accountId)
        {
            var addFailedCountResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}.PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        private static void ResetFailCount(string accountId)
        {
            var resetResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}.PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetResponse.EnsureSuccessStatusCode();
        }
    }
}