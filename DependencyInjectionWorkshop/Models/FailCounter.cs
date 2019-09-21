using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public interface IFailedCounter
    {
        void Reset(string accountId);
        void Add(string accountId);
        int GetCount(string accountId);
        bool GetAccountIsLocked(string accountId);
    }

    public class FailedCounter : IFailedCounter
    {
        public void Reset(string accountId)
        {
            var resetResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                .PostAsJsonAsync("api/failedCounter/Reset", accountId).Result;
            resetResponse.EnsureSuccessStatusCode();
        }

        public void Add(string accountId)
        {
            var addFailedCountResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                    .PostAsJsonAsync("api/failedCounter/Add", accountId).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        public int GetCount(string accountId)
        {
            var failedCountResponse =
                new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                    .PostAsJsonAsync("api/failedCounter/GetFailedCount", accountId).Result;
            failedCountResponse.EnsureSuccessStatusCode();
            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        public bool GetAccountIsLocked(string accountId)
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