using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Star : MonoBehaviour
{
    public float speedX = -1;
    public float speedY = -1;
    public float speedZ = 0;
    public float lowestHeight = 0;
    public float alphaDecreaseSpeed = 0.1f;

    private bool triggered = false;
    private float size = -1;
    private TrailRenderer trailRenderer;
    private Gradient colorGradient;
    private GradientColorKey[] colorKeys;
    private GradientAlphaKey[] alphaKeys;

    void Awake()
    {
        GetComponent<Rigidbody>().velocity = new Vector3(speedX, speedY, speedZ);
        trailRenderer = GetComponentInChildren<TrailRenderer>();
        colorGradient = trailRenderer.colorGradient;
        colorKeys = colorGradient.colorKeys;
        alphaKeys = colorGradient.alphaKeys;
    }

    private void Update()
    {
        if(triggered)
        {
            return;
        }

        if(transform.position.y < lowestHeight)
        {
            StartCoroutine(GradientAlpha());
            triggered = true;
        }
    }

    IEnumerator GradientAlpha()
    {
        while (alphaKeys[0].alpha >= 0)
        {
            alphaKeys = trailRenderer.colorGradient.alphaKeys;
            alphaKeys = new GradientAlphaKey[] {
                new GradientAlphaKey(alphaKeys[0].alpha - alphaDecreaseSpeed, 0),
                new GradientAlphaKey(alphaKeys[1].alpha - alphaDecreaseSpeed, 1)};

            colorGradient.SetKeys(colorKeys, alphaKeys);
            trailRenderer.colorGradient = colorGradient;

            yield return new WaitForSeconds(0.1f);
        }

        Destroy(gameObject);
    }
}