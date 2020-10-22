using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Assets.AbstractWiresEffect.Scripts;
using UnityEngine;
using DG.Tweening;
using UnityEngine.iOS;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;


public class GameController : MonoBehaviour
{

    public GameObject ball;

    public GameObject step;

    public Camera mainCamera;

    public float ballSpeed = 3;

    // public float verticalSpeed = 1;

    // public float tolerant = 0.3f;

    public AudioSource bgmAudio;

    public AudioSource beatEffect;
    
    public AudioSource failAudio;
    
    public RawImage bg;

    public Text countDown;
    public Text score;
    public Text combos;
    public Text hpText;
    private float _score;
    private int _combos = 0;

    public TextAsset defaultScript;

    public bool auto = false;
    
    private float _bpm;
    private float _initialSpace;
    private float _absoluteInitialSpace;
    private List<float> _notes = new List<float>();
    private List<GameObject> _noteSteps;
    private int _nextNoteIndex = -1;
    private float _nextNoteTime;
    private Vector3 _cameraOffset;
    private float _cameraY;
    private float _cameraZ;
    private GameObject _nextStep;
    private int _hp = 10;
    private bool _isGameOver = false;
    private float _initialBallHeight = 0;
    private ParticleSystem _ps;
    private float _lastNodeTime;
    private float _countdownStart = 0;
    private Vector3 _mousePosition = new Vector3();
    private double _scheduledBgmStartTime;
    
    
    // 运动模型
    public float g = -10;        // 重力加速度
    private float _g = -10;
    private Vector3 _speed;       // 初速度向量
    private Vector3 _gravitySpeed;     // 重力向量
    private Vector3 _currentAngle;// 当前角度
    private float _dTime = 0;
    
    // 急停模型
    private float _augUntil = 0f;
    private float _desStart = 0f;
    
    // 文字模型
    private float _charSize = 0;
    private float _charLeftTime = 0;
    private float _charEnlargeSpeed = 0;
    public float feedbackCharSize = 0.21f;
    private TextMesh _currentCharMesh;
    private float _lastNotePos = -1;
    private bool _waterfallPlus = true;
    
    // osu模型
    private SongInfo _osuSongInfo;
    private bool loaded = false;
    private float _initialTime;
    
    // Start is called before the first frame update
    void Start()
    {
        if (auto)
        {
            StaticClass.Auto = true;
        }
        LoadScript();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!loaded)
        {
            return;
        }
        if (_isGameOver)
        {
            return;
        }
        
