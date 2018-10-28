using System.Net.Http;
using System.Web;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

namespace DiscordBot.Modules.Music.YT
{
    public static class YouTube
    {
        private static readonly string SEARCH_VIDEO_QUERY_URI    = @"https://www.googleapis.com/youtube/v3/search?part=snippet&type=video&maxResults=1&q={0}&key={1}";
        private static readonly string SEARCH_PLAYLIST_QUERY_URI = @"https://www.googleapis.com/youtube/v3/search?part=snippet&type=playlist&maxResults=1&q={0}&key={1}";

        public static Response SearchVideo(string searchString, string apiKey)
        {
            //Try to execute
            return Utils.Debug.Try<Response>(() =>
            {
                //Encode the query string
                var encodedQuery = HttpUtility.UrlEncode(searchString);

                //Format our request Uri
                var requestUri = string.Format(SEARCH_VIDEO_QUERY_URI, encodedQuery, apiKey);

                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Send the request
                    var ret = http.GetAsync(requestUri).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Read the json text
                    var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    //Parse JSON and return Response object
                    return JsonConvert.DeserializeObject<Response>(jsonText);
                }
            }, null);
        }

        public static Response SearchPlaylist(string searchString, string apiKey)
        {
            //Try to execute
            return Utils.Debug.Try<Response>(() =>
            {
                //Encode the query string
                var encodedQuery = HttpUtility.UrlEncode(searchString);

                //Format our request Uri
                var requestUri = string.Format(SEARCH_PLAYLIST_QUERY_URI, encodedQuery, apiKey);

                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Send the request
                    var ret = http.GetAsync(requestUri).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Read the json text
                    var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    //Parse JSON and return Response object
                    return JsonConvert.DeserializeObject<Response>(jsonText);
                }
            }, null);
        }

        public static VideoInfo? GetVideoInfo(string id)
        {
            //Try to execute
            return Utils.Debug.Try<VideoInfo?>(() =>
            {
                //Run youtube-dl
                var ytdl = Process.Start(new ProcessStartInfo
                {
                    FileName               = $"youtube-dl",
                    Arguments              = $"--prefer-insecure -e --get-thumbnail --get-duration {id}",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true
                });

                //Prepare video info structure
                VideoInfo info;
                info.id = id;

                //Get the output
                var str = ytdl.StandardOutput.ReadToEndAsync().GetAwaiter().GetResult();
                Utils.Debug.Assert(!string.IsNullOrWhiteSpace(str), "No youtube-dl output");

                //Split by lines
                var lines = Regex.Split(str, @"\r\n|\r|\n");

                //Check that it's valid
                Utils.Debug.Assert(lines != null,     "Lines are null");
                Utils.Debug.Assert(lines.Length >= 2, "Didn't get enough output");

                //Get title
                info.name = lines[0];

                //Get thumbnail
                info.thumb = lines[1];

                //Get the duration
                info.length = lines[2];

                //Check that the values are not null
                Utils.Debug.Assert(!string.IsNullOrWhiteSpace(info.id),     "ID is null");
                Utils.Debug.Assert(!string.IsNullOrWhiteSpace(info.name),   "Name is null");
                Utils.Debug.Assert(!string.IsNullOrWhiteSpace(info.length), "Length is null");
                Utils.Debug.Assert(!string.IsNullOrWhiteSpace(info.thumb),  "Thumb is null");

                //Return the info
                return info;
            }, null);
        }
    }
        /*public static Playlist GetPlaylistInfo(string id)
        {
            //Catch any errors
            try
            {
                //Start the client
                YoutubeClient client = new YoutubeClient();

                //Get the video info
                return client.GetPlaylistAsync(id).GetAwaiter().GetResult();
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

    public sealed class YouTubePlaylist : IPlaylist
    {
        public string Title => this.title;

        public YouTubePlaylist(string title, Song[] playlist, int offset = 0)
        {
            this.title    = title;
            this.playlist = playlist;
            this.offset   = offset;
        }

        public static YouTubePlaylist Create(string playlistID, int offset = 0)
        {
            //Catch any errors
            try
            {
                //Get the playlist info
                var playlistInfo = YouTube.GetPlaylistInfo(playlistID);

                //Allocate array to hold songs
                var arr = new Song[playlistInfo.Videos.Count];

                //Iterate over songs in the playlist
                for (int i = 0; i < playlistInfo.Videos.Count; i++)
                {
                    //Insert song
                    arr[i] = new Song { name = playlistInfo.Videos[i].Title, id = playlistInfo.Videos[i].Id };
                }

                //Return playlist
                return new YouTubePlaylist(playlistInfo.Title, arr, offset);
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

            //Return error
            return null;
        }

        void IPlaylist.SetOffset(int offset)
        {
            //Update offset
            this.offset = offset;
        }

        Song? IPlaylist.GetNext()
        {
            //Lock to prevent race conditions
            lock (this.o)
            {
                //Check if our playlist is null
                if (this.playlist == null) return null;

                //Grab next item
                var next = this.playlist[this.offset % this.playlist.Length];

                //Increment offset
                this.offset++;

                //Return item
                return next;
            }
        }

        private string title;
        private Song[] playlist;
        private int    offset;

        private readonly object o = new object();
    }*/
}
