using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dapper;
using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        public bool Verify(string accountId, string password, string otp)
        {
            var httpClient = new HttpClient() {BaseAddress = new Uri("http://joey.com/")};

            var isLocked = GetIsLocked(accountId, httpClient);
            if (isLocked)
            {
                throw new FailedTooManyTimesException();
            }
            
            var passwordFromDb = GetPasswordFromDb(accountId);

            var hashedPassword = GetHashedPassword(password);

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
            var isLockedResponse = httpClient.PostAsync("api/failedCounter/IsLocked", new StringContent(accountId)).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = bool.Parse(isLockedResponse.Content.ReadAsStringAsync().Result);
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
                httpClient.PostAsync("api/failedCounter/GetFailedCount", new StringContent(accountId)).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = int.Parse(failedCountResponse.Content.ReadAsStringAsync().Result);
            return failedCount;
        }
        
        private static void AddFailCount(string accountId, HttpClient httpClient)
        {
            var addFailedCountResponse = httpClient.PostAsync("api/failedCounter/Add", new StringContent(accountId)).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        private static void ResetFailCount(string accountId, HttpClient httpClient)
        {
            var resetResponse = httpClient.PostAsync("api/failedCounter/Reset", new StringContent(accountId)).Result;
            resetResponse.EnsureSuccessStatusCode();
        }

        private static string GetCurrentOtp(string accountId, HttpClient httpClient)
        {
            var response = httpClient.PostAsync("api/otps", new StringContent(accountId)).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }

            var currentOtp = response.Content.ReadAsStringAsync().Result;
            return currentOtp;
        }

        private static string GetHashedPassword(string password)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            var hashedPassword = hash.ToString();
            return hashedPassword;
        }

        private static string GetPasswordFromDb(string accountId)
        {
            string passwordFromDb;
            using (var connection = new SqlConnection("my connection string"))
            {
                passwordFromDb = connection.Query<string>("spGetUserPassword", new {Id = accountId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            return passwordFromDb;
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}