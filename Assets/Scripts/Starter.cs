using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class StaticClass {
    public static AudioClip Bgm { get; set; }
    public static TextAsset Script { get; set; }
    
    public static OsuLoader Loader { get; set; }
    public static bool Auto { get; set; } = false;
    
    public static string Name { get; set; }

    public static float Sensibility { get; set; } = 0f;

    public static bool IsOsu { get; set; } = false;
}


public class Starter : MonoBehaviour
{

    public GameObject firstButton;
    public Transform parent;
    public string[] names;
    public int[] hardness;
    public TextAsset[] scripts;
    public AudioClip[] bgms;
    public Texture[] images;

    public Toggle autoToggle;

    public Slider sens;
    // public bool auto = false;
    private Dictionary<int, OsuLoader> _loaderDict;

    private string _makeStar(int h)
    {
        const string s = "★";
        var startString = "";
        for (var j = 0; j < h; j++)
        {
            startString += s;
        }

        return startString;
    }

    void Start()
    {
        sens.value = PlayerPrefs.HasKey("sens") ? PlayerPrefs.GetFloat("sens") : StaticClass.Sensibility;
        for (var i = 0; i < names.Length; i++)
        {
            var b = Instantiate(firstButton, parent, true);
            b.transform.Find("Name").GetComponent<Text>().text = names[i];
            
            b.transform.Find("Star").GetComponent<Text>().text = _makeStar(hardness[i]);
            var pos = b.transform.position;
            b.transform.position = new Vector3(pos.x, pos.y - i * 240, pos.z);
            b.transform.Find("Mask").Find("Image").GetComponent<RawImage>().texture = images[i];
            if (PlayerPrefs.HasKey("high-score-" + names[i]))
            {
                var score = PlayerPrefs.GetFloat("high-score-" + names[i]);
                b.transform.Find("Score").GetComponent<Text>().text = Convert.ToInt32(score).ToString().PadLeft(7, '0');
            }else
            {
                b.transform.Find("Score").GetComponent<Text>().text = "0000000";
            }
            
            
            b.GetComponent<Button>().onClick.AddListener(StartGame);
        }
        
        
        const string fullPath = "Assets/Resources/data/";  
        
        _loaderDict = new Dictionary<int, OsuLoader>();
        
        //获取指定路径下面的所有资源文件  
        if (Directory.Exists(fullPath))
        {
            var direction = new DirectoryInfo(fullPath);
            var dirs = direction.GetDirectories();
            
            for (var j = 0; j < dirs.Length; j++)
            {
                var dir = dirs[j];
                // 这是一首曲子
                var image = dir.GetFiles("*.jpg")[0];
                var osuFiles = dir.GetFiles("*.osu");
                var osuLoader = new OsuLoader(osuFiles[0]);
                var info = osuLoader.ReadSongInfo();
                
                
                var b = Instantiate(firstButton, parent, true);
                // 需要标记
                _loaderDict[b.GetInstanceID()] = osuLoader;
                b.transform.Find("Name").GetComponent<Text>().text = info.TitleUnicode;
                b.transform.Find("Star").GetComponent<Text>().text = _makeStar((info.ApproachRate + info.CircleSize) / 2);
                var pos = b.transform.position;
                b.transform.position = new Vector3(pos.x, pos.y - (names.Length + j) * 240, pos.z);
                var tex = new Texture2D(1, 1);
                tex.LoadImage(File.ReadAllBytes(image.FullName));
                var img = b.transform.Find("Mask").Find("Image");
                img.GetComponent<RawImage>().texture = tex;
                img.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;
                if (PlayerPrefs.HasKey("high-score-" + info.BeatmapID))
                {
                    var score = PlayerPrefs.GetFloat("high-score-" + info.BeatmapID);
                    b.transform.Find("Score").GetComponent<Text>().text = Convert.ToInt32(score).ToString().PadLeft(7, '0');
                }else
                {
                    b.transform.Find("Score").GetComponent<Text>().text = "0000000";
                }
            
            
                b.GetComponent<Button>().onClick.AddListener(StartGame);
            }
        }

        var rect = parent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 300 + names.Length * 240);
    }

    private void StartGame()
    {
        var obj = EventSystem.current.currentSelectedGameObject;

        if (_loaderDict.ContainsKey(obj.GetInstanceID()))
        {
            StaticClass.Bgm = null;
            StaticClass.Script = null;
            StaticClass.Auto = autoToggle.isOn;
            StaticClass.Sensibility = sens.value;
            StaticClass.Name = null;
            StaticClass.IsOsu = true;
            StaticClass.Loader = _loaderDict[obj.GetInstanceID()];
            PlayerPrefs.SetFloat("sens", StaticClass.Sensibility);
            SceneManager.LoadScene("Music");
        }
        else
        {
            var n = obj.transform.Find("Name").GetComponent<Text>().text;
            for (var i = 0; i < names.Length; i++)
            {
                if (names[i] == n)
                {
                    var script = scripts[i];
                    var bgm = bgms[i];
                    // 开始
                    StaticClass.Bgm = bgm;
                    StaticClass.Script = script;
                    StaticClass.Auto = autoToggle.isOn;
                    StaticClass.Sensibility = sens.value;
                    StaticClass.Name = n;
                    StaticClass.IsOsu = false;
                    StaticClass.Loader = null;
                    PlayerPrefs.SetFloat("sens", StaticClass.Sensibility);
                    SceneManager.LoadScene("Music");
                    break;
                }
            }
        }
        
        
    }
}
