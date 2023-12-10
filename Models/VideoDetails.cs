namespace youtubeAPI.Models
{
    public class VideoDetails
    {
        public string? Title { get; set; }
        public string? Link { get; set; }
        public string? Thumbnail { get; set; }
        public string? Duration { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
