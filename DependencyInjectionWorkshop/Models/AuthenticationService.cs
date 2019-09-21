namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly IProfile _profile;
        private readonly IHash _hash;
        private readonly IOtpService _otpService;
        private readonly INotification _notification;
        private readonly IFailedCounter _failedCounter;
        private readonly ILogger _logger;

        public AuthenticationService()
        {
            _profile = new ProfileDao();
            _hash = new Sha256Adapter();
            _otpService = new OtpService();
            _notification = new SlackAdapter();
            _failedCounter = new FailedCounter();
            _logger = new NLogAdapter();
        }

        public AuthenticationService(IFailedCounter failedCounter, ILogger logger, IOtpService otpService,
            IProfile profile, IHash hash, INotification notification)
        {
            _profile = profile;
            _hash = hash;
            _otpService = otpService;
            _notification = notification;
            _failedCounter = failedCounter;
            _logger = logger;
        }

        public bool Verify(string accountId, string password, string otp)
        {
            var isLocked = _failedCounter.GetAccountIsLocked(accountId);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profile.GetPassword(accountId);

            var hashedPassword = _hash.Compute(password);

            var currentOtp = _otpService.GetCurrentOtp(accountId);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                _failedCounter.Reset(accountId);

                return true;
            }
            else
            {
                _failedCounter.Add(accountId);

                LogFailCount(accountId);

                _notification.Send(accountId);

                return false;
            }
        }

        private void LogFailCount(string accountId)
        {
            var failedCount = _failedCounter.GetCount(accountId);

            _logger.Info($"accountId:{accountId} failed times:{failedCount}");
        }
    }
}