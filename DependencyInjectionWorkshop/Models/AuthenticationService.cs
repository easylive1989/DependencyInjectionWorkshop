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
        private FailCounter _failCounter;

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _sha256Adapter = new Sha256Adapter();
            _otpService = new OtpService();
            _slackAdapter = new SlackAdapter();
            _failCounter = new FailCounter();
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var isLocked = _failCounter.GetIsLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(accountId);

            var hashedPassword = _sha256Adapter.GetHashedPassword(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                _failCounter.ResetFailCount(accountId);

                return true;
            }
            else
            {
                _failCounter.AddFailCount(accountId);

                LogFailCount(accountId);

                _slackAdapter.Notify(accountId);

                return false;
            }
        }

        private void LogFailCount(string accountId)
        {
            var failedCount = _failCounter.GetFailCount(accountId, new HttpClient() {BaseAddress = new Uri("http://joey.com/")});

            LogMessage($"accountId:{accountId} failed times:{failedCount}");
        }

        private static void LogMessage(string message)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info(message);
        }
    }
}