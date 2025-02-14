using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ValheimExtComponentManager
{
    public static class DownloadUtil
    {
        private const int MaxRetries = 3;
        private const int BufferSize = 81920; // 80 KB

        public static async Task<string> ReadContentAsStringAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        public static async Task DownloadFileAsync(string url, string targetPath)
        {
            int retries = 0;
            while (retries < MaxRetries)
            {
                try
                {
                    var handler = new HttpClientHandler
                    {
                        AllowAutoRedirect = true // Ensure redirects are followed
                    };

                    using (HttpClient client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) })
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        //client.DefaultRequestHeaders.Referrer = new Uri("https://www.dropbox.com/"); // Optional for Dropbox

                        Console.WriteLine($"Downloading from URL: {url}");

                        HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                        response.EnsureSuccessStatusCode();

                        // Check if there was a redirect
                        if (response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.Redirect)
                        {
                            var redirectUrl = response.Headers.Location;
                            Console.WriteLine($"Redirecting to: {redirectUrl}");

                            // Follow the redirect
                            response = await client.GetAsync(redirectUrl);
                            response.EnsureSuccessStatusCode();
                        }

                        long? contentLength = response.Content.Headers.ContentLength;
                        Console.WriteLine($"Content length: {contentLength} bytes");

                        using (Stream contentStream = await response.Content.ReadAsStreamAsync(),
                                      fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, BufferSize, true))
                        {
                            await contentStream.CopyToAsync(fileStream, BufferSize);
                        }

                        Console.WriteLine($"Successfully downloaded file to {targetPath}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    retries++;
                    Console.WriteLine($"Attempt {retries} failed: {ex.Message}");
                    if (retries == MaxRetries)
                    {
                        throw;
                    }
                    await Task.Delay(2000); // Wait for 2 seconds before retrying
                }
            }
        }

        public static async Task DownloadFilesAsync(Dictionary<string, string> urlToTargetPathMap)
        {
            var tasks = new List<Task>();
            using (var semaphore = new SemaphoreSlim(3))
            {
                foreach (var entry in urlToTargetPathMap)
                {
                    await semaphore.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await DownloadFileAsync(entry.Key, entry.Value);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }
        }
    }
}