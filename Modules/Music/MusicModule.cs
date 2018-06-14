﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Audio;
using DiscordBot.Commands;
using DiscordBot.Core;
using DiscordBot.Utils;
using DiscordBot.YT;

namespace DiscordBot.Modules.Music
{
    public partial class MusicModule : CommandModule<MusicModule>
    {
        public override string ModuleName => "Music";

        [DllImport("opus", EntryPoint = "opus_get_version_string", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr OpusVersionString();
        [DllImport("libsodium", EntryPoint = "sodium_version_string", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr SodiumVersionString();

        public override void OnInit()
        {
            //Test dependencies
            var opusVersion = Marshal.PtrToStringAnsi(OpusVersionString());
            Logger.Log(LOG_LEVEL.INFO, $"Loaded opus with version string: {opusVersion}");
            var sodiumVersion = Marshal.PtrToStringAnsi(SodiumVersionString());
            Logger.Log(LOG_LEVEL.INFO, $"Loaded sodium with version string: {sodiumVersion}");

            //Make local copies of the config values we care about
            var bot = Bot.GetBotInstance();
            this.youtubeAPIKey       = (string)BotConfig.Config["youtube_api_key"];
            this.spotifyClientID     = (string)BotConfig.Config["spotify_client_id"];
            this.spotifyClientSecret = (string)BotConfig.Config["spotify_client_secret"];

            //Set the default volume to 30 (TODO: Remove this later? Users should honestly adjust the volume locally)
            this.volume              = 30;

            //Initialise our other fields to defaults
            this.audioStreamer       = null;
            this.audioClient         = null;
            this.audioChannel        = null;
            this.audioOutStream      = null;
            this.source              = null;
            this.skip                = false;
            this.songQueue           = new Queue<Song>();
            this.downloadQueue       = new Queue<VideoInfo>();
        }

        [Command("music join")]
        public void music_join(Context ctx)
        {
            //Get the user's channel
            var channel = (ctx.message.Author as IGuildUser)?.VoiceChannel;

            //Check that it's not null
            Precondition.Assert(channel != null, "No channel to join!");

            //Pass on to implementation
            this.music_join_impl(ctx, channel);
        }

        [Command("music leave")]
        public void music_leave(Context ctx)
        {
            //Pass on to implementation
            this.music_leave_impl(ctx);
        }

        [Command("music play {}")]
        public void music_play(Context ctx, [RegexParameter(@"[\S\s]+")] string song)
        {
            //Pass on to implementation
            this.music_play_impl(ctx, song);
        }

        [Command("music skip")]
        public void music_skip(Context ctx)
        {
            //Pass on to implementation
            this.music_skip_impl(ctx);
        }

        [Command("music queue")]
        public void music_queue(Context ctx)
        {
            //Pass on to implementation
            this.music_queue_impl(ctx);
        }

        [Command("music np")]
        public void music_np(Context ctx)
        {
            //Pass on to implementation
            this.music_np_impl(ctx);
        }

        public override string HelpMessage(Context ctx)
        {
            throw new NotImplementedException();
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
