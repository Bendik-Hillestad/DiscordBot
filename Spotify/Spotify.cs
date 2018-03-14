using System;
using System.Net.Http;
using System.Text;

using Newtonsoft.Json;

using DiscordBot.YT;
using DiscordBot.Utils;

namespace DiscordBot.ST
{
    public static class Spotify
    {
        private static readonly string GET_ACCESS_TOKEN_URI = @"https://accounts.spotify.com/api/token";
        private static readonly string GET_PLAYLIST_URI     = @"https://api.spotify.com/v1/users/{0}/playlists/{1}?fields=name,tracks.items.track(album.artists,name)";

        private static Tuple<DateTime, string> cachedToken  = null;

        private struct AccessTokenResponse
        {
            public string access_token { get; set; }
            public string token_type   { get; set; }
            public int    expires_in   { get; set; }
        }

        private static string Base64Encode(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes);
        }

        private static string GetAccessToken(string clientID, string clientSecret)
        {
            //Check cache
            if (Spotify.cachedToken != null)
            {
                //Check that it has not expired
                if (DateTime.Now < cachedToken.Item1)
                {
                    //Return token
                    return Spotify.cachedToken.Item2;
                }
            }

            //Catch any errors
            try
            {
                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Put id and secret in the header
                    http.DefaultRequestHeaders.Add("Authorization", "Basic " + Base64Encode(clientID + ":" + clientSecret));

                    //Prepare the post data
                    var postData = new StringContent
                    (
                        "grant_type=client_credentials",
                         Encoding.UTF8,
                        "application/x-www-form-urlencoded"
                    );

                    //Send post request
                    var ret = http.PostAsync(GET_ACCESS_TOKEN_URI, postData).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Read the json text
                    var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    //Parse JSON
                    var data = JsonConvert.DeserializeObject<AccessTokenResponse>(jsonText);

                    //Cache data
                    Spotify.cachedToken = new Tuple<DateTime, string>
                    (
                        DateTime.Now + TimeSpan.FromSeconds(data.expires_in),
                        data.access_token
                    );

                    //Return token
                    return data.access_token;
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

        public static Response GetPlaylistInfo(string userID, string playlistID, string clientID, string clientSecret)
        {
            //Catch any errors
            try
            {
                //Get the access token
                var token = Spotify.GetAccessToken(clientID, clientSecret);

                //Check that it's not null
                if (token != null)
                {
                    //Format our request Uri
                    var requestUri = string.Format(GET_PLAYLIST_URI, userID, playlistID);

                    //Get a HttpClient
                    using (var http = new HttpClient())
                    {
                        //Spotify requires we put our key in the header
                        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

                        //Send the request
                        var ret = http.GetAsync(requestUri).GetAwaiter().GetResult();

                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();

                        //Read the json text
                        var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                        //Parse JSON and return Response object
                        return JsonConvert.DeserializeObject<Response>(jsonText);
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
    }

    public sealed class SpotifyPlaylist : IPlaylist
    {
        public string Title => this.title;

        public SpotifyPlaylist(string title, string[] playlist, int offset = 0)
        {
            this.title    = title;
            this.playlist = playlist;
            this.offset   = offset;
        }

        public static SpotifyPlaylist Create(string userID, string playlistID, string clientID, string clientSecret)
        {
            //Catch any errors
            try
            {
                //Get playlist info
                var info = Spotify.GetPlaylistInfo(userID, playlistID, clientID, clientSecret);

                //Check if it was successful
                if ((info?.tracks?.items?.Length ?? 0) > 0)
                {
                    //Prepare array to hold track names
                    var tracks = new string[info.tracks.items.Length];

                    //Iterate over tracks
                    for (int i = 0; i < info.tracks.items.Length; i++)
                    {
                        //Get artist
                        var artist = info.tracks.items[i].track.album.artists[0].name;

                        //Get track name
                        var track = info.tracks.items[i].track.name;

                        //Combine artist and track name
                        tracks[i] = $"{artist} - {track}";
                    }

                    //Return playlist
                    return new SpotifyPlaylist(info.name, tracks);
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

        /*public*/ void IPlaylist.SetOffset(int offset)
        {
            //Update offset
            this.offset = offset;
        }

        /*public*/ Song? IPlaylist.GetNext()
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
                return new Song { name = next };
            }
        }

        private string   title;
        private string[] playlist;
        private int      offset;

        private readonly object o = new object();
    }
}
