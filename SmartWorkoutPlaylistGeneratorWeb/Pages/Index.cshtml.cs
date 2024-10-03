using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

using SmartWorkoutPlaylistGenerator;

public class IndexModel : PageModel
{
    private readonly PlaylistManager _playlistManager;

    public IndexModel(PlaylistManager playlistManager)
    {
        _playlistManager = playlistManager;
    }

    [BindProperty]
    public string PlaylistName { get; set; }

    [BindProperty]
    public string WorkoutType { get; set; }

    [BindProperty]
    public string DesiredMood { get; set; }

    public List<Song> MatchingSongs { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            string playlistId = await _playlistManager.FindPlaylistIdByNameAsync(PlaylistName);
            await _playlistManager.ImportPlaylistFromSpotifyAsync(playlistId);
            MatchingSongs = _playlistManager.GetMatchingSongs(WorkoutType, DesiredMood);

            if (MatchingSongs.Any())
            {
                _playlistManager.ExportMatchingSongsToCsv(MatchingSongs, "matching_songs.csv");
            }
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"An error occurred: {ex.Message}");
        }

        return Page();
    }
}