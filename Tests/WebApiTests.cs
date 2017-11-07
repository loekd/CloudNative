using System;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using Polly.CircuitBreaker;

namespace Tests
{
    [TestClass]
    public class WebApiTests
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [TestMethod]
        public async Task RetryTransient()
        {
            var policy = Policy
                .Handle<Exception>()
                .RetryAsync(3);

            var response = await policy.ExecuteAsync(async () =>
            {
                var resp = await HttpClient.GetAsync("http://localhost:13000/api/values/GatewayTimeoutServiceUnavailableOk");
                resp.EnsureSuccessStatusCode(); //check and throw
                return resp;
            });

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task RetryTransient_WithSpecificStatusCodes()
        {
            var policy = Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.GatewayTimeout)
                .OrResult(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .RetryAsync(3);

            var response = await policy.ExecuteAsync(async () =>
            {
                var resp = await HttpClient.GetAsync("http://localhost:13000/api/values/GatewayTimeoutServiceUnavailableOk");
                //delegate check to 'HandleResult'
                return resp;
            });

            Assert.IsTrue(response.IsSuccessStatusCode);
        }


        [TestMethod]
        public async Task RetryTransient_WithWaitTimes()
        {
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retryCount => TimeSpan.FromSeconds(retryCount));

            var response = await policy.ExecuteAsync(async () =>
            {
                var resp = await HttpClient.GetAsync("http://localhost:13000/api/values/GatewayTimeoutServiceUnavailableOk");
                resp.EnsureSuccessStatusCode(); //check and throw
                return resp;
            });

            Assert.IsTrue(response.IsSuccessStatusCode);
        }

        [TestMethod]
        public async Task Circuit_Breaker()
        {
            var policy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10));
            

            Exception lastError = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        var resp = await HttpClient.GetAsync(
                            "http://localhost:13000/api/values/GetStatusCode?statusCode=500");
                        resp.EnsureSuccessStatusCode(); //check and throw
                        return resp;
                    });
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            Assert.IsInstanceOfType(lastError, typeof(BrokenCircuitException));
            Assert.IsTrue(policy.CircuitState == CircuitState.Open);
        }

        [TestMethod]
        public async Task Retry_And_Circuit_Breaker()
        {
            int callCount = 0;

            var cbPolicy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(60));

            var retryPolicy = Policy
                .Handle<Exception>()
                .RetryAsync(6);

            var combined = Policy.Wrap(retryPolicy, cbPolicy);

            Exception lastError = null;
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await combined.ExecuteAsync(async () =>
                    {
                        callCount++;
                        var resp = await HttpClient.GetAsync(
                            "http://localhost:13000/api/values/GetStatusCode?statusCode=500");
                        resp.EnsureSuccessStatusCode(); //check and throw
                        return resp;
                    });
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            Assert.IsInstanceOfType(lastError, typeof(BrokenCircuitException));
            Assert.IsTrue(cbPolicy.CircuitState == CircuitState.Open);
            Assert.AreEqual(2, callCount);
        }

        [TestMethod]
        public void Circuit_Breaker_IsolateAndReset()
        {
            var policy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(2, TimeSpan.FromSeconds(10));

            Assert.IsTrue(policy.CircuitState == CircuitState.Closed);
            policy.Isolate();
            Assert.IsTrue(policy.CircuitState == CircuitState.Isolated);
            policy.Reset();
            Assert.IsTrue(policy.CircuitState == CircuitState.Closed);

        }
    }
}