        if (_nextNoteIndex > 0)
        {
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                if (Input.GetMouseButton(0))
                {
                    if (_mousePosition != new Vector3())
                    {
                        ball.transform.position += new Vector3(0, 0, Time.deltaTime * (_mousePosition - Input.mousePosition).x);  // 阻碍一下
                    }
                    _mousePosition = Input.mousePosition;
                }else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(0))
                {
                    _mousePosition = new Vector3();
                }
            }

            else
            {
                if (Input.touches.Length == 1 && Input.touches[0].phase == TouchPhase.Moved)
                {
                    // 切记：z位置范围为0~3
                    var radius = (_nextStep.transform.localScale.z / 2);
                    var r = (3f + radius * 2) / Math.Min(Screen.width, 1200) * (1f + StaticClass.Sensibility * 2);  // 变化率，默认最大1200（其以上则另算）
                    var max = 3f + radius;
                    var min = -radius;
                    if (_mousePosition != new Vector3())
                    {
                        ball.transform.position += new Vector3(0, 0, - Input.touches[0].deltaPosition.x * r);
                        if (ball.transform.position.z > max)
                        {
                            // 超过最大限度了
                            var old = ball.transform.position;
                            ball.transform.position = new Vector3(old.x, old.y, max);
                        }else if (ball.transform.position.z < min)
                        {
                            // 超过最小限度了
                            var old = ball.transform.position;
                            ball.transform.position = new Vector3(old.x, old.y, min);
                        }
                    }
                    
                    _mousePosition = Input.mousePosition;
                }else if (Input.touches.Length != 1)
                {
                    _mousePosition = new Vector3();
                }
            }

        }
        

        var pos = _cameraOffset + ball.transform.position;
        mainCamera.transform.position = new Vector3(pos.x, _cameraY, _cameraZ);
    }

    private void FixedUpdate()
    {
        if (!loaded)
        {
            return;
        }
        if (_isGameOver)
        {
            return;
        }
        
        // 急停机制
        if (Time.time >= _augUntil && Time.time < _desStart)
        {
            // 横飞
            ball.transform.position += new Vector3(ballSpeed, 0, 0) * Time.fixedDeltaTime;
        }
        else
        {
            // v=gt
            _gravitySpeed.y = _g * (_dTime += Time.fixedDeltaTime);

            //模拟位移
            ball.transform.position += (_speed + _gravitySpeed) * Time.fixedDeltaTime;
            if (ball.transform.position.y < _nextStep.transform.position.y)
            {
                var position = ball.transform.position;
                position = new Vector3(position.x, _nextStep.transform.position.y, position.z);
                ball.transform.position = position;
            }

            // 弧度转度：Mathf.Rad2Deg
            _currentAngle.x = -Mathf.Atan((_speed.y + _gravitySpeed.y) / _speed.z) * Mathf.Rad2Deg;

            // 设置当前角度
            ball.transform.eulerAngles = _currentAngle;
        }

        if (_charLeftTime > 0f)
        {
            _charSize += _charEnlargeSpeed * Time.fixedDeltaTime;
            _currentCharMesh.characterSize = _charSize;
            _charLeftTime -= Time.fixedDeltaTime;
        }
        
    }

    private void DoNext()
    {
        _nextNoteTime = _notes[_nextNoteIndex] - _absoluteInitialSpace + _initialSpace + _initialTime;
        _nextNoteIndex += 1;
        _nextStep = _noteSteps[_nextNoteIndex];
        StartCoroutine(ShowParticles());
        Fly();
        GenerateSteps();
    }
    
    

    private IEnumerator ShowParticles()
    {
        _ps.Play();
        yield return new WaitForSeconds(0.4f);
        _ps.Stop();
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
                bg.GetComponent<AspectRatioFitter>().aspectRatio = (float)StaticClass.imageTex.width / StaticClass.imageTex.height;      
            }
            bgmAudio.clip = await StaticClass.Loader.GetBgm(_osuSongInfo.AudioFilename);
            _bpm = _osuSongInfo.TimingPoints[0].bpm;
            // emptyBgmTime = _osuSongInfo.AudioLeadIn / 1000f;
            // _osuSongInfo.AudioLeadIn;   概念：audio进入时间
            
            // _initialSpace 从播放audio起，第一个音符前的等待时间
            // TODO 球速
            ballSpeed = _osuSongInfo.ApproachRate * 3;
            
            var first = true;
            foreach (var i in _osuSongInfo.Notes)
            {
                // 这是多少拍子？ 60 / bpm
                if (first) {first = false; _initialSpace = i / 1000f; _absoluteInitialSpace = _initialSpace; continue;}
                _notes.Add(i / 1000f);
            }
            // 已经确定谱子准确
        }
        else
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
                ballSpeed = float.Parse(textList[0].Split(' ')[2], ci);
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

        StartCoroutine(WaitUntilInitial(emptyBgmTime));

    }

    private IEnumerator WaitUntilInitial(float emptyBgmTime)
    {
        while (bgmAudio.clip.loadState != AudioDataLoadState.Loaded)
        {
            // print("load audio");
            yield return new WaitForSeconds(0.1f);
        }
        
        // 起步倒计时
        _initialTime = Time.time;  // 开始时间
        // print(_initialTime);
        // print(emptyBgmTime);
        // print(_initialSpace);
        
        // _absoluteInitialSpace = 第一拍子在BGM开始后的时间
        
        if (_initialSpace + emptyBgmTime < 60.0f / _bpm * 3)  // 小于3拍
        {
            // print("种类0");
            // 小于早期节拍   90(2秒） 0.18 + 1 < 2=
            StartCoroutine(WaitUntilUnpause(60.0f / _bpm * 3 - _initialSpace - StaticClass.SmallDelay));
            // bgmAudio.PlayDelayed(60.0f / _bpm * 3 - _initialSpace - StaticClass.SmallDelay);
            _initialSpace = 60.0f / _bpm * 3;
            _countdownStart = 0;
        }
        else
        {
            // print("种类1");
            
            _initialSpace += emptyBgmTime;  // 初始时长 + 1秒
            // _initialSpace = 第一拍子在BGM开始后的时间 + BGM不播放的时间【从_initialTime算起的话，_initialTime + _initialSpace应该就是第一拍的时间】
            
            _countdownStart = _initialSpace - 60.0f / _bpm * 3;
            // 跑拍时间：_initialTime + _countdownStart的时候

            StartCoroutine(WaitUntilUnpause(emptyBgmTime - StaticClass.SmallDelay));
            // bgmAudio.PlayDelayed();  // 从_initialTime + emptyBgmTime时开始播放
        }
        InitializeBall();
    }

    private IEnumerator WaitUntilUnpause(float time)
    {
        yield return new WaitForSeconds(time);
        bgmAudio.time = 0;
        bgmAudio.UnPause();
    }

    private void InitializeBall()
    {
        var pos = ball.transform.position;
        _initialBallHeight = pos.y;
        
        var transformCamera = mainCamera.transform;
        var cameraPosition = transformCamera.position;
        _cameraOffset = cameraPosition - pos;
        _cameraY = cameraPosition.y;
        _cameraZ = cameraPosition.z;
        GenerateSteps();   // 已算过无误
        
        
        _nextStep = _noteSteps[0];  // 第0步
        _nextNoteIndex = 0;
        _nextNoteTime = _initialSpace + _initialTime;   // 目标时间
        _ps = ball.GetComponent<ParticleSystem>();
        loaded = true;
        StartCoroutine(InitialNote());
        // 直接飞
        ball.transform.position = new Vector3(pos.x - ballSpeed * (_initialSpace + _initialTime - Time.time), pos.y,  pos.z);
        Fly();
    }

    private IEnumerator InitialNote()
    {
        yield return new WaitForSeconds(_initialTime + _countdownStart - Time.time);
        // print(Time.time - _initialTime);
        // 3
        countDown.text = "3";
        yield return new WaitForSeconds(_initialTime + _countdownStart + 60f / _bpm  - Time.time);
        // 2
        countDown.text = "2";
        yield return new WaitForSeconds(_initialTime + _countdownStart + 60f / _bpm * 2  - Time.time);
        // 1
        countDown.text = "1";
        yield return new WaitForSeconds(_initialTime + _countdownStart + 60f / _bpm * 3  - Time.time);
        countDown.text = "";
        // print(Time.time - _initialTime);
        // beatEffect.Play();
    }

    private float _generateZPos(bool isWaterfall, bool isSmallNote, int i)
    {
        float zPos;
        if (isWaterfall)
        {
            zPos = _waterfallPlus ? _lastNotePos + 0.35f : _lastNotePos - 0.35f;
            if (zPos > 3)
            {
                zPos -= 0.7f;   // 增大waterfall的间距
                _waterfallPlus = false;
            }
            if (zPos < 0)
            {
                zPos += 0.7f;
                _waterfallPlus = true;
            }
        }else if (isSmallNote)
        {
            if (i % 4 == 0)
            {
                zPos = 0.5f + Random.value * 1f;
            }else if (i % 2 == 0)
            {
                zPos = 1.5f + Random.value * 1f;
            }
            else
            {
                zPos = _lastNotePos + Random.value * 1.2f - 0.6f;   // 夸大幅度
                zPos = zPos > 3f ? 3f : zPos < 0 ? 0 : zPos;
            }
            
        }
        else
        {
            _waterfallPlus = Random.value > 0.5;
            zPos = Random.value < 0.7f ? ((i % 2 == 0 ? 0 : 2f) + Random.value * 1f) : (Random.value + 1f);
        }

        _lastNotePos = zPos;

        return zPos;
    }
    
    private void GenerateSteps()
    {
        // 做步骤
        if (_nextNoteIndex <= 0)
        {
            // 第一次做
            var firstStep = Instantiate(step);  // 0位置，第一次撞击，InitialStep位置
            firstStep.SetActive(true);
            _noteSteps = new List<GameObject> {firstStep};
            _lastNotePos = 0;

            for (var i = 1; i < Math.Min(15, _notes.Count + 1); i++)
            {
                // 开始做第一个板砖（第一个的位置）
                var s = Instantiate(step);
                var last = _noteSteps[i - 1].transform.position;   // 上一个的位置
                var lastNote = i == 1 ? _absoluteInitialSpace : _notes[i - 2];  // 上一个note，因为notes从第1个开始，如果是step 1，是开始空间（比如1.355）；如果是第2个，是notes[0]，比如1.73
                
                var isSmallNote = (_notes[i - 1] - lastNote) / 60 * _bpm < 0.75;
                var isWaterfall = (_notes[i - 1] - lastNote) / 60 * _bpm < 0.4;

                var zPos = _generateZPos(isWaterfall, isSmallNote, i);
                
                s.transform.position = new Vector3(last.x + ballSpeed * (_notes[i - 1] - lastNote), last.y,  zPos);
                s.SetActive(true);
                _noteSteps.Add(s);
            }
        }
        else
        {
            if (_noteSteps.Count > _notes.Count)
            {
                return;
            }
            var s = Instantiate(step);
            var last = _noteSteps[_noteSteps.Count - 1].transform.position;
            var lastNote =  _notes[_noteSteps.Count - 2];
            var isSmallNote = (_notes[_noteSteps.Count - 1] - lastNote)  / 60 * _bpm < 0.75;
            var isWaterfall = (_notes[_noteSteps.Count - 1] - lastNote)  / 60 * _bpm < 0.4;
            var zPos = _generateZPos(isWaterfall, isSmallNote, _noteSteps.Count - 1);
            s.transform.position = new Vector3(last.x + ballSpeed * (_notes[_noteSteps.Count - 1] - lastNote), last.y, zPos);
            s.SetActive(true);
            _noteSteps.Add(s);
        }
    }

    private void Fly()
    {
        var nextPos = _nextStep.transform.position;
        var thisPos = ball.transform.position;
        
        nextPos = StaticClass.Auto ? new Vector3(nextPos.x, _initialBallHeight, nextPos.z) : new Vector3(nextPos.x, _initialBallHeight, thisPos.z);
        
        
        var dur = _nextNoteTime - Time.time;   // 飞这么久


        // 超自然模型（即永远只行为1/8拍，固定结果高度，平飞行，直到前1/8时，再次下降，该模型下G不变）
        var standardDur = 60f / _bpm / 2f;  // 1/2拍子

        if (standardDur * 2 < dur * 1.1f)
        {
            _g = g / standardDur * 1f;
            _speed = new Vector3(ballSpeed, 
                - 0.5f * _g * (standardDur * 2), (nextPos.z - thisPos.z) / (standardDur * 2));
            _gravitySpeed = Vector3.zero;
            _dTime = 0;
            _augUntil = Time.time + standardDur;
            _desStart = _nextNoteTime - standardDur;
        }
        else
        {
            // 重力模型
            _g = _nextNoteIndex > 0 ? g / dur : g / _initialSpace;
            // 通过一个式子计算初速度
            _speed = new Vector3((nextPos.x - thisPos.x) / dur,
                (nextPos.y - thisPos.y) / dur - 0.5f * _g * dur, (nextPos.z - thisPos.z) / dur);
            // 重力初始速度为0
            _gravitySpeed = Vector3.zero;
            _dTime = 0;
            _augUntil = 0;
            _desStart = 0;
        }

        StartCoroutine(DoAutoJump(dur));
        
    }
    
    private IEnumerator DoAutoJump(float dur)
    {
        yield return new WaitForSeconds(dur);
        // 判断位置
        // print(Time.time);
        var blockPos = _nextStep.transform.position;
        var ballPos = ball.transform.position;
        var dis = ballPos.z - blockPos.z;
        var radius = (_nextStep.transform.localScale.z / 2);
        beatEffect.Play();
        if (dis * dis > radius * radius * 2.25f && _nextNoteIndex < _notes.Count)
        {
            // 更不容易死一点但...
            _hp -= 1;


            const string hpItem = "❤";
            var t = "";
            for (var i = 0; i < _hp; i++)
            {
                t += hpItem;
            }

            hpText.text = t;
            if (_hp <= 0)
            {
                _isGameOver = true;
                OnFail();
            }
            else
            {
                // 坠落
                ShowFeedback(_nextStep, 0);
            }
        }
        else
        {
            if (dis * dis > radius * radius * 1f)
            {
                // 偏移
                ShowFeedback(_nextStep, 1);
            }
            else if (dis * dis > radius * radius * 0.49f)
            {
                // 普通
                ShowFeedback(_nextStep, 2);
            }
            else
            {
                // 精准
                ShowFeedback(_nextStep, 3);
            }
        }

        if (_hp >= 1)
        {
            if (_nextNoteIndex >= _notes.Count)
            {
                _isGameOver = true;
                var newRecord = HandleScore();
                if (_combos == _notes.Count)
                {
                    if (newRecord)
                    {
                        countDown.text = "New FC!!!\n" + score.text;
                    }
                    else
                    {
                        countDown.text = "Full Combo!!!\n" + score.text;
                    }
                }
                else
                {
                    if (newRecord)
                    {
                        countDown.text = "New Record!!!\n" + score.text;
                    }
                    else
                    {
                        countDown.text = score.text;
                    }
                }
                
                
                StartCoroutine(GoBack());
            }
            else
            {
                // 可以销毁
                Destroy(_nextStep,2.0f);
                DoNext();
            }   
        }
    }

    private IEnumerator GoBack()
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("Title");
    }

    private void ShowFeedback(GameObject s, int status=3)
    {
        _charSize = 0;
        _currentCharMesh = s.transform.Find("Feedback").GetComponent<TextMesh>();
        _currentCharMesh.characterSize = 0;
        if (status == 3)
        {
            _currentCharMesh.text = "Perfect!";
            _currentCharMesh.color = new Color(0.95f, 0.69f, 0.15f);
            _score += 1000f * (1 + _combos / 100f);
            _noteSteps[_nextNoteIndex].GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(0.03f, 1);
            _combos += 1;
        }
        else if (status == 2)
        {
            _currentCharMesh.text = "Great!";
            _currentCharMesh.color = new Color(0.21f, 0.63f, 0.8f);
            _score += 1000f * (1 + _combos / 100f) * 0.6f;
            _noteSteps[_nextNoteIndex].GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(0.05f, 1);
            _combos += 1;
        }
        else if (status == 1)
        {
            _currentCharMesh.text = "Ok";
            _currentCharMesh.color = new Color(0.6f, 0.8f, 0.5f);
            _score += 1000f * (1 + _combos / 100f) * 0.2f;
            _noteSteps[_nextNoteIndex].GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(0.08f, 1);
            _combos += 1;
        }
        else
        {
            _currentCharMesh.text = "Fail";
            _currentCharMesh.color = new Color(0.6f, 0.8f, 0.5f);
            _noteSteps[_nextNoteIndex].GetComponent<MeshRenderer>().material.color = new Color(191 / 255f, 2 / 255f, 0);
            _combos = 0;
        }

        if (status > 0)
        {
            var pos = _noteSteps[_nextNoteIndex].transform.position;
            _noteSteps[_nextNoteIndex].transform.DOMove(new Vector3(pos.x, pos.y - 0.2f, pos.z), 60f / _bpm / 8);
            StartCoroutine(RecoverPlace(_noteSteps[_nextNoteIndex].transform));

        }
        
        
        score.text = Convert.ToInt32(_score).ToString().PadLeft(7, '0');
        combos.text = _combos.ToString() + (_combos > 1 ? " Combos" : " Combo");

        _charLeftTime = 0.3f;
        _charEnlargeSpeed = feedbackCharSize / _charLeftTime;
    }

    private IEnumerator RecoverPlace(Transform trans)
    {
        yield return new WaitForSeconds(60f / _bpm / 8);
        var pos = trans.position;
        trans.DOMove(new Vector3(pos.x, pos.y + 0.1f, pos.z), 60f / _bpm / 8);
    }
    

    private void OnFail()
    {
        ball.transform.DOKill();
        bgmAudio.Stop();
        failAudio.Play();
        var newRecord = HandleScore();
        if (newRecord)
        {
            countDown.text = "NEW RECORD!!!\n" + score.text;
        }
        else
        {
            countDown.text = score.text;
        }
        var pos = ball.transform.position;
        ball.transform.DOMove(new Vector3(pos.x, -100, pos.z), 2);
        StartCoroutine(GoBack());
    }

    private bool HandleScore()
    {
        if (StaticClass.Auto) return false;
        if (PlayerPrefs.GetFloat("high-score-" + _osuSongInfo.BeatmapID) < _score)
        {
            PlayerPrefs.SetFloat("high-score-" + _osuSongInfo.BeatmapID, _score);
            return true;
        }

        return false;
    }
}
