using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroy : MonoBehaviour {

    // Set in editor
    public float lifetime;

    private const float SCALE_TIME = 0.5f;
    private float scalingTime;
    private float trailRenderStartingWidth;
    private TrailRenderer tr;

	// Use this for initialization
	void Start () {
        tr = GetComponentInChildren<TrailRenderer>();
        if (tr != null) {
            trailRenderStartingWidth = tr.widthMultiplier;
        }
        Invoke("StartScaling", lifetime);
	}
	
	// Update is called once per frame
	void Update () {
        if (scalingTime > 0) {
            scalingTime += Time.deltaTime;
            if (scalingTime > SCALE_TIME) {
                Destroy(gameObject);
            }

            transform.localScale = Vector3.one * (1 - scalingTime / SCALE_TIME);
            if (tr != null) {
                tr.widthMultiplier = 1 - scalingTime / SCALE_TIME;
            }
        }
	}

    private void StartScaling() {
        scalingTime += Time.deltaTime;
    }
}
