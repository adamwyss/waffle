using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WaFFL.Evaluation
{

    public interface IHttpClient
    {
        string DownloadString(string uri);
    }

    public class HttpClient : IHttpClient
    {
        private WebClient webClient;

        private int webRequests;

        public HttpClient()
        {
            webClient = new WebClient();
        }
        public string DownloadString(string uri)
        {
            string data = this.webClient.DownloadString(uri);

            Console.WriteLine("Web Requests {0} - {1}", ++this.webRequests, uri);

            return data;
        }
    }

    public class HttpDelay : IHttpClient
    {
        private readonly Random random;
        private IHttpClient innerClient;
        private int min;
        private int max;

        public HttpDelay(IHttpClient innerClient)
            : this(innerClient, 2000, 5000)
        {
        }

        public HttpDelay(IHttpClient innerClient, int min, int max)
        {
            this.random = new Random();
            this.innerClient = innerClient;
            this.min = min;
            this.max = max;
        }
        public string DownloadString(string uri)
        {
            int delay = this.random.Next(this.min, this.max);
            System.Threading.Thread.Sleep(delay);
            return innerClient.DownloadString(uri);
        }
    }

    public class HttpCache : IHttpClient
    {
        private readonly IHttpClient innerClient;
        private readonly string cacheDirectory;

        public HttpCache(IHttpClient innerClient)
            : this(innerClient, "cache")
        {
        }

        public HttpCache(IHttpClient innerClient, string cacheDirectory)
        {
            this.innerClient = innerClient;
            this.cacheDirectory = cacheDirectory;
            if (!Directory.Exists(cacheDirectory))
            {
                Directory.CreateDirectory(cacheDirectory);
            }
        }

        public string DownloadString(string uri)
        {
            string hash = ComputeSha256Hash(uri);
            string filePath = Path.Combine(this.cacheDirectory, $"{hash}.html");

            DateTime lastWrite = File.GetLastWriteTimeUtc(filePath);
            bool oneHourCache = (uri.EndsWith("games.htm") || uri.EndsWith("injuries.htm"));
            bool cacheExpired = (DateTime.UtcNow - lastWrite) > TimeSpan.FromHours(1);
            if (File.Exists(filePath) && (!oneHourCache || (oneHourCache && !cacheExpired)))
            {
                Console.WriteLine("Using Cache - {0}", uri);
                return File.ReadAllText(filePath);
            }
            else if (oneHourCache && cacheExpired)
            {
                Console.WriteLine("Cache Expired - {0}", lastWrite);
            }

            // we are somewhat restricted on our web requests.
            System.Threading.Thread.Sleep(new Random().Next(2000, 5000));
            string data = this.innerClient.DownloadString(uri);

            File.WriteAllText(filePath, data);

            return data;
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();

                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));

                return builder.ToString();
            }
        }
    }
}
