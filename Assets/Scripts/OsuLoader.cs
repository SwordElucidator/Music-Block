

using System;
using System.IO;

public struct SongInfo
{
    // public string AudioFilename;
    // public int AudioLeadIn;  // Milliseconds of silence before the audio starts playing
    // public int PreviewTime;  // Time in milliseconds when the audio preview should start
    // public int Mode;
    public string Title;
    public string TitleUnicode;
    public string Artist;
    public string ArtistUnicode;
    public string Version;
    public string Source;
    public string[] Tags;
    public int CircleSize;
    public int ApproachRate;
    public int BeatmapID;
    public int BeatmapSetID;
}


public class OsuLoader
{
    private readonly FileInfo _file;
    
    public OsuLoader(FileInfo file)
    {
        _file = file;
    }
    

    public SongInfo ReadSongInfo()
    {
        var reader = _file.OpenText();
        var block = "";
        var info = new SongInfo();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null) continue;
            if (line.Length >= 2 && line[0] == '[' && line[line.Length - 1] == ']')
            {
                block = line;
                if (block == "[Events]") break;
                continue;
            }
            if (block == "[Metadata]")
            {
                if (line.StartsWith("Title:")) info.Title = line.Split(new char[]{':'}, 2)[1];
                if (line.StartsWith("TitleUnicode:")) info.TitleUnicode = line.Split(new char[]{':'}, 2)[1];
                if (line.StartsWith("Artist:")) info.Artist = line.Split(new char[]{':'}, 2)[1];
                if (line.StartsWith("ArtistUnicode:")) info.ArtistUnicode = line.Split(new char[]{':'}, 2)[1];
                if (line.StartsWith("Version:")) info.Version = line.Split(new char[]{':'}, 2)[1];
                if (line.StartsWith("Source:")) info.Source = line.Split(new char[]{':'}, 2)[1];
                if (line.StartsWith("Tags:")) info.Tags = line.Split(new char[]{':'}, 2)[1].Split(' ');
                if (line.StartsWith("BeatmapID:")) info.BeatmapID = int.Parse(line.Split(new char[] {':'}, 2)[1]);
                if (line.StartsWith("BeatmapSetID:")) info.BeatmapSetID = int.Parse(line.Split(new char[] {':'}, 2)[1]);
            }else if (block == "[Difficulty]")
            {
                if (line.StartsWith("CircleSize:")) info.CircleSize = int.Parse(line.Split(new char[]{':'}, 2)[1]);
                if (line.StartsWith("ApproachRate:")) info.ApproachRate = int.Parse(line.Split(new char[]{':'}, 2)[1]);
            }
        }
        return info;
    }
}
