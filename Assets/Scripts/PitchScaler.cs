using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PitchScaler : MonoBehaviour
{
    // Set in editor
    public float minPitch;
    public float maxPitch;

    private AudioSource source;
    private float pitch;

    // Start is called before the first frame update
    void Start() {
        pitch = Random.Range(minPitch, maxPitch);
        source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update() {
        source.pitch = pitch * Mathf.Lerp(0.25f, 1, Time.timeScale);
    }
}
