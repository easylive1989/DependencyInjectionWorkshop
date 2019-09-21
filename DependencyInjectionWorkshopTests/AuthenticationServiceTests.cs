using DependencyInjectionWorkshop.Models;
using NSubstitute;
using NUnit.Framework;

namespace DependencyInjectionWorkshopTests
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private const string DefaultAccountId = "joey";
        private const string DefaultHashedPassword = "my hashed password";
        private const string DefaultInputPassword = "abc";
        private const string DefaultOtp = "123456";
        private AuthenticationService _authenticationService;
        private IFailedCounter _failedCounter;
        private IHash _hash;
        private ILogger _logger;
        private INotification _notification;
        private IOtpService _otpService;
        private IProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _notification = Substitute.For<INotification>();
            _logger = Substitute.For<ILogger>();
            _failedCounter = Substitute.For<IFailedCounter>();
            _otpService = Substitute.For<IOtpService>();
            _hash = Substitute.For<IHash>();
            _profile = Substitute.For<IProfile>();
            _authenticationService =
                new AuthenticationService(_failedCounter, _logger, _otpService, _profile, _hash, _notification);
        }

        [Test]
        public void is_valid()
        {
            GivenPassword(DefaultAccountId, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccountId, DefaultOtp);

            var isValid1 = WhenVerify(DefaultAccountId, DefaultInputPassword, DefaultOtp);
            var isValid = isValid1;

            ShouldBeValid(isValid);
        }

        private bool WhenValid()
        {
            GivenPassword(DefaultAccountId, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccountId, DefaultOtp);

            var isValid = WhenVerify(DefaultAccountId, DefaultInputPassword, DefaultOtp);
            return isValid;
        }

        [Test]
        public void is_invalid()
        {
            GivenPassword(DefaultAccountId, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccountId, "wrong otp");

            var isValid = WhenVerify(DefaultAccountId, DefaultInputPassword, DefaultOtp);

            ShouldBeInvalid(isValid);
        }

        [Test]
        public void reset_failed_count_when_valid()
        {
            WhenValid();
            
            _failedCounter.Received(1).Reset(DefaultAccountId);
        }

        private static void ShouldBeInvalid(bool isValid)
        {
            Assert.IsFalse(isValid);
        }

        private static void ShouldBeValid(bool isValid)
        {
            Assert.IsTrue(isValid);
        }

        private bool WhenVerify(string accountId, string password, string otp)
        {
            return _authenticationService.Verify(accountId, password, otp);
        }

        private void GivenOtp(string accountId, string otp)
        {
            _otpService.GetCurrentOtp(accountId).Returns(otp);
        }

        private void GivenHash(string inputPassword, string hashedPassword)
        {
            _hash.Compute(inputPassword).Returns(hashedPassword);
        }

        private void GivenPassword(string accountId, string password)
        {
            _profile.GetPassword(accountId).Returns(password);
        }
    }
}