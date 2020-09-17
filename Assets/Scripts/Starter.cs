using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
    public Transform canvas;
    public string[] names;
    public TextAsset[] scripts;
    public AudioClip[] bgms;
    public Texture[] images;
    public bool auto = false;

    void Start()
    {
        for (var i = 0; i < names.Length; i++)
        {
            var b = Instantiate(firstButton, canvas, true);
            b.transform.Find("Name").GetComponent<Text>().text = names[i];
            var pos = b.transform.position;
            b.transform.position = new Vector3(pos.x, pos.y - i * 240, pos.z);
            b.transform.Find("Image").GetComponent<RawImage>().texture = images[i];
            b.GetComponent<Button>().onClick.AddListener(StartGame);
        }
        firstButton.SetActive(false);
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
                StaticClass.Auto = auto;
                SceneManager.LoadScene("Music");
                break;
            }
        }
    }
}
