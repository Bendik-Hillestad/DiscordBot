using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using DiscordBot.Core;
using DiscordBot.Music;
using DiscordBot.Utils;
using DiscordBot.YT;

namespace DiscordBot.Modules.Music
{
    public partial class MusicModule : CommandModule<MusicModule>
    {
        public void music_join_impl(Context ctx, IVoiceChannel channel)
        {
            //Catch any errors
            bool e = Debug.Try(() =>
            {
                //TODO: Rewrite this ugly mess
                //Check if we're already connected to a channel
                if ((this.audioClient?.ConnectionState ?? ConnectionState.Disconnected) == ConnectionState.Connected)
                {
                    //Disconnect first
                    this.audioOutStream.Flush();
                    this.audioOutStream.Dispose();
                    this.audioClient.StopAsync().GetAwaiter().GetResult();
                    this.audioOutStream = null;
                    this.audioClient = null;
                    this.audioChannel = null;

                    //Connect to the new channel
                    this.audioChannel = channel;
                    channel.ConnectAsync((client) =>
                    {
                        //Save client
                        this.audioClient = client;

                        //Catch connected event
                        client.Connected += () =>
                        {
                            //Create output stream
                            this.audioOutStream = client.CreatePCMStream(AudioApplication.Music);
                            return Task.CompletedTask;
                        };
                    });
                }
                else
                {
                    //Connect to the new channel
                    this.audioChannel = channel;
                    channel.ConnectAsync((client) =>
                    {
                        //Save client
                        this.audioClient = client;

                        //Catch connected event
                        client.Connected += () =>
                        {
                            //Create output stream
                            this.audioOutStream = client.CreatePCMStream(AudioApplication.Music);
                            return Task.CompletedTask;
                        };
                    });
                }

                //Spawn the downloader if it's not started yet (or has crashed)
                if (this.audioDownloader == null)
                {
                    this.SpawnAudioStreamDownloader();
                }

                //Spawn the streamer if it's not started yet (or has crashed)
                if (this.audioStreamer == null)
                {
                    this.SpawnAudioStreamer();
                }

                //Return success
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Success",
                    $"Joined {channel.Name}."
                );
            });

