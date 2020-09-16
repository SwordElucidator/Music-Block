using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraBehaviour : MonoBehaviour {

    [SerializeField] private float rotateSpeed = 0.5f;
    [SerializeField] private float moveSpeed = 100f;
    public Transform cam;

    private Transform _transform;

	void Start () {
        _transform = transform;
	}
    
	void Update () {
        _transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime * Mathf.Rad2Deg, Space.World);
        _transform.Rotate(Vector3.right, -Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime * Mathf.Rad2Deg, Space.Self);

        if(Input.mouseScrollDelta.y != 0)
        {
            cam.Translate(Vector3.forward * Input.mouseScrollDelta.y * Time.deltaTime * moveSpeed, Space.Self);
        }
    }
}
