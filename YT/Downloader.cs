using System;
using System.IO;
using System.Net.Http;

using DiscordBot.Utils;

namespace DiscordBot.YT
{
    public static class Downloader
    {
        /*private static string ContainerToExtension(Container container)
        {
            //Switch on container
            switch (container)
            {
                case Container.Mp4:  return ".mp4";
                case Container.M4A:  return ".m4a";
                case Container.WebM: return ".webm";
                case Container.Tgpp: return ".3gpp";
                case Container.Flv:  return ".flv";
            }

            //Unrecognized container
            return null;
        }

        private static string EncodingToExtension(AudioEncoding encoding)
        {
            //Switch on encoding
            switch (encoding)
            {
                case AudioEncoding.Mp3:    return ".mp3";
                case AudioEncoding.Aac:    return ".aac";
                case AudioEncoding.Vorbis: return ".ogg";
                case AudioEncoding.Opus:   return ".opus";
            }

            //Unrecognized extension
            return null;
        }

        public static byte[] DownloadAudioStream(AudioStreamInfo stream)
        {
            //Catch any errors
            try
            {
                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Send the request
                    var ret = http.GetAsync(stream.Url).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Get the temp path
                    var tempPath = Path.Combine(Path.GetTempPath(), "DiscordBot/");

                    //If on Linux we'll replace temp path with /mnt/ramdisk/DiscordBot
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        tempPath = "/mnt/ramdisk/DiscordBot";
                    }

                    //Make sure the directory exists
                    Directory.CreateDirectory(tempPath);

                    //Generate path for temporary file
                    var path = Path.Combine
                    (
                        tempPath,
                        Guid.NewGuid().ToString() + EncodingToExtension(stream.AudioEncoding)
                    );

                    //Create the temporary file
                    using (TempFile file = new TempFile(path, FileAccess.Write, FileShare.Read))
                    {
                        //Copy data to file
                        ret.Content.CopyToAsync(file.Stream).GetAwaiter().GetResult();

                        //Calculate gain to normalize audio
                        var gain = FFmpeg.CalculateGain(path);

                        //Transcode to uncompressed 16-bit 48khz stream
                        var data = FFmpeg.Transcode(path, gain);

                        //Return transcoded file
                        return data;
                    }
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

            //Return failure
            return null;
        }

        public static AudioStreamInfo GetBestAudioStream(Video video)
        {
            //Catch any errors
            try
            {
                //Prepare the best match
                AudioStreamInfo bestMatch = null;

                //TODO: Use Itags, which moves the definition of "best" further up the callstack

                //Iterate over the streams
                foreach (AudioStreamInfo stream in video.AudioStreamInfos)
                {
                    //Only grab opus-encoded streams
                    if (stream.AudioEncoding == AudioEncoding.Opus)
                    {
                        //Check if the bitrate is closer to 64k than current best match
                        if (Math.Abs(stream.Bitrate - 64000) < Math.Abs((bestMatch?.Bitrate ?? 0) - 64000))
                        {
                            bestMatch = stream;
                        }
                    }     
                }

                //Check if we couldn't find a opus stream
                if (bestMatch == null)
                {
                    //Search again but allow non-opus streams
                    foreach (AudioStreamInfo stream in video.AudioStreamInfos)
                    {
                        //Check if the bitrate is closer to 64k than current best match
                        if (Math.Abs(stream.Bitrate - 64000) < Math.Abs((bestMatch?.Bitrate ?? 0) - 64000))
                        {
                            bestMatch = stream;
                        }
                    }
                }

                //Return the best match
                return bestMatch;
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
        */
    }
}
