using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace PGNScraper
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("Please input a Chess.com username and press enter: ");
            var username = Console.ReadLine();
            var initialURL = string.Format("https://api.chess.com/pub/player/{0}/games/archives", username);
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(initialURL);
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = client.GetAsync(initialURL).Result;
            List<string> plycounts = new List<string>();
            var gameCount = 0;
            for (var i = 200; i > 0; i--)
            {
                plycounts.Add(string.Format("{0}{1}", i.ToString(), "..."));
            }
            var stringResponse = await response.Content.ReadAsStringAsync();
            JObject jObj = JObject.Parse(stringResponse);
            if (jObj.ToString().Contains("archives"))
            {
                using (FileStream fs = File.Create(string.Format("{0}{1}", KnownFolders.GetPath(KnownFolder.Downloads), string.Format(@"\{0}.pgn", username))))
                {

                    foreach (var child in jObj["archives"].Children())
                    {
                        var yearMonth = child.ToString().Substring(Math.Max(0, child.ToString().Length - 7));
                        var month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Int32.Parse(yearMonth.Substring(Math.Max(0, yearMonth.Length - 2))));
                        var year = yearMonth.Substring(0, 4);
                        Console.Write(string.Format("\rGathering {0} monthly games: {1}   ", username, string.Format("{0} {1}", year, month)));
                        HttpClient innerClient = new HttpClient();
                        innerClient.BaseAddress = new Uri(child.ToString());
                        innerClient.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                        HttpResponseMessage innerResponse = innerClient.GetAsync(child.ToString()).Result;
                        var innerStringResponse = await innerResponse.Content.ReadAsStringAsync();
                        JObject innerJObj = JObject.Parse(innerStringResponse);
                        foreach (var game in innerJObj["games"].Children())
                        {
                            if (game.ToString().Contains("pgn"))
                            {
                                var pgn = game["pgn"].ToString();
                                foreach (var possiblePly in plycounts)
                                {
                                    if (pgn.Contains(possiblePly))
                                    {
                                        var moveCount = int.Parse(possiblePly.Remove(possiblePly.IndexOf('.')));
                                        var plyCount = moveCount * 2;
                                        pgn = string.Format("[PlyCount \"{0}\"]{1}", plyCount, pgn);
                                        break;
                                    }
                                }
                                Byte[] pgnBytes = new UTF8Encoding(true).GetBytes(pgn);
                                fs.Write(pgnBytes, 0, pgnBytes.Length);
                                gameCount++;
                            }
                        }
                    }
                    Console.WriteLine(string.Format("\nDone! Your PGN has been saved to your Downloads folder. ({0:n0} total games)", gameCount));
                    Console.ReadLine();
                }
            }
            else
            {
                Console.WriteLine(string.Format("No Chess.com users found matching '{0}'", username));
            }
            client.Dispose();
        }
        public static class KnownFolders
        {
            private static string[] _knownFolderGuids = new string[]
            {
                "{374DE290-123F-4565-9164-39C4925E467B}", // Downloads
            };

            public static string GetPath(KnownFolder knownFolder)
            {
                return GetPath(knownFolder, false);
            }

            public static string GetPath(KnownFolder knownFolder, bool defaultUser)
            {
                return GetPath(knownFolder, KnownFolderFlags.DontVerify, defaultUser);
            }

            private static string GetPath(KnownFolder knownFolder, KnownFolderFlags flags,
                bool defaultUser)
            {
                SHGetKnownFolderPath(new Guid(_knownFolderGuids[(int)knownFolder]),
                    (uint)flags, new IntPtr(defaultUser ? -1 : 0), out IntPtr outPath);

                string path = Marshal.PtrToStringUni(outPath);
                Marshal.FreeCoTaskMem(outPath);
                return path;
            }

            [DllImport("Shell32.dll")]
            private static extern int SHGetKnownFolderPath(
                [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken,
                out IntPtr ppszPath);

            [Flags]
            private enum KnownFolderFlags : uint
            {
                SimpleIDList = 0x00000100,
                NotParentRelative = 0x00000200,
                DefaultPath = 0x00000400,
                Init = 0x00000800,
                NoAlias = 0x00001000,
                DontUnexpand = 0x00002000,
                DontVerify = 0x00004000,
                Create = 0x00008000,
                NoAppcontainerRedirection = 0x00010000,
                AliasOnly = 0x80000000
            }
        }

        public enum KnownFolder
        {
            Downloads
        }

    }
}
