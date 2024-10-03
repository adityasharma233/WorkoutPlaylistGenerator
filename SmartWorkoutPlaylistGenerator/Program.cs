using System;
using System.Linq;
using System.Threading.Tasks;
using SmartWorkoutPlaylistGenerator; // Ensure this namespace is correct and matches your project's namespace

public class Program // Remove 'partial' if not necessary
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Welcome to the Smart Workout Playlist Generator!");

        // Spotify API credentials
        string clientId = "SPOTIFY_CLIENT_ID";
        string clientSecret = "SPOTIFY_CLIENT_SECRET";
        string refreshToken = "SPOTIFY_REFRESH_TOKEN";

        var playlistManager = new PlaylistManager(clientId, clientSecret, refreshToken);

        try
        {
            string playlistName = GetUserInput("Enter the name of your Spotify playlist: ");
            string playlistId = await playlistManager.FindPlaylistIdByNameAsync(playlistName);
            
            Console.WriteLine("Importing playlist...");
            await playlistManager.ImportPlaylistFromSpotifyAsync(playlistId);

            var workoutType = GetUserInput("Enter workout type (Cardio/Strength/Yoga): ");
            var desiredMood = GetUserInput("Enter desired workout mood (Energetic/Relaxed/Focused): ");

            var matchingSongs = playlistManager.GetMatchingSongs(workoutType, desiredMood);

            if (matchingSongs.Any())
            {
                Console.WriteLine("\nMatching songs for your workout:");
                foreach (var song in matchingSongs)
                {
                    Console.WriteLine($"{song.Title} by {song.Artist} - Mood: {song.Mood}");
                }
            }
            else
            {
                Console.WriteLine("No matching songs found for your workout preferences.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            Console.WriteLine("Error details:");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("\nIf the error persists, please check the following:");
            Console.WriteLine("1. Ensure the playlist exists in your Spotify account and is accessible.");
            Console.WriteLine("2. Verify that your Spotify API credentials are correct and up-to-date.");
            Console.WriteLine("3. Check if you have the necessary permissions to access your playlists and their tracks.");
        }
    }

    static string GetUserInput(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine();
    }
}
