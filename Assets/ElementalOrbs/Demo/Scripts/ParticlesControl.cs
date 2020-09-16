using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticlesControl : MonoBehaviour {

    [SerializeField] private List<ParticleSystem> particles;

    private float _timer;
    private float _maxTimer = 3.5f;

    void Start ()
    {
        _timer = _maxTimer;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update () {
        if (_timer >= 0)
        {
            _timer -= Time.deltaTime;
        }
		if(Input.GetKeyDown(KeyCode.Space) && _timer <= 0)
        {
            foreach (var item in particles)
            {
                item.Play(true);
            }
            _timer = _maxTimer;
        }
	}
}
