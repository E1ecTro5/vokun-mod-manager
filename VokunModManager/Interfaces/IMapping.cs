namespace VokunModManager.Interfaces;

public interface IMapping
{
    public string Source { get; set; }
    public string Destination { get; set; }
    public int Priority { get; set; }
}