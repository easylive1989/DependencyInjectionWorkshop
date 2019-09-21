namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly IProfile _profile;
        private readonly IHash _hash;
        private readonly IOtpService _otpService;
        private readonly INotification _notification;
        private readonly IFailCounter _failCounter;
        private readonly ILogger _logger;

        public AuthenticationService()
        {
            _profile = new ProfileDao();
            _hash = new Sha256Adapter();
            _otpService = new OtpService();
            _notification = new SlackAdapter();
            _failCounter = new FailCounter();
            _logger = new NLogAdapter();
        }

        public AuthenticationService(IProfile profile, IHash hash, IOtpService otpService, INotification notification, IFailCounter failCounter, ILogger logger)
        {
            _profile = profile;
            _hash = hash;
            _otpService = otpService;
            _notification = notification;
            _failCounter = failCounter;
            _logger = logger;
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var isLocked = _failCounter.GetIsLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profile.GetPassword(accountId);

            var hashedPassword = _hash.Compute(password);

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

                _notification.Send(accountId);

                return false;
            }
        }

        private void LogFailCount(string accountId)
        {
            var failedCount = _failCounter.GetFailCount(accountId);

            _logger.Info($"accountId:{accountId} failed times:{failedCount}");
        }
    }
}