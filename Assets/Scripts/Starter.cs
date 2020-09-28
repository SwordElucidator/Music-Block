using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LightBuzz.Archiver;
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
    public GameObject downloading;
    public Transform parent;
    public string[] names;
    public int[] hardness;
    public TextAsset[] scripts;
    public AudioClip[] bgms;
    public Texture[] images;

    public Toggle autoToggle;

    public Slider sens;
    // public bool auto = false;
    private Dictionary<Button, OsuLoader> _loaderDict;

    private string _makeStar(int h)
    {
        const string s = "★";
        var startString = "";
        for (var j = 0; j < Math.Min(h, 10); j++)
        {
            startString += s;
        }

        return startString;
    }

    private void PlaceItems()
    {
        sens.value = PlayerPrefs.HasKey("sens") ? PlayerPrefs.GetFloat("sens") : StaticClass.Sensibility;
        for (var i = 0; i < names.Length; i++)
        {
            var b = Instantiate(firstButton, parent, true);
            b.transform.Find("Name").GetComponent<Text>().text = names[i];
            
            var mapHolder = b.transform.Find("Maps").Find("Viewport").Find("Content");
            var mapBtn = mapHolder.Find("MapButton");
            var map = Instantiate(mapBtn, mapHolder, true);
            
            map.transform.Find("Star").GetComponent<Text>().text = _makeStar(hardness[i]);
            Transform transform1;
            (transform1 = map.transform).Find("Title").GetComponent<Text>().text = "Standard";
            var posMap = transform1.position;
            transform1.position = new Vector3(posMap.x, posMap.y, posMap.z);
            
            var pos = b.transform.position;
            b.transform.position = new Vector3(pos.x, pos.y - i * 420, pos.z);
            b.transform.Find("Mask").Find("Image").GetComponent<RawImage>().texture = images[i];
            
            if (PlayerPrefs.HasKey("high-score-" + names[i]))
            {
                var score = PlayerPrefs.GetFloat("high-score-" + names[i]);
                map.transform.Find("Score").GetComponent<Text>().text = Convert.ToInt32(score).ToString().PadLeft(7, '0');
            }else
            {
                map.transform.Find("Score").GetComponent<Text>().text = "0000000";
            }
            
            map.GetComponent<Button>().onClick.AddListener(StartGame);
        }
        
        
        var fullPath = Path.Combine(Application.persistentDataPath, "songs");  
        
        _loaderDict = new Dictionary<Button, OsuLoader>();

        var dirLength = 0;
        //获取指定路径下面的所有资源文件  

        if (Directory.Exists(fullPath))
        {
            var direction = new DirectoryInfo(fullPath);
            var dirs = direction.GetDirectories();
            dirLength = dirs.Length;
            for (var j = 0; j < dirs.Length; j++)
            {
                var dir = dirs[j];
                
                var imgs = dir.GetFiles("*.jpg");
                if (imgs.Length == 0)
                {
                    imgs = dir.GetFiles("*.png");
                }
                if (imgs.Length == 0)
                {
                    imgs = dir.GetFiles("*.jpeg");
                }

                if (imgs.Length == 0)
                {
                    print(dir);
                }

                var image = imgs[0];
                var osuFiles = dir.GetFiles("*.osu");
                // 外button位置
                var b = Instantiate(firstButton, parent, true);
                var pos = b.transform.position;
                b.transform.position = new Vector3(pos.x, pos.y - (names.Length + j) * 420, pos.z);
                var mapHolder = b.transform.Find("Maps").Find("Viewport").Find("Content");
                
                for (var k = 0; k < osuFiles.Length; k++)
                {
                    var osuLoader = new OsuLoader(osuFiles[k]);
                    var info = osuLoader.ReadSongInfo();
                    
                    var mapBtn = mapHolder.Find("MapButton");
                    var map = Instantiate(mapBtn, mapHolder, true);
                    
                    _loaderDict[map.GetComponent<Button>()] = osuLoader;
      
                    map.transform.Find("Star").GetComponent<Text>().text = _makeStar((info.ApproachRate + info.CircleSize + (int)(info.NoteLength / 100f)) / 3);

                    var bText = b.transform.Find("Name").GetComponent<Text>();
                    if (bText.text != info.TitleUnicode)
                    {
                        bText.text = info.TitleUnicode;
                        var tex = new Texture2D(1, 1);
                        tex.LoadImage(File.ReadAllBytes(image.FullName));
                        var img = b.transform.Find("Mask").Find("Image");
                        img.GetComponent<RawImage>().texture = tex;
                        img.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width / tex.height;   
                    }

                    if (PlayerPrefs.HasKey("high-score-" + info.BeatmapID))
                    {
                        var score = PlayerPrefs.GetFloat("high-score-" + info.BeatmapID);
                        map.transform.Find("Score").GetComponent<Text>().text = Convert.ToInt32(score).ToString().PadLeft(7, '0');
                    }else
                    {
                        map.transform.Find("Score").GetComponent<Text>().text = "0000000";
                    }
                    
                    Transform transform1;
                    (transform1 = map.transform).Find("Title").GetComponent<Text>().text = info.Version;
                    var posMap = transform1.position;
                    transform1.position = new Vector3(posMap.x + k * 320, posMap.y, posMap.z);
                    
                    map.GetComponent<Button>().onClick.AddListener(StartGame);   
                }
                
                mapHolder.GetComponent<RectTransform>().sizeDelta = new Vector2(osuFiles.Length * 320, 0);
            }
        }

        parent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 300 + (names.Length + dirLength) * 420);
    }

    private IEnumerator Download()
    {
        var uwr = new UnityWebRequest("https://vcz.oss-cn-beijing.aliyuncs.com/songs.zip", UnityWebRequest.kHttpVerbGET);
        string path = Path.Combine(Application.persistentDataPath, "songs.zip");
        uwr.downloadHandler = new DownloadHandlerFile(path);
        yield return uwr.SendWebRequest();
        if (uwr.isNetworkError || uwr.isHttpError)
            Debug.LogError(uwr.error);
        else
        {
            Debug.Log("File successfully downloaded and saved to " + path);
            var songsDir = Path.Combine(Application.persistentDataPath, "songs");
            if (!new DirectoryInfo(songsDir).Exists)
            {
                Directory.CreateDirectory(songsDir);
            }
            Archiver.Decompress ( path, songsDir, true);
            File.Delete(path);
            var dir = new DirectoryInfo(Path.Combine(Application.persistentDataPath, "songs"));
            var files = dir.GetFiles("*.osz");
            foreach (var file in files)
            {
                var des = Path.Combine(Application.persistentDataPath, "songs", file.Name.Split('.')[0]);
                if (!new DirectoryInfo(des).Exists)
                {
                    Directory.CreateDirectory(des);
                }
                Archiver.Decompress ( file.FullName, des, true);
                File.Delete(file.FullName);
            }
            downloading.SetActive(false);
            PlayerPrefs.SetString("downloaded1", "1");
            PlaceItems();
        }
            
        // System.IO
        // Application.persistentDataPath
    }
    
    void Start()
    {
        if (PlayerPrefs.HasKey("downloaded1"))
        {
            downloading.SetActive(false);
            PlaceItems();
        }else
        {
            StartCoroutine(Download());
        }
    }

    private void StartGame()
    {
        var obj = EventSystem.current.currentSelectedGameObject;
        // info.BeatmapID
        if (_loaderDict.ContainsKey(obj.GetComponent<Button>()))
        {
            StaticClass.Bgm = null;
            StaticClass.Script = null;
            StaticClass.Auto = autoToggle.isOn;
            StaticClass.Sensibility = sens.value;
            StaticClass.Name = null;
            StaticClass.IsOsu = true;
            StaticClass.Loader = _loaderDict[obj.GetComponent<Button>()];
            PlayerPrefs.SetFloat("sens", StaticClass.Sensibility);
            SceneManager.LoadScene("Music");
        }
        else
        {
            var n = obj.transform.parent.parent.parent.parent.Find("Name").GetComponent<Text>().text;
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
