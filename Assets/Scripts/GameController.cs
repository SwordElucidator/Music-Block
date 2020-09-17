using System;
using System.Collections;
using System.Collections.Generic;
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

    public TextAsset defaultScript;
    
    private float _bpm;
    private float _initialSpace;
    private List<float> _notes;
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
    private float _emptyAudioTime = 0;
    private float _countdownStart = 0;
    private Vector3 _mousePosition = new Vector3();
    
    
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
    
    // Start is called before the first frame update
    void Start()
    {
        LoadScript();
        InitializeBall();
    }

    // Update is called once per frame
    void Update()
    {
        if (_isGameOver)
        {
            return;
        }
        
        if (_nextNoteIndex > 0)
        {
            if (Input.GetMouseButton(0))
            {
                if (_mousePosition != new Vector3())
                {
                    if (SystemInfo.deviceType == DeviceType.Desktop)
                    {
                        ball.transform.position += new Vector3(0, 0, Time.deltaTime * (_mousePosition - Input.mousePosition).x);  // 阻碍一下
                    }
                    else
                    {
                        ball.transform.position += new Vector3(0, 0, Time.deltaTime * (_mousePosition - Input.mousePosition).x / Screen.width * 200f);  // 阻碍一下
                    }
                    
                }
                _mousePosition = Input.mousePosition;
            }else if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonDown(0))
            {
                _mousePosition = new Vector3();
            }
        }
        

        var pos = _cameraOffset + ball.transform.position;
        mainCamera.transform.position = new Vector3(pos.x, _cameraY, _cameraZ);
    }

    private void FixedUpdate()
    {
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

    private void LoadScript()
    {
        var textList = (StaticClass.Script ? StaticClass.Script : defaultScript).text.Split('\n');
        if (StaticClass.Bgm)
        {
            bgmAudio.clip = StaticClass.Bgm;
        }
        _bpm = float.Parse(textList[0].Split(' ')[0]);  // 182节/分钟的话  
        _initialSpace = float.Parse(textList[0].Split(' ')[1]);
        
        // 起步倒计时
        if (_initialSpace < 60.0f / _bpm * 3)
        {
            // 小于早期节拍
            _emptyAudioTime = 60.0f / _bpm * 3 - _initialSpace;
            _initialSpace = 60.0f / _bpm * 3;
            _countdownStart = 0;
        }
        else
        {
            _emptyAudioTime = 0;
            _countdownStart = _initialSpace - 60.0f / _bpm * 3;

        }
        
        _notes = new List<float>();
        foreach (var i in textList[1].Split(' '))
        {
            _notes.Add(float.Parse(i));
        }
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
        ball.GetComponent<TrailRenderer>().time = 0.8f;
        GenerateSteps();
        _nextStep = _noteSteps[0];
        _nextNoteIndex = 0;
        _nextNoteTime = _initialSpace + Time.time;
        _ps = ball.GetComponent<ParticleSystem>();
        StartCoroutine(StartBgm());
        StartCoroutine(InitialNote());
        
    }

    private IEnumerator StartBgm()
    {
        if (_emptyAudioTime > 0)
        {
            yield return new WaitForSeconds(_emptyAudioTime);
        }
        bgmAudio.Play();
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
    
    private void GenerateSteps()
    {
        // 做步骤
        if (_nextNoteIndex <= 0)
        {
            // 第一次做
            var firstStep = Instantiate(step);
            firstStep.SetActive(true);
            _noteSteps = new List<GameObject> {firstStep};
            
            for (var i = 1; i < Math.Min(10, _notes.Count + 1); i++)
            {
                var s = Instantiate(step);
                var last = _noteSteps[i - 1].transform.position;
                var isSmallNote = _notes[i - 1] < 1;
                s.transform.position = new Vector3(last.x + ballSpeed * _notes[i - 1] / _bpm * 60.0f, last.y,  isSmallNote ? (i % 2 == 0 ? 1 : 1.5f) + Random.value * 0.5f : Random.value < 0.7f ? ((i % 2 == 0 ? 0 : 2f) + Random.value * 1f) : Random.value * 3);
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
            s.transform.position = new Vector3(last.x + ballSpeed * _notes[_noteSteps.Count - 1] / _bpm * 60.0f, last.y, isSmallNote ? (_noteSteps.Count % 2 == 1 ? 1 : 1.5f) + Random.value * 0.5f : Random.value < 0.7f ? ((_noteSteps.Count % 2 == 1 ? 0 : 2f) + Random.value * 1f) : Random.value * 3);
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
        if (!ball.GetComponent<WiresConnector>().enabled)
        {
            ball.GetComponent<WiresConnector>().enabled = true;
        }
        
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
        if (dis * dis > radius * radius && _nextNoteIndex < _notes.Count)
        {
            // 死了
            _isGameOver = true;
            _failed = true;
            StartCoroutine(OnFail());
        }
        else
        {
            if (dis * dis > radius * radius / 4f)
            {
                ShowFeedback(_nextStep, false);
            }
            else
            {
                ShowFeedback(_nextStep, true);
            }
            if (_nextNoteIndex >= _notes.Count)
            {
                _isGameOver = true;
                countDown.text = "YOU WIN!";
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

    private void ShowFeedback(GameObject s, bool isPerfect=true)
    {
        _charSize = 0;
        _currentCharMesh = s.transform.Find("Feedback").GetComponent<TextMesh>();
        _currentCharMesh.characterSize = 0;
        if (isPerfect)
        {
            _currentCharMesh.text = "Perfect!";
            _currentCharMesh.color = new Color(0.95f, 0.69f, 0.15f);
        }
        else
        {
            _currentCharMesh.text = "Great!";
            _currentCharMesh.color = new Color(0.21f, 0.63f, 0.8f);
        }

        _charLeftTime = 0.3f;
        _charEnlargeSpeed = feedbackCharSize / _charLeftTime;
    }
    

    private IEnumerator OnFail()
    {
        ball.transform.DOKill();
        bgmAudio.Stop();
        failAudio.Play();
        var pos = ball.transform.position;
        ball.transform.DOMove(new Vector3(pos.x, -100, pos.z), 2);
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene("Music");
    }
}
