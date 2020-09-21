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
    // public bool auto = false;

    void Start()
    {
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
            b.GetComponent<Button>().onClick.AddListener(StartGame);
        }

        var rect = parent.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 270 + names.Length * 240);
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
                // 
                StaticClass.Bgm = bgm;
                StaticClass.Script = script;
                StaticClass.Auto = autoToggle.isOn;
                SceneManager.LoadScene("Music");
                break;
            }
        }
    }
}
