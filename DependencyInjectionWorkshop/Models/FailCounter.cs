using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public class FailCounter
    {
        public void ResetFailCount(string accountId)
        {
            var resetResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                .PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetResponse.EnsureSuccessStatusCode();
        }

        public void AddFailCount(string accountId)
        {
            var addFailedCountResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                    .PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        public int GetFailCount(string accountId, HttpClient httpClient)
        {
            var failedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        public bool GetIsLocked(string accountId)
        {
            var isLockedResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                    .PostAsJsonAsync("api/failedCounter/IsLocked", new StringContent(accountId)).Result;
            isLockedResponse.EnsureSuccessStatusCode();
            var isLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isLocked;
        }
    }
}