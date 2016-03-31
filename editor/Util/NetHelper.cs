using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace StorybrewEditor.Util
{
    public static class NetHelper
    {
        public static string Request(string url, string cachePath, int cacheDuration)
        {
            var fullPath = Path.GetFullPath(cachePath);
            var folder = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            else if (File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath).AddSeconds(cacheDuration) > DateTime.UtcNow)
                return File.ReadAllText(cachePath);

            using (var webClient = new WebClient())
            {
                Debug.Print($"Requesting {url}");
                webClient.Headers.Add("user-agent", Program.Name);
                var response = webClient.DownloadString(url);
                File.WriteAllText(cachePath, response);
                return response;
            }
        }

        public static void Request(string url, string cachePath, int cacheDuration, Action<string, Exception> action)
        {
            try
            {
                var fullPath = Path.GetFullPath(cachePath);
                var folder = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                else if (File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath).AddSeconds(cacheDuration) > DateTime.UtcNow)
                {
                    action(File.ReadAllText(cachePath), null);
                    return;
                }

                using (var webClient = new WebClient())
                {
                    Debug.Print($"Requesting {url}");
                    webClient.Headers.Add("user-agent", Program.Name);
                    webClient.DownloadStringCompleted += (sender, e) =>
                    {
                        if (e.Error == null)
                        {
                            File.WriteAllText(cachePath, e.Result);
                            action(e.Result, null);
                        }
                        else action(null, e.Error);
                    };
                    webClient.DownloadStringAsync(new Uri(url));
                }
            }
            catch (Exception e)
            {
                action(null, e);
            }
        }
    }
}
