using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Execute;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Text;
namespace Download
{
    public class Chunked
    {
        private static void WriteProgress(long begin, long end)
        {
            completed = completed + (end - begin);
            Double p = Math.Round((((Double)completed / length) * 100), 2);
            if (p < 100)
            {
                string percent = String.Format("{0:00.00}", p);
                Double completedMB = Math.Round((Double)(completed / 1048576), 2);
                DateTime now = DateTime.Now;
                Double ela = (now - start).TotalSeconds;
                Double rem = (ela * ((Double)length / completed)) - ela;
                string rate = (Math.Round(((completedMB * 8) / ela), 2)).ToString();
                TimeSpan diff = (now.AddSeconds(rem) - now);
                string da = diff.Days.ToString("D2");
                string ho = diff.Hours.ToString("D2");
                string mi = diff.Minutes.ToString("D2");
                string se = diff.Seconds.ToString("D2");
                string mis = diff.Milliseconds.ToString("D3");
                ts = percent + "% :: " + String.Format("{0:00000.00}", completedMB.ToString()) + " MB of " + String.Format("{0:00000.00}", lengthMB.ToString()) + " MB :: " + da + " Days :: " + ho + " Hours :: " + mi + " Minutes :: " + se + " Seconds ::" + mis + " remaining :: " + rate + " Mbps";
                Console.SetCursorPosition(left, top);
                Console.Write("\r{0}   ", ts);
            }
            else
            {
                Double completedMB = Math.Round((Double)(completed / 1048576), 2);
                DateTime now = DateTime.Now;
                Double ela = (now - start).TotalSeconds;
                string rate = (Math.Round(((completedMB * 8) / ela), 2)).ToString();
                ts = "100% :: " + String.Format("{0:00000.00}", lengthMB.ToString()) + " MB of " + String.Format("{0:00000.00}", lengthMB.ToString()) + " MB :: 00 Days :: 00 Hours :: 00 Minutes :: 00 Seconds ::000 remaining :: " + rate + " Mbps";
                Console.SetCursorPosition(left, top);
                Console.Write("\r{0}   ", ts);
            }
        }
        private static long completed = 0;
        private static Int32 top = Console.CursorTop;
        private static Int32 left = Console.CursorLeft;
        private static Double lengthMB = 0;
        private static long length = 0;
        private static DateTime start = DateTime.Now;
        private static string ts = String.Empty;
        private static Int32 chunk = 262144;
        private static Int32 numberOfChunks = 0;
        private static async Task<string> StartDownload(string Url, string filePath, bool silent = false)
        {
            ServicePointManager.DefaultConnectionLimit = 10;
            RetObject ret = Execute.HttpRequest.Send(
                Url,
                HttpMethod.Head
            );
            length = long.Parse(ret.HttpResponseMessage.Content.Headers.ContentLength.ToString());
            lengthMB = Math.Round(((Double)length / 1048576), 2);
            numberOfChunks = Convert.ToInt32(Math.Floor(Convert.ToDecimal((length / chunk)))) + 1;
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            start = DateTime.Now;
            FileStream fs = File.Open(filePath, FileMode.OpenOrCreate);
            for (Int32 i = 0; i < numberOfChunks; i++)
            {
                List<Int32> be = new List<Int32>();
                be.Add((i * chunk));
                be.Add((i * chunk + chunk - 1));
                await Task.Factory.StartNew((b) =>
                {
                    long begin = ((List<Int32>)b)[0];
                    long end = ((List<Int32>)b)[1];
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(Url);
                    req.ServicePoint.ConnectionLimit = 10;
                    req.AllowReadStreamBuffering = false;
                    req.AllowWriteStreamBuffering = false;
                    req.AddRange(begin, end);
                    using (HttpWebResponse res = (HttpWebResponse)req.GetResponse() as HttpWebResponse)
                    {
                        using (Stream stream = res.GetResponseStream())
                        {
                            stream.CopyTo(fs);
                            req.Abort();
                            res.Close();
                            res.Dispose();
                        }
                    }
                }, be, TaskCreationOptions.LongRunning);
                if (!silent)
                {
                    WriteProgress(be[0], be[1]);
                }
            }
            fs.Close();
            fs.Dispose();
            String[] sp = new string[ts.Length];
            String[] bs = new string[ts.Length];
            return "done";
        }
        public static void Start(string Url, string filePath, bool silent = false)
        {
            Task task = StartDownload(Url, filePath, silent);
            task.Wait();
        }
    }
}
