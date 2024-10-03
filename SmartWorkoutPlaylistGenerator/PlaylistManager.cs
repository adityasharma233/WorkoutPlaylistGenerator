using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace SmartWorkoutPlaylistGenerator{

    public class PlaylistManager
    {
        private List<Song> _playlist = new List<Song>();
        private const string SpotifyApiBaseUrl = "https://api.spotify.com/v1";
        private const string SpotifyAccountsUrl = "https://accounts.spotify.com/api/token";

        private string _accessToken;
        private DateTime _tokenExpirationTime;

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;

        public PlaylistManager(string clientId, string clientSecret, string refreshToken)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
        }

        private async Task EnsureValidTokenAsync()
        {
            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpirationTime)
            {
                await RefreshAccessTokenAsync();
            }
        }

        private async Task RefreshAccessTokenAsync()
        {
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, SpotifyAccountsUrl);

                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", _refreshToken)
                });
                request.Content = content;

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to refresh token. Status: {response.StatusCode}, Response: {responseBody}");
                }

                var tokenResponse = JsonConvert.DeserializeObject<JObject>(responseBody);

                _accessToken = tokenResponse["access_token"].ToString();
                int expiresIn = tokenResponse["expires_in"].Value<int>();
                _tokenExpirationTime = DateTime.UtcNow.AddSeconds(expiresIn - 60);  // Refresh 1 minute before expiration
            }
        }

        public async Task<string> FindPlaylistIdByNameAsync(string playlistName)
        {
            await EnsureValidTokenAsync();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                string nextUrl = $"{SpotifyApiBaseUrl}/me/playlists?limit=50";
                var allPlaylists = new List<JToken>();

                while (!string.IsNullOrEmpty(nextUrl))
                {
                    var response = await client.GetStringAsync(nextUrl);
                    var jsonResponse = JObject.Parse(response);

                    allPlaylists.AddRange(jsonResponse["items"]);
                    nextUrl = (string)jsonResponse["next"];
                }

                var bestMatch = allPlaylists
                    .Select(item => new 
                    { 
                        Name = (string)item["name"], 
                        Id = (string)item["id"],
                        Score = ComputeLevenshteinDistance(playlistName, (string)item["name"])
                    })
                    .OrderBy(x => x.Score)
                    .FirstOrDefault();

                if (bestMatch != null)
                {
                    Console.WriteLine($"Best matching playlist: {bestMatch.Name} (ID: {bestMatch.Id})");
                    return bestMatch.Id;
                }

                throw new Exception($"No playlist found matching '{playlistName}'.");
            }
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                d[0, j] = j;

            for (int j = 1; j <= t.Length; j++)
            {
                for (int i = 1; i <= s.Length; i++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }

            return d[s.Length, t.Length];
        }

        public async Task ImportPlaylistFromSpotifyAsync(string playlistId)
        {
            await EnsureValidTokenAsync();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                string nextUrl = $"{SpotifyApiBaseUrl}/playlists/{playlistId}/tracks?limit=100";

                Console.WriteLine($"Attempting to fetch tracks from playlist ID: {playlistId}");

                while (!string.IsNullOrEmpty(nextUrl))
                {
                    HttpResponseMessage response = await client.GetAsync(nextUrl);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to fetch playlist tracks. Status: {response.StatusCode}, URL: {nextUrl}, Response: {errorContent}");
                    }

                    string responseBody = await response.Content.ReadAsStringAsync();
                    var jsonResponse = JObject.Parse(responseBody);

                    foreach (var item in jsonResponse["items"])
                    {
                        var track = item["track"];
                        if (track != null && track.Type != JTokenType.Null && !string.IsNullOrEmpty((string)track["id"]))
                        {
                            var song = new Song
                            {
                                Id = (string)track["id"],
                                Title = (string)track["name"],
                                Artist = (string)track["artists"][0]["name"]
                            };

                            try
                            {
                                await EnrichSongWithAudioFeaturesAsync(song);
                                _playlist.Add(song);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Warning: Could not fetch audio features for track '{song.Title}' (ID: {song.Id}). Skipping this song.");
                            }
                        }
                    }

                    nextUrl = (string)jsonResponse["next"];
                }
            }

            Console.WriteLine($"Imported {_playlist.Count} songs from Spotify playlist.");
        }

        public void ExportMatchingSongsToCsv(List<Song> matchingSongs, string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine("Title,Artist,Mood,Tempo,Energy,Valence");
                foreach (var song in matchingSongs)
                {
                    writer.WriteLine($"{song.Title},{song.Artist},{song.Mood},{song.Tempo},{song.Energy},{song.Valence}");
                }
            }
            Console.WriteLine($"Exported matching songs to {fileName}");
        }

        private async Task EnrichSongWithAudioFeaturesAsync(Song song)
        {
            await EnsureValidTokenAsync();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

                HttpResponseMessage response = await client.GetAsync($"{SpotifyApiBaseUrl}/audio-features/{song.Id}");
                
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Failed to fetch audio features. Status: {response.StatusCode}, Track ID: {song.Id}, Response: {errorContent}");
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var audioFeatures = JsonConvert.DeserializeObject<JObject>(responseBody);

                song.Tempo = (int)audioFeatures["tempo"];
                song.Energy = (float)audioFeatures["energy"];
                song.Valence = (float)audioFeatures["valence"];

                song.Mood = InferMood(song);
            }
        }

        private string InferMood(Song song)
        {
            if (song.Energy > 0.7 && song.Tempo > 120)
                return "Energetic";
            else if (song.Energy < 0.4 && song.Valence < 0.4)
                return "Relaxed";
            else if (song.Energy > 0.5 && song.Valence > 0.6)
                return "Focused";
            else
                return "Neutral";
        }

        public List<Song> GetMatchingSongs(string workoutType, string desiredMood)
        {
            return _playlist
                .Where(s => MatchesMood(s.Mood, desiredMood) && MatchesWorkoutType(s, workoutType))
                .ToList();
        }

        private bool MatchesMood(string songMood, string desiredMood)
        {
            return songMood.Equals(desiredMood, StringComparison.OrdinalIgnoreCase);
        }

        private bool MatchesWorkoutType(Song song, string workoutType)
        {
            switch (workoutType.ToLower())
            {
                case "cardio":
                    return song.Tempo > 120 && song.Energy > 0.6;
                case "strength":
                    return song.Tempo > 100 && song.Tempo <= 120 && song.Energy > 0.5;
                case "yoga":
                    return song.Tempo < 100 && song.Energy < 0.5;
                default:
                    return true;
            }
        }
    }
}
