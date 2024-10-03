# WorkoutPlaylistGenerator
The Workout Playlist Generator is a smart playlist generator that integrates with Spotify's API to create personalized workout playlists based on user preferences for workout type and mood. It categorizes songs from your Spotify playlists by mood and tempo, offering dynamic suggestions for cardio, strength training, or yoga workouts.

Features
Spotify Integration: Imports songs from your Spotify playlists using the Spotify API.
Mood and Workout Matching: Automatically matches songs to your desired workout type and mood (e.g., energetic, relaxed, focused).
Custom Playlists: Generates custom playlists for different workout types like cardio, strength, and yoga.
Audio Feature Analysis: Utilizes Spotify's audio features such as tempo, energy, and valence to determine the mood and suitability of songs.
Export to CSV: Exports the matching songs to a CSV file for easy access and review.
Project Structure
SmartWorkoutPlaylistGenerator: Core logic for interacting with the Spotify API, managing playlists, analyzing song features, and generating workout-specific song recommendations.
SmartWorkoutPlaylistGeneratorWeb: A web interface built using ASP.NET Core Razor Pages, allowing users to input their workout preferences and generate playlists via a user-friendly interface.
