using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using DiscordBot.Core;
using DiscordBot.Modules.Music.Utility;
using DiscordBot.Modules.Music.YT;
using DiscordBot.Utils;

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

            //Try to get video info
            var tmp = YouTube.GetVideoInfo(id);

            //Check that it's not null
            Debug.Assert(tmp != null, "Video info is null.");

            //Get the value
            var videoInfo = tmp.Value;

            //Calculate duration
            int dur = Regex.Match(videoInfo.length, @"^(\d+)(?:\:(\d+))?(?:\:(\d+))?$").Groups
                           .Select(g => g?.Value).Where(s => !string.IsNullOrWhiteSpace(s))
                           .Skip  (1).Reverse()
                           .Select((s, i) => int.Parse(s) * (int)(Math.Pow(60, i) + 0.5))
                           .Sum   ();

            //Check that it's not too long
            if (dur < 36000)
            {
                //Store the duration
                videoInfo.length = dur.ToString();

                //Queue the music
                this.musicQueue.Enqueue(videoInfo);

                //Build the response
                var response = new EmbedBuilder().WithColor(Color.Blue)
                                                 .WithThumbnailUrl(videoInfo.thumb)
                                                 .AddField("Success", $"{videoInfo.name} [{videoInfo.length}] was added to the queue.")
                                                 .Build();

                //Send the response
                ctx.message.Channel.SendMessageAsync("", embed: response);
            }
            else
            {
                //Return video too long error
                Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel,
                    "Error",
                    $"{videoInfo.name} [{videoInfo.length}] could not be added.\nIt's too long."
                ); return;
            }
        }

        public void music_skip_impl(Context ctx)
        {
            //Check if we're connected
            Precondition.Assert(this.audioClient != null, "Not connected to a voice channel.");

            //Check if we're playing anything
            Precondition.Assert(this.current != null, "Not playing anything.");

            //Request skip
            this.skip = true;

            //Return success
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Success",
                "Skipping song" 
            );
        }

        public void music_queue_impl(Context ctx)
        {
            //Check if there is anything in the queue
            if (this.musicQueue.Count > 0)
            {
                //Grab the names
                var queue = this.musicQueue.Select((m, i) => ((i == 0) ? "--> " : "    ") + m.name)
                                .Aggregate((m1, m2) => $"{m1}\n{m2}");

                //Return queue
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Result:",
                    $"```{queue}```"
                ); return;
            }

            //Return empty
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Result:",
                "None"
            );
        }

        public void music_np_impl(Context ctx)
        {
            //Check if we're playing anything
            if (!string.IsNullOrWhiteSpace(this.current?.Name))
            {
                //Return the title
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Now playing:",
                    this.current.Name
                ); return;
            }

            //Return nothing
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Now playing:",
                "Nothing"
            );
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

        private void SpawnAudioStreamer()
        {
            //Spawn the streaming thread
            this.audioStreamer = Task.Factory.StartNew(() =>
            {
                //Setup block size
                const int blockSize = 3840;
                Span<byte> buf = stackalloc byte[blockSize];

                //Loop forever
                while (true)
                {
                    //Grab the next music to play
                    this.current = this.next;
                    this.next    = null;

                    //Check if need to pop something from the queue
                    if (this.current == null)
                    {
                        //Check if the queue has something
                        if (!this.musicQueue.IsEmpty)
                        {
                            //Pop a value from the queue
                            VideoInfo tmp;
                            while (!this.musicQueue.TryDequeue(out tmp));

                            //Generate a stream from it
                            this.current = YoutubeStream.CreateUnbuffered(tmp.id, tmp.name);
                        }
                    }

                    //Check if we have a value
                    if (this.current != null)
                    {
                        //Check if the queue has something
                        if (!this.musicQueue.IsEmpty)
                        {
                            //Peek at the front of the queue
                            VideoInfo tmp;
                            while (!this.musicQueue.TryPeek(out tmp));

                            //Check that it's not too long, we don't want to buffer long videos
                            if (int.Parse(tmp.length) < 1000)
                            {
                                //Pop it from the queue
                                while (!this.musicQueue.TryDequeue(out tmp));

                                //Generate a stream from it
                                this.next = YoutubeStream.CreateBuffered(tmp.id, tmp.name);
                            }
                        }
                    }
                    else
                    {
                        //Sleep and retry
                        Task.Delay(1000).GetAwaiter().GetResult();
                        continue;
                    }

                    //Playing loop
                    while (true)
                    {
                        //Check if a skip is requested
                        if (this.skip)
                        {
                            //Unset the flag
                            this.skip = false;

                            //Check if we need to cancel the buffering
                            if (this.current.IsBuffered)
                            {
                                //Cancel the buffering
                                this.current.Cancellation.Cancel();
                                this.current.WaitForExit();
                            }

                            //Stop playing
                            break;
                        }

                        //Sleep if we're not connected
                        bool connected = (this.audioClient != null) && (this.audioClient.ConnectionState == ConnectionState.Connected) &&
                                         (this.audioOutStream != null);
                        while (!connected) Task.Delay(1000).GetAwaiter().GetResult();

                        //Grab a block of samples
                        var count = this.current.ReadBlock(buf);

                        //Stop playing if we're done
                        if (count == 0)
                        {
                            this.audioOutStream.Flush();
                            break;
                        }

                        //Change the volume
                        //Audio.AdjustVolume(ref samples, this.volume);

                        //Send data
                        try   { this.audioOutStream.Write(buf.Slice(0, count)); }
                        catch {}
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
    }
}
