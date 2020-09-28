

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public struct TimingPoint
{
    public int time;
    public float bpm;
    public float speedMultiplier;
}


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
    
    // 变速部分
    public List<TimingPoint> TimingPoints;
    
    public List<int> Notes;
    public int NoteLength;
}


public class OsuLoader
{
    private readonly FileInfo _file;
    
    public OsuLoader(FileInfo file)
    {
        _file = file;
    }

    public async Task<AudioClip> GetBgm(string name)
    {
        AudioClip clip = null;
        var path = "file://" + Path.Combine(_file.Directory.FullName, name);
        var uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.MPEG);
        uwr.SendWebRequest();
        try
        {
            while (!uwr.isDone) await Task.Delay(5);
 
            if (uwr.isNetworkError || uwr.isHttpError) Debug.Log($"{uwr.error}");
            else
            {
                Debug.Log("Found Clip");
                clip = DownloadHandlerAudioClip.GetContent(uwr);
            }
        }
        catch (Exception err)
        {
            Debug.Log($"{err.Message}, {err.StackTrace}");
        }

        return clip;
    }


    private string _readLine(string line, string block, ref SongInfo info, bool easy=false)
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
            if (line.StartsWith("CircleSize:")) info.CircleSize = (int)float.Parse(line.Split(new char[]{':'}, 2)[1].Trim());
            if (line.StartsWith("ApproachRate:")) info.ApproachRate = (int)float.Parse(line.Split(new char[]{':'}, 2)[1].Trim());
        }else if (block == "[TimingPoints]")
        {
            if (easy) return block;
            if (line.Trim().Length == 0) return block;
            var items = line.Split(',');
            var time = int.Parse(items[0]);
            CultureInfo ci = CultureInfo.CreateSpecificCulture("en-US");
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            var bpmOrSpeed = (float)double.Parse(items[1], ci);
            var tp = new TimingPoint();
            tp.time = time;
            if (bpmOrSpeed > 0)
            {
                tp.bpm = 1f / bpmOrSpeed * 1000f * 60f;
                tp.speedMultiplier = 1;
            }
            else
            {
                tp.bpm = info.TimingPoints.Last().bpm;
                tp.speedMultiplier = -100f / bpmOrSpeed;
            }
            
            info.TimingPoints.Add(tp);
        }else if (block == "[HitObjects]")
        {
            if (easy)
            {
                info.NoteLength += 1;
                return block;
            }
            if (line.Length > 0) info.Notes.Add(int.Parse(line.Split(',')[2].Trim()));
        }
        return block;
    }
    
    public SongInfo DeepRead()
    {
        var reader = _file.OpenText();
        var block = "";
        var info = new SongInfo {Notes = new List<int>(), TimingPoints = new List<TimingPoint>()};
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
        var info = new SongInfo {Notes = new List<int>(), TimingPoints = new List<TimingPoint>()};
        while (!reader.EndOfStream)
        {
            block = _readLine(reader.ReadLine(), block, ref info, true);
        }
        reader.Close();
        return info;
    }
}
