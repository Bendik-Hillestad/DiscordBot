using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DiscordBot.Utils;

namespace DiscordBot.YT
{
    public static class FFmpeg
    {
        public static string CalculateGain(string path, int timeoutMS = 30000)
        {
            //Catch any errors
            try
            {
                //Determine the null path
                var nullPath = (Environment.OSVersion.Platform == PlatformID.Unix) ? "/dev/null" : "NUL";

                //Start ffmpeg
                var ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName               = $"ffmpeg",
                    Arguments              = $"-i \"{path}\" " +
                                             $"-af \"volumedetect\" " +
                                             $"-f null {nullPath}",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                });

                //Capture output asynchronously
                var task = Task.Run(async () =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await ffmpeg.StandardError.BaseStream.CopyToAsync(ms);
                        return Encoding.UTF8.GetString(ms.ToArray());
                    }
                });

                //Wait for exit or timeout
                bool success = ffmpeg.WaitForExit(timeoutMS);

                //Check if it didn't exit in time
                if (!success)
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, "FFmpeg timed out while detecting volume.");

                    //Kill the process
                    ffmpeg.Kill();
                }

                //Grab the output data
                var output = task.GetAwaiter().GetResult();

                //Setup regex to find reported max volume
                var regex = Regex.Escape("max_volume:") + @"\s*(.+?)\s*dB";

                //Check if we got it
                if (Regex.IsMatch(output, regex))
                {
                    //Extract max volume
                    var max = float.Parse(Regex.Match(output, regex).Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);

                    //Just need to negate it to calculate gain
                    return (-max).ToString("0.0", CultureInfo.InvariantCulture);
                }
                else
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, "FFmpeg didn't output max volume.");
                }
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);
            }

            //Return no gain
            return "0.0";
        }

        public static byte[] Transcode(string path, string gain = "0.0", int timeoutMS = 60000)
        {
            //Catch any errors
            try
            {
                //Start ffmpeg
                var ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName               = $"ffmpeg",
                    Arguments              = $"-i \"{path}\" " +
                      (!gain.Equals("0.0") ? $"-af \"volume={gain}dB\" " : "") +
                                             $"-f s16le -ar 48000 -ac 2 pipe:1",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                });

                //Capture output asynchronously
                var task = Task.Run(async () =>
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await ffmpeg.StandardOutput.BaseStream.CopyToAsync(ms);
                        return ms.ToArray();
                    }
                });

                //Wait for exit or timeout
                bool success = ffmpeg.WaitForExit(timeoutMS);

                //Check if it didn't exit in time
                if (!success)
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, "FFmpeg timed out while decoding stream.");

                    //Kill the process
                    ffmpeg.Kill();
                }

                //Return the output data
                return task.GetAwaiter().GetResult();
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);
            }

            //Return failure
            return null;
        }
    }
}
