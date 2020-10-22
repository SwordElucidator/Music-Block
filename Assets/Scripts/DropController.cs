using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class DropController : MonoBehaviour
{

    public Camera cam;

    public Transform[] lines;

    public Transform terminalLine;

    public GameObject singleCube;

    public float speed = 3;

    public float tolerant = 0.2f;

    public Text text;
    public Text score;
    public Text combo;

    public GameObject screenDirectionNotifier;

    private int _combos = 0;  // 连续打击
    private float _score = 0;  // 分数
    private float[] _lastCheckLine;
    
    public AudioSource bgmAudio;

    public AudioSource beatEffect;
    
    // public AudioSource failEffect;

    public RawImage bg;
    
    private float _bpm;
    
    public TextAsset defaultScript;
    
    // osu模型
    private SongInfo _osuSongInfo;
    private bool loaded = false;
    private float _initialTime;
    
    private float _initialSpace;  // 计算后的初始时长（到第一个音符位置）
    private float _absoluteInitialSpace;  // 绝对初始时长
    private List<float> _notes = new List<float>();
    private int _leftNoteCount = 0;
    private int _nextNoteIndex = 0;
    

    private Dictionary<float, GameObject>[] cubes;
    // Start is called before the first frame update

    void CreateSingleCube(int line, float time)
    {
        var cube = Instantiate(singleCube);
        cube.transform.position = new Vector3(terminalLine.position.x + (time - Time.time) * speed, singleCube.transform.position.y, lines[line].transform.position.z);
        cubes[line][time] = cube;
    }

    private async void LoadScript()
    {
        var emptyBgmTime = 1f;
        if (StaticClass.IsOsu)
        {
            _osuSongInfo = StaticClass.Loader.DeepRead();
            if (StaticClass.imageTex)
            {
                bg.GetComponent<RawImage>().texture = StaticClass.imageTex;
                bg.GetComponent<AspectRatioFitter>().aspectRatio =
                    (float) StaticClass.imageTex.width / StaticClass.imageTex.height;
            }

            bgmAudio.clip = await StaticClass.Loader.GetBgm(_osuSongInfo.AudioFilename);
            _bpm = _osuSongInfo.TimingPoints[0].bpm;
            emptyBgmTime = _osuSongInfo.AudioLeadIn / 1000f;
            // _osuSongInfo.AudioLeadIn;   概念：audio进入时间

            // _initialSpace 从播放audio起，第一个音符前的等待时间
            // TODO 球速
            speed = _osuSongInfo.ApproachRate * 3;

            var first = true;
            foreach (var i in _osuSongInfo.Notes)
            {
                // 这是多少拍子？ 60 / bpm
                if (first)
                {
                    first = false;
                    _initialSpace = i / 1000f;  // 第一拍被称为_initialSpace
                    _notes.Add(i / 1000f);  // 特别的，第一拍也扔进去
                    _absoluteInitialSpace = _initialSpace;
                    continue;
                }
                _notes.Add(i / 1000f);
            }
        }else
        {
            var textList = (StaticClass.Script ? StaticClass.Script : defaultScript).text.Split('\n');
            
            if (StaticClass.Bgm)
            {
                bgmAudio.clip = StaticClass.Bgm;
            }

            CultureInfo ci = CultureInfo.CreateSpecificCulture("en-US");
            ci.NumberFormat.CurrencyDecimalSeparator = ".";
            _bpm = float.Parse(textList[0].Split(' ')[0], ci);  // 182节/分钟的话  
            _initialSpace = float.Parse(textList[0].Split(' ')[1], ci);
            _absoluteInitialSpace = _initialSpace;
            if (textList[0].Split(' ').Length > 2)
            {
                // 控制球速
                speed = float.Parse(textList[0].Split(' ')[2], ci);
            }
            // 添加步骤
            var lastTime = _absoluteInitialSpace;
            foreach (var i in textList[1].Split(' '))
            {
                var next = float.Parse(i, ci) * 60f / _bpm + lastTime;
                _notes.Add(next);
                lastTime = next;
            }
        }
        
        bgmAudio.Play();
        bgmAudio.Pause();
        
        tolerant = 60 / _bpm * 0.3f;
        
        // 起步倒计时
        _initialTime = Time.time;
        // print(_initialSpace);
        if (_initialSpace + emptyBgmTime < 60.0f / _bpm * 3)  // 小于3拍
        {
            // print("种类0");
            // 小于早期节拍   90(2秒） 0.18 + 1 < 2
            _initialSpace = 60.0f / _bpm * 3;
            StartCoroutine(WaitUntilUnpause(60.0f / _bpm * 3 - _initialSpace - StaticClass.SmallDelay));
        }
        else
        {
            // print("种类1");
            _initialSpace += emptyBgmTime;  // 初始时长 + 1秒
            StartCoroutine(WaitUntilUnpause(emptyBgmTime - StaticClass.SmallDelay));
        }
        // 准备就绪
        MakeSteps();
    }
    
    private IEnumerator WaitUntilUnpause(float time)
    {
        yield return new WaitForSeconds(time);
        bgmAudio.time = 0;
        bgmAudio.UnPause();
    }

    private void MakeSteps()
    {
        if (_nextNoteIndex >= _notes.Count) return;
        for (var i = _nextNoteIndex; i < Math.Min(_nextNoteIndex + 30, _notes.Count); i++)
        {
            // 先放30个
            CreateSingleCube((int)Math.Floor(Random.value * lines.Length), _initialTime +  _notes[i] - _absoluteInitialSpace + _initialSpace);
            _leftNoteCount += 1;
        }

        _nextNoteIndex = Math.Min(_nextNoteIndex + 30, _notes.Count);
        loaded = true;

    }
    
    void Start()
    {
        text.text = "";
        cubes = new Dictionary<float, GameObject>[lines.Length];
        for (var i = 0; i < cubes.Length; i++)
        {
            cubes[i] = new Dictionary<float, GameObject>();
        }
        _lastCheckLine = new float[cubes.Length];
        for (var i = 0; i < _lastCheckLine.Length; i++)
        {
            _lastCheckLine[i] = 0.0f;
        }

        LoadScript();
    }

    
    private void CheckLine(int i)
    {

        if (Time.time - _lastCheckLine[i] <= 0.05f)
        {
            return;
        }

        // 这里要计入一个"容忍时间"，即容忍的范畴里寻找最小的那个
        var exactClickTime = Time.time;
        _lastCheckLine[i] = exactClickTime;
        var minTolerateTime = Time.time - tolerant;
        var maxTolerateTime = Time.time + tolerant;
        var maxDeathTime = Time.time + tolerant * 2;
        var toKill = float.MinValue;

        GameObject colorChangeItem = null;
        var color = Color.black;
        
        foreach (var time in cubes[i].Keys.OrderBy(f => f))
        {
            if (time < minTolerateTime) continue;
            if (time >= maxDeathTime) break;
            
            // 否则结算
            // Destroy(cubes[i][time]);
            _leftNoteCount -= 1;
            toKill = time;
            
            if (time >= maxTolerateTime)
            {
                // 算没打上  TODO
                _combos = 0;
                combo.text =  _combos.ToString() + (_combos > 1 ? " Combos" : " Combo");
                colorChangeItem = cubes[i][time];
                color = new Color(1f, 0.16f, 0.15f);
                // cubes[i][time].GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else
            {
                _combos += 1;
                combo.text =  _combos.ToString() + (_combos > 1 ? " Combos" : " Combo");
                // 结算精度
                if (Math.Abs(exactClickTime - time) < tolerant * 0.4f)
                {
                    // pf
                    _score += 1000f * (1 + _combos / 100f);
                    colorChangeItem = cubes[i][time];
                    color = new Color(0.13f, 1f, 0.14f);
                    // cubes[i][time].GetComponent<MeshRenderer>().material.color = new Color(0.13f, 1f, 0.14f);
                }else if (Math.Abs(exactClickTime - time) < tolerant * 0.7f)
                {
                    _score += 1000f * (1 + _combos / 100f) * 0.6f;
                    colorChangeItem = cubes[i][time];
                    color = new Color(1f, 0.62f, 0.47f);
                    // cubes[i][time].GetComponent<MeshRenderer>().material.color = new Color(1f, 0.62f, 0.47f);
                }
                else
                {
                    _score += 1000f * (1 + _combos / 100f) * 0.2f;
                    colorChangeItem = cubes[i][time];
                    color = new Color(0.73f, 0.73f, 0.73f);
                    // cubes[i][time].GetComponent<MeshRenderer>().material.color = new Color(0.73f, 0.73f, 0.73f);
                }
                score.text = Convert.ToInt32(_score).ToString().PadLeft(7, '0');
                beatEffect.Play();
            }
            Destroy(cubes[i][time], 0.1f);
            break;
        }

        if (Math.Abs(toKill - float.MinValue) > 1f)
        {
            // 正常
            cubes[i].Remove(toKill);
        }
        
        if (color != Color.black) colorChangeItem.GetComponent<MeshRenderer>().material.color = color;
        
    }
    
    private bool HandleScore()
    {
        if (StaticClass.Auto) return false;
        if (PlayerPrefs.GetFloat("drop-high-score-" + _osuSongInfo.BeatmapID) < _score)
        {
            PlayerPrefs.SetFloat("drop-high-score-" + _osuSongInfo.BeatmapID, _score);
            return true;
        }

        return false;
    }
    
    // Update is called once per frame

    private bool isLand = true;
    
    void Update()
    {
        if (Screen.width < Screen.height && isLand)
        {
            // if (!screenDirectionNotifier.activeSelf)
            // {
            //     screenDirectionNotifier.SetActive(true);
            // }
            isLand = false;
            cam.transform.position = new Vector3(-3f, 16.98f, 0);
        }
        else if (Screen.width >= Screen.height && !isLand)
        {
            // if (screenDirectionNotifier.activeSelf)
            // {
            //     screenDirectionNotifier.SetActive(false);
            // }
            isLand = true;
            cam.transform.position = new Vector3(-0.8f, 6.94f, 0);
        }
        
        
        if (Input.touches.Length > 0)
        {
            print("touch!");
            foreach (var touch in Input.touches)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    var raycast = cam.ScreenPointToRay(touch.position);
                    RaycastHit raycastHit;
                    
                    if (Physics.Raycast(raycast, out raycastHit))
                    {
                        var touchedTransform = raycastHit.collider.transform;
                        for (var i = 0; i < lines.Length; i++)
                        {
                            if (lines[i] == touchedTransform)
                            {
                                CheckLine(i);
                                break;
                            }
                        }
                    }
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                CheckLine(0);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                CheckLine(1);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                CheckLine(2);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                CheckLine(3);
            }
        }

        var allEmpty = true;
        // cube 移动

        var termX = terminalLine.position.x;
        var toCheck = new List<int>();
        for (var i = 0; i < cubes.Length; i++)
        {
            var cubesInLine = cubes[i];
            var toRemove = new List<float>();
            foreach (var item in cubesInLine)
            {
                allEmpty = false;
                var pos = item.Value.transform.position;
                item.Value.transform.position = new Vector3(pos.x - Time.deltaTime * speed, pos.y, pos.z);

                if (StaticClass.Auto && pos.x <= termX)
                {
                    if (!toCheck.Contains(i))
                    {
                        toCheck.Add(i);
                    }
                }
                
                if (pos.x < termX - tolerant * speed)  // 算死了
                {
                    item.Value.GetComponent<MeshRenderer>().material.color = Color.red;
                    Destroy(item.Value, 0.1f);
                    _combos = 0;
                    combo.text =  _combos.ToString() + (_combos > 1 ? " Combos" : " Combo");
                    _leftNoteCount -= 1;
                    toRemove.Add(item.Key);
                }
            }
            
            foreach (var line in toCheck)
            {
                CheckLine(line);
            }

            foreach (var t in toRemove)
            {
                cubesInLine.Remove(t);
            }
        }

        if (allEmpty && loaded)
        {
            // 游戏结束
            DoGameOver();
        }

        if (_leftNoteCount < 15)
        {
            MakeSteps();
        }
        
    }
    
    private IEnumerator GoBack()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("Title");
    }

    private void DoGameOver()
    {
        var newRecord = HandleScore();
        if (newRecord)
        {
            text.text = "New Record!!!\n" + score.text;
        }
        else
        {
            text.text = score.text;
        }
        StartCoroutine(GoBack());
    }
}
