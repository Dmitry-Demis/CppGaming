namespace cppQuest.Server.Models;

public class ParagraphReadingStats
{
    public int UserId { get; set; }
    public string ParagraphId { get; set; } = "";

    public int TotalReadingSeconds { get; set; }
    public int ReadingSessionsCount { get; set; }
    public int CodeRunsCount { get; set; }
    public long ScrollPixels { get; set; }   // суммарно прокручено пикселей

    public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

    // SM-2 spaced repetition: interval in days until next review
    public int SrInterval { get; set; } = 1;
    public float SrEaseFactor { get; set; } = 2.5f;
    public int SrRepetitions { get; set; } = 0;
    public DateTime? SrNextDue { get; set; }

    public User User { get; set; } = null!;
}
