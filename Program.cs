using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Runtime.InteropServices;

namespace DiscordSniper
{
    internal class Program
    {

        public struct DiscordInfo
        {
            public string Id { get; set; }
            public string CurrentUsername { get; set; }
            public string CurrentDiscriminator { get; set; }
        }

        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        private const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        static void Main()
        {
            Program.InitializeConsoleColors();
            Console.Title = "[+] DiscordSniper | Created by lucifers wife | ;3#0001";
            string[] checker_tokens = System.IO.File.ReadAllLines("checker_tokens.txt");
            Console.WriteLine($"\u001b[36m[\u001b[37m+\u001b[36m]\u001b[37m {checker_tokens.Length} checker tokens have been loaded.");


            Dictionary<string, string> map = new Dictionary<string, string>();
            foreach (var account in System.IO.File.ReadAllLines("nitro_tokens_and_pwd.txt"))
            {
                string[] slice = account.Split(':');
                map.Add(slice[0], slice[1]);
            }

            Console.WriteLine($"\u001b[36m[\u001b[37m+\u001b[36m]\u001b[37m {map.Count} claimer tokens have been loaded.");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            string[] discordIds = System.IO.File.ReadAllLines("ids.txt");
            Console.WriteLine($"\u001b[36m[\u001b[37m+\u001b[36m]\u001b[37m {discordIds.Length} Discord ID's have been loaded.");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            string[] proxies = null;
            if (System.IO.File.Exists("proxies.txt"))
            {
                proxies = System.IO.File.ReadAllLines("proxies.txt");
                Console.WriteLine($"\u001b[36m[\u001b[37m+\u001b[36m]\u001b[37m {proxies.Length} proxies have been loaded.");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }


            Console.WriteLine($"\n\u001b[31m[\u001b[37m+\u001b[31m]\u001b[37m Waiting for DiscordInfo array...");
            DiscordInfo[] discordInfos = Program.GetDiscordInfos(discordIds, checker_tokens, proxies);

            Console.WriteLine($"\n\u001b[32m[\u001b[37m+\u001b[32m]\u001b[37m Got DiscordInfo array!");
            Thread.Sleep(TimeSpan.FromSeconds(1));

            Console.WriteLine($"\n\u001b[32m[\u001b[37m+\u001b[32m]\u001b[37m Starting now...");
            Thread.Sleep(TimeSpan.FromSeconds(1));


            List<Thread> threads = new List<Thread>();
            foreach (DiscordInfo info in discordInfos)
            {
                Thread thread = new Thread(() =>
                {
                    while (true)
                    {
                        while (!Program.IsDiscordChange(info, checker_tokens, proxies));
                        KeyValuePair<string, string> pair = map.ElementAt(new Random().Next(map.Count - 1));
                        if (Program.AttemptReserve(info, pair))
                        {
                            Console.WriteLine($"\n\u001b[32m[\u001b[37m+\u001b[32m]\u001b[37m claimed the Discord username: {info.CurrentUsername}#{info.CurrentDiscriminator}!");
                            System.IO.File.WriteAllText($"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}.txt", $"Username Claimed: {info.CurrentUsername}#{info.CurrentDiscriminator}\nToken: {pair.Key}\nUnixTime: {DateTimeOffset.Now.ToUnixTimeMilliseconds()}");
                            break;
                        }
                        else
                        {
                            Console.WriteLine($"\n\u001b[31m[\u001b[37m+\u001b[31m]\u001b[37m missed the Discord username: {info.CurrentUsername}#{info.CurrentDiscriminator}...");
                        }
                    }
                });
                thread.Start();
                threads.Add(thread);
            }
            foreach (Thread _t in threads)
            {
                _t.Join();
            }
            Thread.Sleep(TimeSpan.FromSeconds(10));

        }
        
