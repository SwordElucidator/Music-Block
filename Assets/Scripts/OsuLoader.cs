

using System;
using System.Collections;
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
    
    // 扩展部分
    public string AudioFilename;
    public int AudioLeadIn;
    public int PreviewTime;
    public int Mode;
    
    // 变速部分todo

    public ArrayList Notes;
}


public class OsuLoader
{
    private readonly FileInfo _file;
    
    public OsuLoader(FileInfo file)
    {
        _file = file;
    }


    private static string _readLine(TextReader reader, string block, SongInfo info)
    {
        var line = reader.ReadLine();
        if (line == null) return block;
        if (line.Length >= 2 && line[0] == '[' && line[line.Length - 1] == ']')
        {
            return line;
        }

        if (block == "[General]")
        {
            if (line.StartsWith("AudioFilename:")) info.AudioFilename = line.Split(new char[]{':'}, 2)[1];
            if (line.StartsWith("AudioLeadIn:")) info.AudioLeadIn = int.Parse(line.Split(new char[] {':'}, 2)[1]);
            if (line.StartsWith("PreviewTime:")) info.PreviewTime = int.Parse(line.Split(new char[] {':'}, 2)[1]);
            if (line.StartsWith("Mode:")) info.Mode = int.Parse(line.Split(new char[] {':'}, 2)[1]);
        }
        else if (block == "[Metadata]")
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
        }else if (block == "[HitObjects]")
        {
            info.Notes.Add(int.Parse(line.Split(',')[2]));
        }
        return block;
    }
    
    public SongInfo DeepRead()
    {
        var reader = _file.OpenText();
        var block = "";
        var info = new SongInfo();
        info.Notes = new ArrayList();
        while (!reader.EndOfStream)
        {
            block = _readLine(reader, block, info);
        }
        reader.Close();
        return info;
    }
    

    public SongInfo ReadSongInfo()
    {
        var reader = _file.OpenText();
        var block = "";
        var info = new SongInfo();
        while (!reader.EndOfStream)
        {
            block = _readLine(reader, block, info);
            if (block == "[Events]") break;
        }
        reader.Close();
        return info;
    }
}
