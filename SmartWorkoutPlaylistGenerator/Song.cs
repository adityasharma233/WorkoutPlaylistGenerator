namespace SmartWorkoutPlaylistGenerator{
 
        public class Song
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
            public int Tempo { get; set; }
            public float Energy { get; set; }
            public float Valence { get; set; }
            public string Mood { get; set; }
        }
}