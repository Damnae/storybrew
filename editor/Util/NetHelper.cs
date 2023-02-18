using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace StorybrewEditor.Util
{
    public static class NetHelper
    {
        public static void Request(string url, string cachePath, int cacheDuration, Action<string, Exception> action)
        {
            try
            {
                var fullPath = Path.GetFullPath(cachePath);
                var folder = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                else if (File.Exists(cachePath) && File.GetLastWriteTimeUtc(cachePath).AddSeconds(cacheDuration) > DateTime.UtcNow)
                {
                    Program.Schedule(() => action(File.ReadAllText(cachePath), null));
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
                            var result = e.Result;
                            Program.Schedule(() =>
                            {
                                File.WriteAllText(cachePath, result);
                                action(result, null);
                            });
                        }
                        else Program.Schedule(() => action(null, e.Error));
                    };
                    webClient.DownloadStringAsync(new Uri(url));
                }
            }
            catch (Exception e)
            {
                Program.Schedule(() => action(null, e));
            }
        }
        public static void Post(string url, NameValueCollection data, Action<string, Exception> action)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    Debug.Print($"Post {url}");
                    webClient.Headers.Add("user-agent", Program.Name);
                    webClient.UploadValuesCompleted += (sender, e) =>
                    {
                        if (e.Error == null)
                        {
                            var response = Encoding.UTF8.GetString(e.Result);
                            Program.Schedule(() => action(response, null));
                        }
                        else Program.Schedule(() => action(null, e.Error));
                    };
                    webClient.UploadValuesAsync(new Uri(url), "POST", data);
                }
            }
            catch (Exception e)
            {
                Program.Schedule(() => action(null, e));
            }
        }
        public static void BlockingPost(string url, NameValueCollection data, Action<string, Exception> action)
        {
            try
            {
                using (var webClient = new WebClient())
                {
                    Debug.Print($"Post {url}");
                    webClient.Headers.Add("user-agent", Program.Name);
                    var result = webClient.UploadValues(new Uri(url), "POST", data);
                    var response = Encoding.UTF8.GetString(result);
                    action(response, null);
                }
            }
            catch (Exception e)
            {
                action(null, e);
            }
        }
        public static void Download(string url, string filename, Func<float, bool> progressFunc, Action<Exception> completedAction)
        {
            try
            {
                var fullPath = Path.GetFullPath(filename);
                var folder = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                else if (File.Exists(filename)) File.Delete(filename);

                using (var webClient = new WebClient())
                {
                    Debug.Print($"Downloading {url}");
                    webClient.Headers.Add("user-agent", Program.Name);
                    webClient.DownloadProgressChanged += (sender, e) => Program.Schedule(() =>
                    {
                        if (!progressFunc((float)e.BytesReceived / e.TotalBytesToReceive)) webClient.CancelAsync();
                    });
                    webClient.DownloadFileCompleted += (sender, e) => Program.Schedule(() =>
                    {
                        if (e.Cancelled)
                        {
                            Debug.Print($"Download cancelled {url}");
                            return;
                        }
                        if (e.Error == null) completedAction(null);
                        else completedAction(e.Error);
                    });
                    webClient.DownloadFileAsync(new Uri(url), filename);
                }
            }
            catch (Exception e)
            {
                Program.Schedule(() => completedAction(e));
            }
        }
    }
}