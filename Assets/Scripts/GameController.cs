using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    
    public AudioSource failAudio;

    public Text countDown;
    public Text score;
    public Text combos;
    private float _score;
    private int _combos = 0;

    public TextAsset defaultScript;

    public bool auto = false;
    
    private float _bpm;
    private float _initialSpace;
    private List<float> _notes = new List<float>();
    private List<GameObject> _noteSteps;
    private int _nextNoteIndex = -1;
    private float _nextNoteTime;
    private Vector3 _cameraOffset;
    private float _cameraY;
    private float _cameraZ;
    private GameObject _nextStep;
    private bool _isGameOver = false;
    private bool _failed = false;
    private float _initialBallHeight = 0;
    private ParticleSystem _ps;
    private float _lastNodeTime;
    // private float _emptyAudioTime = 0;
    private float _countdownStart = 0;
    private Vector3 _mousePosition = new Vector3();
    private double _scheduledBgmStartTime;
    
    
    // 运动模型
    public float g = -10;        // 重力加速度
    private float _g = -10;
    private Vector3 _speed;       // 初速度向量
    private Vector3 _gravitySpeed;     // 重力向量
    private Vector3 _currentAngle;// 当前角度
    private float _time = 0;
    private float _dTime = 0;
    
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
                    var r = (3f + radius * 2) / Screen.width * (1f + StaticClass.Sensibility * 2);  // 变化率
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
        if (_nextNoteIndex <= 0)
        {
            return;
        }
        // v=gt
        _gravitySpeed.y = _g * (_dTime += Time.fixedDeltaTime);

        //模拟位移
        ball.transform.position += (_speed + _gravitySpeed) * Time.fixedDeltaTime;

        // 弧度转度：Mathf.Rad2Deg
        _currentAngle.x = -Mathf.Atan((_speed.y + _gravitySpeed.y) / _speed.z) * Mathf.Rad2Deg;

        // 设置当前角度
        ball.transform.eulerAngles = _currentAngle;

        if (_charLeftTime > 0f)
        {
            _charSize += _charEnlargeSpeed * Time.fixedDeltaTime;
            _currentCharMesh.characterSize = _charSize;
            _charLeftTime -= Time.fixedDeltaTime;
        }
        
    }

    private void DoNext()
    {
        _nextNoteTime += _notes[_nextNoteIndex] / _bpm * 60.0f;
        _noteSteps[_nextNoteIndex].GetComponent<MeshRenderer>().material.mainTextureScale = new Vector2(0.04f, 1);
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
        const float emptyBgmTime = 1;
        if (StaticClass.IsOsu)
        {
            _osuSongInfo = StaticClass.Loader.DeepRead();
            bgmAudio.clip = await StaticClass.Loader.GetBgm(_osuSongInfo.AudioFilename);
            _bpm = _osuSongInfo.TimingPoints[0].bpm;  // TODO
            // _osuSongInfo.AudioLeadIn;   概念：audio进入时间
            
            // _initialSpace 从播放audio起，第一个音符前的等待时间
            _initialSpace = _osuSongInfo.Notes[0] / 1000f;
            // TODO 球速
            ballSpeed = _osuSongInfo.ApproachRate * 2;
            
            var lastTime = _initialSpace;
            var first = true;
            foreach (var i in _osuSongInfo.Notes)
            {
                // 这是多少拍子？ 60 / bpm
                if (first) {first = false; continue;}
                _notes.Add((i / 1000f - lastTime) / 60 * _bpm);
                lastTime = i / 1000f;
            }
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
            if (textList[0].Split(' ').Length > 2)
            {
                // 控制球速
                ballSpeed = float.Parse(textList[0].Split(' ')[2], ci);
            }
            // 添加步骤
            foreach (var i in textList[1].Split(' '))
            {
                _notes.Add(float.Parse(i, ci));
            }
        }
        // 起步倒计时
        if (_initialSpace + emptyBgmTime < 60.0f / _bpm * 3)
        {
            // 小于早期节拍   90(2秒） 0.18 + 1 < 2
            // _emptyAudioTime = 60.0f / _bpm * 3 - _initialSpace;
            bgmAudio.PlayScheduled(AudioSettings.dspTime + 60.0f / _bpm * 3 - _initialSpace);
            _initialSpace = 60.0f / _bpm * 3;
            _countdownStart = 0;
        }
        else
        {
            // _emptyAudioTime = emptyBgmTime;
            _countdownStart = _initialSpace - 60.0f / _bpm * 3 + emptyBgmTime;
            _initialSpace += emptyBgmTime;
            bgmAudio.PlayScheduled(AudioSettings.dspTime + emptyBgmTime);
        }
        loaded = true;
        InitializeBall();
    }


    private void InitializeBall()
    {
        var pos = ball.transform.position;
        _initialBallHeight = pos.y;
        var transformCamera = mainCamera.transform;
        // ball.GetComponent<TrailRenderer>().time = 0;
        var cameraPosition = transformCamera.position;
        _cameraOffset = cameraPosition - pos;
        _cameraY = cameraPosition.y;
        _cameraZ = cameraPosition.z;
        // pos = new Vector3(pos.x - _initialSpace * ballSpeed, pos.y, pos.z);
        // ball.transform.position = pos;
        // transformCamera.position = _cameraOffset + pos;
        // ball.GetComponent<TrailRenderer>().time = 0.8f;
        GenerateSteps();
        _nextStep = _noteSteps[0];
        _nextNoteIndex = 0;
        _nextNoteTime = _initialSpace + Time.time;
        _ps = ball.GetComponent<ParticleSystem>();
        StartCoroutine(InitialNote());
        
    }

    private IEnumerator InitialNote()
    {
        yield return new WaitForSeconds(_countdownStart);
        // 3
        countDown.text = "3";
        yield return new WaitForSeconds(60f / _bpm);
        // 2
        countDown.text = "2";
        yield return new WaitForSeconds(60f / _bpm);
        // 1
        countDown.text = "1";
        yield return new WaitForSeconds(60f / _bpm);
        countDown.text = "";
        DoNext();
    }

    private float _generateZPos(bool isWaterfall, bool isSmallNote, int i)
    {
        float zPos;
        if (isWaterfall)
        {
            zPos = _waterfallPlus ? _lastNotePos + 0.15f : _lastNotePos - 0.15f;
            if (zPos > 3)
            {
                zPos -= 0.3f;
                _waterfallPlus = false;
            }
            if (zPos < 0)
            {
                zPos += 0.3f;
                _waterfallPlus = true;
            }
        }else if (isSmallNote)
        {
            if (i % 4 == 0)
            {
                zPos = 0.8f + Random.value * 0.7f;
            }else if (i % 2 == 0)
            {
                zPos = 1.5f + Random.value * 0.7f;
            }
            else
            {
                zPos = _lastNotePos + Random.value * 0.4f - 0.2f;
            }
            
        }
        else
        {
            _waterfallPlus = Random.value > 0.5;
            zPos = Random.value < 0.7f ? ((i % 2 == 0 ? 0 : 2f) + Random.value * 1f) : Random.value * 3;
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
            var firstStep = Instantiate(step);
            firstStep.SetActive(true);
            _noteSteps = new List<GameObject> {firstStep};
            _lastNotePos = 0;

            for (var i = 1; i < Math.Min(15, _notes.Count + 1); i++)
            {
                var s = Instantiate(step);
                var last = _noteSteps[i - 1].transform.position;
                var isSmallNote = _notes[i - 1] < 1;
                var isWaterfall = _notes[i - 1] < 0.5;

                var zPos = _generateZPos(isWaterfall, isSmallNote, i);
                s.transform.position = new Vector3(last.x + ballSpeed * _notes[i - 1] / _bpm * 60.0f, last.y,  zPos);
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
            var isSmallNote = _notes[_noteSteps.Count - 1] < 1;
            var isWaterfall = _notes[_noteSteps.Count - 1] < 0.5;
            var zPos = _generateZPos(isWaterfall, isSmallNote, _noteSteps.Count - 1);
            s.transform.position = new Vector3(last.x + ballSpeed * _notes[_noteSteps.Count - 1] / _bpm * 60.0f, last.y, zPos);
            s.SetActive(true);
            _noteSteps.Add(s);
        }
    }

    private void Fly()
    {
        var nextPos = _nextStep.transform.position;
        nextPos = StaticClass.Auto ? new Vector3(nextPos.x, _initialBallHeight, nextPos.z) : new Vector3(nextPos.x, _initialBallHeight, ball.transform.position.z);
        ball.transform.DOKill();
        var dur = _nextNoteIndex <= 0 ? _initialSpace : _nextNoteTime - Time.time;
        
        // ball.transform.DOJump(nextPos, _nextNoteIndex <= 0 ? verticalSpeed * 2 : verticalSpeed, 1, dur);

        if (_nextNoteIndex > 0)
        {
            var thisPos = ball.transform.position;
            _time = dur;
            _g = g / _notes[_nextNoteIndex - 1] * _bpm / 90f;
            // 通过一个式子计算初速度
            _speed = new Vector3((nextPos.x - thisPos.x) / _time,
                (nextPos.y - thisPos.y) / _time - 0.5f * _g * _time, (nextPos.z - thisPos.z) / _time);
            // 重力初始速度为0
            _gravitySpeed = Vector3.zero;
            _dTime = 0;
        }

        // 自动算下一个
        // _lastNodeTime = _nextNoteTime;
        // 走你

        StartCoroutine(DoAutoJump(dur));
        
    }
    
    private IEnumerator DoAutoJump(float dur)
    {
        yield return new WaitForSeconds(dur);
        // 判断位置
        var blockPos = _nextStep.transform.position;
        var ballPos = ball.transform.position;
        var dis = ballPos.z - blockPos.z;
        var radius = (_nextStep.transform.localScale.z / 2);
        if (dis * dis > radius * radius * 1.69f && _nextNoteIndex < _notes.Count)
        {
            // 更不容易死一点但...
            _isGameOver = true;
            _failed = true;
            OnFail();
        }
        else
        {
            if (dis * dis > radius * radius)
            {
                ShowFeedback(_nextStep, 1);
            }
            else if (dis * dis > radius * radius / 4f)
            {
                ShowFeedback(_nextStep, 2);
            }
            else
            {
                ShowFeedback(_nextStep, 3);
            }
            if (_nextNoteIndex >= _notes.Count)
            {
                _isGameOver = true;
                var newRecord = HandleScore();
                if (newRecord)
                {
                    countDown.text = "FC New Record!!!\n" + score.text;
                }
                else
                {
                    countDown.text = "FULL COMBO!!!\n" + score.text;
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
            _score += 1000000f / _notes.Count;
        }
        else if (status == 2)
        {
            _currentCharMesh.text = "Great!";
            _currentCharMesh.color = new Color(0.21f, 0.63f, 0.8f);
            _score += 1000000f / _notes.Count * 0.7f;
        }
        else
        {
            _currentCharMesh.text = "Ok";
            _currentCharMesh.color = new Color(0.6f, 0.8f, 0.5f);
            _score += 1000000f / _notes.Count * 0.4f;
        }
        
        _combos += 1;
        score.text = Convert.ToInt32(_score).ToString().PadLeft(7, '0');
        combos.text = _combos.ToString() + (_combos > 1 ? " Combos" : "Combo");

        _charLeftTime = 0.3f;
        _charEnlargeSpeed = feedbackCharSize / _charLeftTime;
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
        if (PlayerPrefs.GetFloat("high-score-" + StaticClass.Name) < _score)
        {
            PlayerPrefs.SetFloat("high-score-" + StaticClass.Name, _score);
            return true;
        }

        return false;
    }
}
