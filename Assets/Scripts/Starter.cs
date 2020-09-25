using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class StaticClass {
    public static AudioClip Bgm { get; set; }
    public static TextAsset Script { get; set; }
    public static bool Auto { get; set; } = false;
    
    public static string Name { get; set; }

    public static float Sensibility { get; set; } = 0f;
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

    void Start()
    {
        sens.value = PlayerPrefs.HasKey("sens") ? PlayerPrefs.GetFloat("sens") : StaticClass.Sensibility;
        for (var i = 0; i < names.Length; i++)
        {
            var b = Instantiate(firstButton, parent, true);
            b.transform.Find("Name").GetComponent<Text>().text = names[i];
            const string s = "★";
            var startString = "";
            for (var j = 0; j < hardness[i]; j++)
            {
                startString += s;
            }

            b.transform.Find("Star").GetComponent<Text>().text = startString;
            var pos = b.transform.position;
            b.transform.position = new Vector3(pos.x, pos.y - i * 240, pos.z);
            b.transform.Find("Image").GetComponent<RawImage>().texture = images[i];
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

        var rect = parent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 300 + names.Length * 240);
    }

    private void StartGame()
    {
        var obj = EventSystem.current.currentSelectedGameObject;
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
                PlayerPrefs.SetFloat("sens", StaticClass.Sensibility);
                SceneManager.LoadScene("Music");
                break;
            }
        }
    }
}
