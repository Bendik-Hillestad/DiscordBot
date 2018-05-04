using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Runtime.InteropServices;

using Discord;
using Discord.WebSocket;
using Discord.Audio;

using DiscordBot.YT;
using DiscordBot.ST;
using DiscordBot.Utils;

namespace DiscordBot.Core
{
    public sealed partial class Bot
    {
#       pragma warning disable IDE1006
        [DllImport("CppUtils")]
        private static unsafe extern void s16le_vector_scalar_multiply(Int16* vec, Int32 count, Int32 scalar);
#       pragma warning restore IDE1006

        [CommandInit]
        private void ConstructMusicCommands()
        {
            //Try to activate YouTube API
            if (!string.IsNullOrEmpty(this.config.youtube_api_key))
            {
                this.youtubeAPIKey = this.config.youtube_api_key;
            }

            //Try to activate Spotify API
            if (!string.IsNullOrEmpty(this.config.spotify_client_id) && !string.IsNullOrEmpty(this.config.spotify_client_secret))
            {
                this.spotifyClientID     = this.config.spotify_client_id;
                this.spotifyClientSecret = this.config.spotify_client_secret;
            }

            //Register our Music command category + commands
            this.commandCategories.Add
            (
                new CommandCategory("music", thistype, "CmdMusicHelp")
                .RegisterCommand
                (
                    new Command
                    (
                        "join", this, "CmdMusicJoin", "CmdMusicJoinHelp"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "play", this, "CmdMusicPlay", "CmdMusicPlayHelp",
                        "URL or search terms", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "playlist", this, "CmdMusicPlaylist", "CmdMusicPlaylistHelp",
                        "URL or search terms", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "volume", this, "CmdMusicVolume", "CmdMusicVolumeHelp",
                        "value", @"(\d+)(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "skip", this, "CmdMusicSkip", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "leave", this, "CmdMusicLeave", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "np", this, "CmdMusicNP", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "queue", this, "CmdMusicQueue", null
                    )
                )
            );

            //Create an additional category for the shortened version
            this.commandCategories.Add
            (
                new CommandCategory(null, null, null)
                .RegisterCommand
                (
                    new Command
                    (
                        "play", this, "CmdMusicPlay", "CmdMusicPlayHelp",
                        "music",               @"music(?:$|\s)",
                        "URL or search terms", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "play", this, "CmdMusicPlay", "CmdMusicPlayHelp",
                        "URL or search terms", @"(.+?)$"
                    )
                )
            );

            //Initialise our fields
            this.volume             = 30;
            this.audioStreamer      = null;
            this.audioClient        = null;
            this.audioChannel       = null;
            this.audioOutStream     = null;
            this.source             = null;
            this.skip               = false;
            this.songQueue          = new Queue<Song>();
            this.downloadQueue      = new Queue<VideoInfo>();
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

                            //Loop until skip is requested (or stream is over)
                            while (!this.skip)
                            {
                                //Calculate number of bytes in the next chunk
                                var byteCount = Math.Min(data.Length - offset, blockSize);

                                //Check if there is more data
                                if (byteCount > 0)
                                {
                                    //Setup an unsafe section so we can change the volume with our fast C code
                                    unsafe
                                    {
                                        //Get a pointer to the data
                                        fixed (byte* ptr = &data[offset])
                                        {
                                            //Change the volume
                                            s16le_vector_scalar_multiply((short*)ptr, byteCount >> 1, (this.volume << 8) / 100);
                                        }
                                    }

                                    //TODO: try { stream.write ... } catch { sleep }

                                    //Send data
                                    this.audioOutStream.Write(data, offset, byteCount);

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
                        this.client.SetGameAsync(this.source?.name);

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

        private string CmdMusicJoin(SocketUserMessage msg)
        {
            //Catch any errors
            try
            {
                //Get the user's channel
                var channel = (msg.Author as IGuildUser)?.VoiceChannel;

                //Check that it's not null
                if (channel != null)
                {
                    //Check if we're already connected to a channel
                    if ((this.audioClient?.ConnectionState ?? ConnectionState.Disconnected) == ConnectionState.Connected)
                    {
                        //Disconnect first
                        this.audioOutStream.Flush();
                        this.audioOutStream.Dispose();
                        this.audioClient.   StopAsync().GetAwaiter().GetResult();
                        this.audioOutStream = null;
                        this.audioClient    = null;
                        this.audioChannel   = null;
                        
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
                    return "Joined " + channel.Name + ".";
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

                //Clear audio client
                this.audioClient = null;
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                //Clear audio client
                this.audioClient = null;
            }

            //Return failure
            return "Couldn't join.";
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

        private string CmdMusicPlay(SocketUserMessage _, string request)
        {
            //Try to get the id if there is one
            var id = GetYouTubeVideoID(request);

            //Check if it's null
            if (id == null)
            {
                //Search for it instead
                var response = YouTube.SearchVideo(request, this.youtubeAPIKey);

                //Check if it was successful
                if (response != null && (response.items?.Length ?? 0) > 0)
                {
                    //Get the first item
                    id = response.items[0].id.videoId;
                }
                else
                {
                    //Return error
                    return "Couldn't find video.";
                }
            }

            //Try to execute
            return Debug.Try<string>(() =>
            {
                //Get video info
                var videoInfo = YouTube.GetVideoInfo(id);

                //Check that it's not null
                Debug.Assert(videoInfo != null, "Video info is null.");

                //Calculate duration
                int dur = Regex.Match(videoInfo.Value.length, @"^(\d+)(?:\:(\d+))?(?:\:(\d+))?$").Groups
                               .Select((g) => g?.Value).Where((s) => !string.IsNullOrWhiteSpace(s))
                               .Skip(1).Reverse()
                               .Select((s, i) => int.Parse(s) * (int)(Math.Pow(60, i) + 0.5))
                               .Sum();

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
                    return $"{title} [{Utility.PadNum(dur / 60)}:{Utility.PadNum(dur % 60)}] was added to the queue.";
                }
                else
                {
                    //Find the extension
                    var regex = Regex.Match(videoInfo.Value.name, @"(?:\.([^.]+?)$)");

                    //Extract the title
                    var title = videoInfo.Value.name.Substring(0, regex.Index);

                    //Return failure
                    return $"ERROR: {title} [{videoInfo.Value.length}] could not be added.\nIt's too long.";
                }
            }, "Couldn't get video info.");
        }

        private string CmdMusicPlayHelp(SocketUserMessage _)
        {
            return "The syntax for \"$music play\" is \"$music play [youtube-link or name]\".\n" +
                   "For example: \n" +
                   "    \"$music play never gonna give you up\"\n" +
                   "    or\n" +
                   "    \"$music play <https://www.youtube.com/watch?v=dQw4w9WgXcQ>\"\n" +
                   "The <https://youtu.be/dQw4w9WgXcQ> style links work as well." +
                   "Alternatively, you can type \"$play music\" or simply \"$play\".";
        }

        private string CmdMusicPlaylist(SocketUserMessage _, string request)
        {
           // return "Temporarily disabled";

            //Try to get the id if there is one
            var stPlaylistID = GetSpotifyPlaylistID(request);
            var ytPlaylistID = GetYouTubePlaylistID(request);
            var videoID      = GetYouTubeVideoID   (request);

            //Check if we didn't extract a playlist id
            if (stPlaylistID == null && ytPlaylistID == null)
            {
                //Search for it instead
                var response = YouTube.SearchPlaylist(request, this.youtubeAPIKey);

                //Check if it was successful
                if (response != null && (response.items?.Length ?? 0) > 0)
                {
                    //Get the first item
                    ytPlaylistID = response.items[0].id.playlistId;
                }
                else
                {
                    //Return error
                    return "Couldn't find playlist.";
                }
            }

            //Check if it's a youtube playlist
            if (ytPlaylistID != null)
            {
                //Extract playlist
                //var playlist = YouTubePlaylist.Create(ytPlaylistID/*,  videoID */);
                /*
                //Check if it was successful
                if (playlist != null)
                {
                    //Set playlist
                    this.defaultPlaylist = playlist;

                    //Return success
                    return "The playlist '" + playlist.Title + "' will now be used when nothing else is queued.";
                }
                */
                //Return error
                return "Couldn't extract videos from playlist.";
            }
            //Spotify playlist
            else
            {
                //Extract playlist
                var playlist = SpotifyPlaylist.Create(stPlaylistID[0], stPlaylistID[1], this.spotifyClientID, this.spotifyClientSecret);

                //Check if it was successful
                if (playlist != null)
                {
                    //Set playlist
                    this.defaultPlaylist = playlist;

                    //Return success
                    return "The playlist '" + playlist.Title + "' will now be used when nothing else is queued.";
                }

                //Return error
                return "Couldn't extract videos from playlist.";
            }
        }

        private string CmdMusicPlaylistHelp(SocketUserMessage _)
        {
            return "The syntax for \"$music playlist\" is \"$music playlist [youtube-link or name]\".\n" +
                   "For example: \n" +
                   "    \"$music playlist heart of thorns ost\"\n" +
                   "    or\n" +
                   "    \"$music playlist <https://www.youtube.com/playlist?list=PLUJ9TtKEOLBzJSE8KAX3lL_LrcITDxYV9>\"\n";
        }

        private string CmdMusicSkip(SocketUserMessage _)
        {
            //Check if we're connected
            if (this.audioClient != null)
            {
                //Check if we're playing something
                if (this.source != null)
                {
                    //Request skip
                    this.skip = true;

                    //Return success
                    return "Skipping.";
                }

                //Return error
                return "Not playing anything.";
            }

            //Return error
            return "Not connected to a voice channel.";
        }

        private string CmdMusicVolume(SocketUserMessage _, int value)
        {
            //Check that it's between 0 and 100
            if (value >= 0 && value <= 100)
            {
                //Set the volume
                this.volume = value;

                //Return success
                return "Volume was set to " + value + "%.";
            }

            //Return error
            return "The volume must be between 0 and 100";
        }

        private string CmdMusicVolumeHelp(SocketUserMessage _)
        {
            return "You must provide the volume as a natural number between 0 and 100.";
        }

        private string CmdMusicLeave(SocketUserMessage _)
        {
            //Disconnect
            this.audioOutStream?.Flush();
            this.audioOutStream?.Dispose();
            this.audioClient?.   StopAsync();
            this.audioOutStream = null;
            this.audioClient    = null;
            this.audioChannel   = null;

            //Return success
            return "Channel left.";
        }

        private string CmdMusicNP(SocketUserMessage _)
        {
            //Check if we're playing anything
            if (!string.IsNullOrWhiteSpace(this.source?.name))
            {
                //Reply with the title
                return "Now playing: " + this.source.Value.name;
            }

            //Return nothing
            return "Not playing anything.";
        }

        private string CmdMusicQueue(SocketUserMessage _)
        {
            //Grab the songs in the queue
            string queue = (!string.IsNullOrWhiteSpace(this.source?.name)) ? ("\n--> " + this.source.Value.name) : "";
            for (int i = 0; i < this.songQueue.Count; i++)
            {
                queue += "\n    " + this.songQueue.ElementAt(i).name;
            }

            //Show downloads
            for (int i = 0; i < Math.Min(this.downloadQueue.Count, 7); i++)
            {
                queue += "\n    " + (this.downloadQueue.ElementAt(i).name ?? "<unresolved>");
            }

            //Check if there were any songs
            if (!string.IsNullOrWhiteSpace(queue))
            {
                //Return queue
                return "This is the queue right now:```\n" + queue + "```";
            }

            //Return empty
            return "Queue is empty.";
        }

        private static string CmdMusicHelp()
        {
            return "These are the music commands available:\n" +
                   "    $music join\n" +
                   "    $music leave\n" +
                   "    $music play [youtube-link or name]\n" +
                   "    $music playlist [youtube-link, spotify-link or name]\n" +
                   "    $music volume [Value]\n" +
                   "    $music skip\n" +
                   "    $music np\n" +
                   "    $music queue\n";
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

        private string           youtubeAPIKey;
        private string           spotifyClientID;
        private string           spotifyClientSecret;
        private Task             audioStreamer;
        private Task             audioDownloader;
        private IVoiceChannel    audioChannel;
        private IAudioClient     audioClient;
        private AudioOutStream   audioOutStream;
        private bool             skip;
        private int              volume;
        private Song?            source;
        private Queue<Song>      songQueue;
        private Queue<VideoInfo> downloadQueue;
        private IPlaylist        defaultPlaylist;

        private readonly Semaphore downloadSemaphore = new Semaphore(0, 1);
        private readonly object    songQueueLock     = new object();
        private readonly object    downloadQueueLock = new object();
    }
}