            //Check if there was an error
            if (!e)
            {
                //Return generic error
                Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel,
                    "Error",
                    "Couldn't join channel."
                );
            }
        }

        public void music_leave_impl(Context ctx)
        {
            //TODO: Fix this ugly mess at some point
            //Disconnect
            this.audioOutStream?.Flush    ();
            this.audioOutStream?.Dispose  ();
            this.audioClient?.   StopAsync();
            this.audioOutStream = null;
            this.audioClient    = null;
            this.audioChannel   = null;

            //Return success
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Success",
                "Channel left."
            );
        }

        public void music_play_impl(Context ctx, string song)
        {
            //Try to get the id if there is one
            var id = GetYouTubeVideoID(song);

            //Check if it's null
            if (id == null)
            {
                //Search for it instead
                var response = YouTube.SearchVideo(song, this.youtubeAPIKey);

                //Check if it was successful
                if (response != null && (response.items?.Length ?? 0) > 0)
                {
                    //Get the first item
                    id = response.items[0].id.videoId;
                }
                else
                {
                    //Return video not found error
                    Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel,
                        "Error",
                        "Couldn't find video."
                    ); return;
                }
            }

            //Get video info
            var videoInfo = YouTube.GetVideoInfo(id);

            //Check that it's not null
            Debug.Assert(videoInfo != null, "Video info is null.");

            //Calculate duration
            int dur = Regex.Match(videoInfo.Value.length, @"^(\d+)(?:\:(\d+))?(?:\:(\d+))?$").Groups
                           .Select(g => g?.Value).Where(s => !string.IsNullOrWhiteSpace(s))
                           .Skip  (1).Reverse()
                           .Select((s, i) => int.Parse(s) * (int)(Math.Pow(60, i) + 0.5))
                           .Sum   ();

            //Check that it's not too long
            if (dur < 600)
            {
                //Queue for download
                this.QueueDownload(videoInfo.Value);

                //Find the extension
                var regex = Regex.Match(videoInfo.Value.name, @"(?:\.([^.]+?)$)");

                //Extract the title
                var title = videoInfo.Value.name.Substring(0, regex.Index);

                //Return success
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Success",
                    $"{title} [{Utility.PadNum(dur / 60)}:{Utility.PadNum(dur % 60)}] was added to the queue."
                ); return;
            }
            else
            {
                //Find the extension
                var regex = Regex.Match(videoInfo.Value.name, @"(?:\.([^.]+?)$)");

                //Extract the title
                var title = videoInfo.Value.name.Substring(0, regex.Index);

                //Return video too long error
                Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel,
                    "Error",
                    $"{title} [{videoInfo.Value.length}] could not be added.\nIt's too long."
                ); return;
            }
        }

        private static string GetYouTubeVideoID(string str)
        {
            //Check if it's just the id
            if (Regex.IsMatch(str, @"^[a-zA-Z0-9_\-]{11}$"))
            {
                //Return the string
                return str;
            }

            //Check if it even contains an id
            if (Regex.IsMatch(str, @"[a-zA-Z0-9_\-]{11}"))
            {
                //Look for watch?v={id} or youtu.be/{id}
                var regex = @"(?:" + 
                                Regex.Escape(@"watch?v=")  + "|" +
                                Regex.Escape(@"youtu.be/") +
                            @")([a-zA-Z0-9_\-]{11})";
                if (Regex.IsMatch(str, regex))
                {
                    //Extract the ID and return it
                    return Regex.Match(str, regex).Groups[1].Value;
                }
            }

            //Return null
            return null;
        }

        private static string[] GetSpotifyPlaylistID(string str)
        {
            //Look for /{user}/playlist/{id}
            var match = Regex.Match(str, @"/user/(.+?)/playlist/([a-zA-Z0-9_\-]{22})");
            if (match.Success)
            {
                //Extract user and playlist ID
                return new string[] { match.Groups[1].Value, match.Groups[2].Value };
            }

            //Return null
            return null;
        }

        private static string GetYouTubePlaylistID(string str)
        {
            //Check if it's just the id
            if (Regex.IsMatch(str, @"^PL([a-zA-Z0-9_\-]{32}|[a-zA-Z0-9_\-]{16})$"))
            {
                //Return the string
                return str;
            }

            //Check if it even contains an id
            if (Regex.IsMatch(str, @"PL([a-zA-Z0-9_\-]{32}|[a-zA-Z0-9_\-]{16})"))
            {
                //Look for ?list={id} or &list={id}
                var match = Regex.Match(str, @"[\?|&]list=PL([a-zA-Z0-9_\-]{32}|[a-zA-Z0-9_\-]{16})");
                if (match.Success)
                {
                    //Extract the ID and return it
                    return "PL" + match.Groups[1].Value;
                }
            }

            //Return null
            return null;
        }

        private void SpawnAudioStreamDownloader()
        {
            //Launch a new thread that will download audio streams
            this.audioDownloader = Task.Factory.StartNew(() =>
            {
                //Loop forever
                while (true)
                {
                    //Wait for signal
                    this.downloadSemaphore.WaitOne();

                    //Get next item to download
                    var next = this.GetNextDownload();

                    //Skip if it's null
                    if (!next.HasValue) continue;

                    //Try to execute
                    string path = null;
                    var song = Debug.Try<Song?>(() =>
                    {
                        //Prepare the Song structure
                        Song s = new Song { id = next.Value.id };

                        //Find the extension
                        var regex = Regex.Match(next.Value.name, @"(?:\.([^.]+?)$)");

                        //Extract the title
                        s.name = next.Value.name.Substring(0, regex.Index);

                        //Download the audio
                        path = YouTube.DownloadAudio(next.Value);

                        //Calculate gain to normalize audio
                        var gain = FFmpeg.CalculateGain(path);

                        //Transcode to uncompressed 16-bit 48khz stream
                        s.data = FFmpeg.Transcode(path, gain);

                        //Return the song
                        return s;
                    }, null);

                    //Delete the temporary file
                    if (path != null) Debug.Try(() => System.IO.File.Delete(path));

                    //Check if we managed to download
                    if (song.HasValue)
                    {
                        //Add to the queue
                        this.QueueSong(song.Value);

                        //Go back to start
                        continue;
                    }
                    else
                    {
                        //Log error
                        Logger.Log(LOG_LEVEL.ERROR, $"Couldn't find stream for {next.Value.name}.");

                        //Trigger another download if not already triggered
                        try { this.downloadSemaphore.Release(); } catch (Exception) { }
                    }
                }
            }, TaskCreationOptions.LongRunning)
            //Capture exit
            .ContinueWith((t) =>
            {
                //Get errors
                t.Exception?.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });

                //Log exit
                Logger.Log(LOG_LEVEL.ERROR, "Audio downloader exited!");

                //Set pointer to null
                this.audioDownloader = null;
            });
        }

        private void SpawnAudioStreamer()
        {
            //Spawn the streaming thread
            this.audioStreamer = Task.Factory.StartNew(() =>
            {
                //Setup block size
                const int blockSize = 3840;

                //Loop forever
                while (true)
                {
                    //Check that the audio client is connected and we have a source
                    if
                    (
                        (this.audioClient?.ConnectionState ?? ConnectionState.Disconnected) == ConnectionState.Connected &&
                         this.audioOutStream != null && this.source != null
                    )
                    {
                        //Capture errors so we just skip to next file when there's an issue
                        try
                        {
                            //Grab the data
                            var data   = this.source.Value.data;
                            var offset = 0;

                            //Prepare a buffer
                            var buffer = new Samples { raw = new byte[blockSize] };

                            //Loop until skip is requested (or stream is over)
                            while (!this.skip)
                            {
                                //Calculate number of bytes in the next chunk
                                var byteCount = Math.Min(data.Length - offset, blockSize);

                                //Check if there is more data
                                if (byteCount > 0)
                                {
                                    //Copy into our buffer
                                    Buffer.BlockCopy(data, offset, buffer.raw, 0, byteCount);

                                    //Change the volume
                                    Audio.AdjustVolume(ref buffer, this.volume);

                                    //TODO: try { stream.write ... } catch { sleep }

                                    //Send data
                                    this.audioOutStream.Write(buffer.raw, 0, byteCount);

                                    //Update offset
                                    offset += byteCount;
                                }
                                else
                                {
                                    //Flush the stream
                                    this.audioOutStream.Flush();

                                    //Stop
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            //Log error
                            Logger.Log(LOG_LEVEL.ERROR, ex.Message);
                        }

                        //Set source to null
                        this.source = null;

                        //Set skip to false
                        this.skip = false;
                    }
                    else
                    {
                        //Sleep
                        Thread.Sleep(1000);
                    }

                    //TODO: Use a semaphore here to block this thread instead of sleeping / spinning

                    //Check if we're connected
                    if 
                    (
                        (this.audioClient?.ConnectionState ?? ConnectionState.Disconnected) == ConnectionState.Connected &&
                         this.audioOutStream != null
                    )
                    {
                        //Get the next song
                        var next = this.GetNextSong();

                        //Set new source
                        this.source = next;

                        //Update currently playing
                        Bot.GetBotInstance().SetStatus(this.source?.name);

                        //Trigger download if not already downloading
                        try { this.downloadSemaphore.Release(); } catch (Exception) { }
                    }
                }
            }, TaskCreationOptions.LongRunning)
            //Capture exit
            .ContinueWith((t) =>
            {
                //Get errors
                t.Exception?.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });

                //Log exit
                Logger.Log(LOG_LEVEL.ERROR, "Audio streamer exited!");

                //Set pointer to null
                this.audioStreamer = null;
            });
        }

        private void QueueSong(Song s)
        {
            //Lock to prevent race conditions
            lock (this.songQueueLock)
            {
                //Add song to queue
                this.songQueue.Enqueue(s);
            }
        }

        private void QueueDownload(VideoInfo videoInfo)
        {
            //Lock to prevent race conditions
            lock (this.downloadQueueLock)
            {
                //Push onto the queue
                this.downloadQueue.Enqueue(videoInfo);

                //Trigger download if not already downloading
                try { this.downloadSemaphore.Release(); } catch (Exception) { }
            }
        }

        private Song? GetNextSong()
        {
            //Lock to prevent race conditions
            lock (this.songQueueLock)
            {
                //Check if the queue has any elements
                if (this.songQueue.Count > 0)
                {
                    //Pop an element off of the queue
                    return this.songQueue.Dequeue();
                }
            }

            //Return failure
            return null;
        }

        private VideoInfo? GetNextDownload()
        {
            //Lock to prevent race conditions
            lock (this.downloadQueueLock)
            {
                //Check if the queue has any elements
                if (this.downloadQueue.Count > 0)
                {
                    //Pop an element off of the queue
                    return this.downloadQueue.Dequeue();
                }
            }

            //Get the next song from our default playlist
            return null;//this.defaultPlaylist?.GetNext();
        }
    }
}