        public static void InitializeConsoleColors()
        {
            var iStdOut = GetStdHandle(STD_OUTPUT_HANDLE);
            if (!GetConsoleMode(iStdOut, out uint outConsoleMode))
            {
                Console.WriteLine("failed to get output console mode");
                Console.ReadKey();
                return;
            }

            outConsoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
            if (!SetConsoleMode(iStdOut, outConsoleMode))
            {
                Console.WriteLine($"failed to set output console mode, error code: {GetLastError()}");
                Console.ReadKey();
                return;
            }
        }

        public static DiscordInfo[] GetDiscordInfos(string[] ids, string[] tokens, string[] proxies = null)
        {
            DiscordInfo[] discordInfos = new DiscordInfo[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                using (HttpClientHandler httpClientHandler = new HttpClientHandler() { UseCookies = false })
                {
                    if (proxies != null)
                    {
                        httpClientHandler.Proxy = new WebProxy($"http://{proxies[new Random().Next(proxies.Length - 1)]}");
                    }
                    using (HttpClient httpClient = new HttpClient(handler: httpClientHandler))
                    {
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokens[new Random().Next(tokens.Length)]);
                        using (HttpResponseMessage httpResponseMessage = httpClient.GetAsync($"https://discord.com/api/v9/users/{ids[i]}/profile?with_mutual_guilds=false").Result)
                        {
                            string response = httpResponseMessage.Content.ReadAsStringAsync().Result;
                            JObject jobject = JsonConvert.DeserializeObject<JObject>(response);

                            discordInfos[i] = new DiscordInfo
                            {
                                Id = ids[i],
                                CurrentUsername = jobject.SelectToken("user.username").ToObject<String>(),
                                CurrentDiscriminator = jobject.SelectToken("user.discriminator").ToObject<String>()
                            };

                        }
                    }
                }
            }
            return discordInfos;
        }

        public static bool IsDiscordChange(DiscordInfo discordInfo, string[] tokens, string[] proxies = null)
        {
            using (HttpClientHandler httpClientHandler = new HttpClientHandler() { UseCookies = false })
            {
                if (proxies != null)
                {
                    httpClientHandler.Proxy = new WebProxy($"http://{proxies[new Random().Next(proxies.Length - 1)]}");
                }
                using (HttpClient httpClient = new HttpClient(handler: httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(tokens[new Random().Next(tokens.Length)]);
                    using (HttpResponseMessage httpResponseMessage = httpClient.GetAsync($"https://discord.com/api/v9/users/{discordInfo.Id}/profile?with_mutual_guilds=false").Result)
                    {
                        try
                        {
                            string response = httpResponseMessage.Content.ReadAsStringAsync().Result;
                            JObject jobject = JsonConvert.DeserializeObject<JObject>(response);

                            string username = jobject.SelectToken("user.username").ToObject<String>();
                            string discriminator = jobject.SelectToken("user.discriminator").ToObject<String>();
                            return discordInfo.CurrentUsername != username || discordInfo.CurrentDiscriminator != discriminator;

                        }
                        catch
                        {
                            return false;
                        }
                        
                    }
                }
            }
        }

        public static bool AttemptReserve(DiscordInfo discordInfo, KeyValuePair<string, string> pair)
        {
            using (HttpClientHandler httpClientHandler = new HttpClientHandler() { UseCookies = false })
            {
                using (HttpClient httpClient = new HttpClient(handler: httpClientHandler))
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(pair.Key);
                    using (HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("PATCH"), "https://discord.com/api/v9/users/@me"))
                    {
                        using (StringContent stringContent = new StringContent("{\"username\":\"" + discordInfo.CurrentUsername + "\",\"password\":\"" + pair.Value + "\",\"discriminator\":\"" + discordInfo.CurrentDiscriminator + "\"}"))
                        {
                            stringContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                            message.Content = stringContent;

                            using (HttpResponseMessage httpResponseMessage = httpClient.SendAsync(message).Result)
                            {
                                return httpResponseMessage.IsSuccessStatusCode;
                            }
                        } 
                    }
                }
            }
        }
    }
}
