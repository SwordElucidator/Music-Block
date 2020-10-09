using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    private int _combos = 0;  // 连续打击
    private float _score = 0;  // 分数
    

    private Dictionary<float, GameObject>[] cubes;
    // Start is called before the first frame update

    void CreateSingleCube(int line, float time)
    {
        var cube = Instantiate(singleCube);
        cube.transform.position = new Vector3(terminalLine.position.x + (time - Time.time) * speed, singleCube.transform.position.y, lines[line].transform.position.z);
        cubes[line][time] = cube;
    }
    
    void Start()
    {
        text.text = "";
        cubes = new Dictionary<float, GameObject>[lines.Length];
        for (var i = 0; i < cubes.Length; i++)
        {
            cubes[i] = new Dictionary<float, GameObject>();
        }
        
        CreateSingleCube(0, Time.time + 2);
        CreateSingleCube(1, Time.time + 2);
        CreateSingleCube(1, Time.time + 3);
        CreateSingleCube(2, Time.time + 4);
        CreateSingleCube(3, Time.time + 5);
    }


    private void CheckLine(int i)
    {
        // 这里要计入一个"容忍时间"，即容忍的范畴里寻找最小的那个
        var exactClickTime = Time.time;
        var minTolerateTime = Time.time - tolerant;
        var maxTolerateTime = Time.time + tolerant;
        var maxDeathTime = Time.time + tolerant * 2;
        var toKill = float.MinValue;
        foreach (var time in cubes[i].Keys.OrderBy(f => f))
        {
            if (time < minTolerateTime) continue;
            if (time >= maxDeathTime) break;
            
            // 否则结算
            Destroy(cubes[i][time]);
            toKill = time;
            
            if (time >= maxTolerateTime)
            {
                // 算没打上  TODO
                _combos = 0;
                combo.text =  _combos.ToString() + (_combos > 1 ? " Combos" : " Combo");
            }
            else
            {
                print("GOT!!!");
                _combos += 1;
                combo.text =  _combos.ToString() + (_combos > 1 ? " Combos" : " Combo");
                // 结算精度
                if (Math.Abs(exactClickTime - time) < tolerant * 0.4f)
                {
                    // pf
                    _score += 1000f * (1 + _combos / 100f);
                }else if (Math.Abs(exactClickTime - time) < tolerant * 0.7f)
                {
                    _score += 1000f * (1 + _combos / 100f) * 0.6f;
                }
                else
                {
                    _score += 1000f * (1 + _combos / 100f) * 0.2f;
                }
                score.text = Convert.ToInt32(_score).ToString().PadLeft(7, '0');
            }
            break;
        }

        if (Math.Abs(toKill - float.MinValue) > 1f)
        {
            // 正常
            cubes[i].Remove(toKill);
        }
    }
    
    private bool HandleScore()
    {
        if (StaticClass.Auto) return false;
        // if (PlayerPrefs.GetFloat("high-score-" + _osuSongInfo.BeatmapID) < _score)
        // {
        //     PlayerPrefs.SetFloat("high-score-" + _osuSongInfo.BeatmapID, _score);
        //     return true;
        // }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.touches.Length > 0)
        {
            foreach (var touch in Input.touches)
            {
                RaycastHit hit;
                Ray ray = cam.ScreenPointToRay(touch.position);
        
                if (Physics.Raycast(ray, out hit)) {
                    Transform objectHit = hit.transform;
                    for (var i = 0; i < lines.Length; i++)
                    {
                        if (objectHit == lines[i])
                        {
                            CheckLine(i);
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
        foreach (var cubesInLine in cubes)
        {
            var toRemove = new List<float>();
            foreach (var item in cubesInLine)
            {
                allEmpty = false;
                var pos = item.Value.transform.position;
                item.Value.transform.position = new Vector3(pos.x - Time.deltaTime * speed, pos.y, pos.z);
                if (pos.x < -2)
                {
                    Destroy(item.Value);
                    toRemove.Add(item.Key);
                }
            }

            foreach (var t in toRemove)
            {
                cubesInLine.Remove(t);
            }
        }

        if (allEmpty)
        {
            // 游戏结束
            DoGameOver();
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
