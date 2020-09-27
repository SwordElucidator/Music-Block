

using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

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

    public AudioClip GetBgm(string name)
    {
        var path = Path.Combine(_file.Directory.FullName, name);
        string m = "";
        bool start = false;
        var items = path.Split(Path.DirectorySeparatorChar);
        for (var i = 0; i < items.Length; i++)
        {
            var item = items[i];
            if (item == "Resources")
            {
                start = true;
                continue;
            }
            if (!start) continue;
            if (i == items.Length - 1)
            {
                var temp = item.Split('.');
                for (var j = 0; j < temp.Length - 1; j++)
                {
                    m += temp[j] + ".";
                }

                m = m.Substring(0, m.Length - 1);
            }
            else
            {
                m += item + Path.DirectorySeparatorChar;
            }
            
        }

        return Resources.Load<AudioClip>(m);
    }


    private string _readLine(string line, string block, ref SongInfo info)
    {
        if (line == null) return block;
        if (line.Length >= 2 && line[0] == '[' && line[line.Length - 1] == ']')
        {
            return line;
        }

        if (block == "[General]")
        {
            if (line.StartsWith("AudioFilename:")) info.AudioFilename = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("AudioLeadIn:")) info.AudioLeadIn = int.Parse(line.Split(new char[] {':'}, 2)[1].Trim());
            if (line.StartsWith("PreviewTime:")) info.PreviewTime = int.Parse(line.Split(new char[] {':'}, 2)[1].Trim());
            if (line.StartsWith("Mode:")) info.Mode = int.Parse(line.Split(new char[] {':'}, 2)[1].Trim());
        }
        else if (block == "[Metadata]")
        {
            if (line.StartsWith("Title:")) info.Title = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("TitleUnicode:")) info.TitleUnicode = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("Artist:")) info.Artist = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("ArtistUnicode:")) info.ArtistUnicode = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("Version:")) info.Version = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("Source:")) info.Source = line.Split(new char[]{':'}, 2)[1].Trim();
            if (line.StartsWith("Tags:")) info.Tags = line.Split(new char[]{':'}, 2)[1].Trim().Split(' ');
            if (line.StartsWith("BeatmapID:")) info.BeatmapID = int.Parse(line.Split(new char[] {':'}, 2)[1].Trim());
            if (line.StartsWith("BeatmapSetID:")) info.BeatmapSetID = int.Parse(line.Split(new char[] {':'}, 2)[1].Trim());
        }else if (block == "[Difficulty]")
        {
            if (line.StartsWith("CircleSize:")) info.CircleSize = int.Parse(line.Split(new char[]{':'}, 2)[1].Trim());
            if (line.StartsWith("ApproachRate:")) info.ApproachRate = int.Parse(line.Split(new char[]{':'}, 2)[1].Trim());
        }else if (block == "[HitObjects]")
        {
            info.Notes.Add(int.Parse(line.Split(',')[2].Trim()));
        }
        return block;
    }
    
    public SongInfo DeepRead()
    {
        var reader = _file.OpenText();
        var block = "";
        var info = new SongInfo {Notes = new ArrayList()};
        while (!reader.EndOfStream)
        {
            block = _readLine(reader.ReadLine(), block, ref info);
        }
        reader.Close();
        return info;
    }
    

    public SongInfo ReadSongInfo()
    {
        var reader = _file.OpenText();
        var block = "";
        var info = new SongInfo {Notes = new ArrayList()};
        while (!reader.EndOfStream)
        {
            block = _readLine(reader.ReadLine(), block, ref info);
            if (block == "[Events]") break;
        }
        reader.Close();
        return info;
    }
}
