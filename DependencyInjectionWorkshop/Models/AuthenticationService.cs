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

            var isLockedResponse = httpClient.PostAsync("api/failedCounter/IsLocked", new StringContent(accountId)).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            if (bool.Parse(isLockedResponse.Content.ReadAsStringAsync().Result))
            {
                throw new FailedTooManyTimesException();
            }
            
            string passwordFromDb;
            using (var connection = new SqlConnection("my connection string"))
            {
                passwordFromDb = connection.Query<string>("spGetUserPassword", new {Id = accountId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }

            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(password));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            var hashedPassword = hash.ToString();

            
            var response = httpClient.PostAsync("api/otps", new StringContent(accountId)).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"web api error, accountId:{accountId}");
            }

            var currentOtp = response.Content.ReadAsStringAsync().Result;

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                var resetResponse = httpClient.PostAsync("api/failedCounter/Reset", new StringContent(accountId)).Result;
                resetResponse.EnsureSuccessStatusCode();
                
                return true;
            }
            else
            {
                var addFailedCountResponse = httpClient.PostAsync("api/failedCounter/Add", new StringContent(accountId)).Result;
                addFailedCountResponse.EnsureSuccessStatusCode();
                
                
                var failedCountResponse =
                    httpClient.PostAsync("api/failedCounter/GetFailedCount", new StringContent(accountId)).Result;

                failedCountResponse.EnsureSuccessStatusCode();

                var failedCount = int.Parse(failedCountResponse.Content.ReadAsStringAsync().Result);
                var logger = NLog.LogManager.GetCurrentClassLogger();
                logger.Info($"accountId:{accountId} failed times:{failedCount}");

                var slackClient = new SlackClient("my api token");
                var message = $"{accountId} try to login failed";
                slackClient.PostMessage(response1 => { }, "my channel", message, "my bot name");
                
                return false;
            }
        }
        
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}