using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BookMart.Services
{
    public class WebsitePinger : BackgroundService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _url = "https://bookmart-c2m2.onrender.com/"; // 🔁 Replace with your deployed site URL
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(30); // ⏱️ every 30 seconds

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = await _httpClient.GetAsync(_url);
                    Console.WriteLine($"[{DateTime.Now}] Pinged: {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ping error: {ex.Message}");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
