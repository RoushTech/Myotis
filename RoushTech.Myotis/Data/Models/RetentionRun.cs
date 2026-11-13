namespace RoushTech.Myotis.Data.Models;

public class RetentionRun
{
    public Guid Id { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int DeletedCount { get; set; }

    public string? Error { get; set; }
}
