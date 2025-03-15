namespace MpvNet;

public class MediaTrack
{
    public int    Id       { get; set; }
    public bool   External { get; set; }
    public string Text     { get; set; } = "";
    public string Type     { get; set; } = "";
}
