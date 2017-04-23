using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandMarker : MonoBehaviour {

    // Set in editor
    public float decayTime;

    private float timeAlive = 0;

	// Use this for initialization
	void Start () {
        Update();
    }
	
	// Update is called once per frame
	void Update () {
        timeAlive += Time.deltaTime;
        Vector3 scale = Vector3.one * (1f - timeAlive / decayTime) * 2;
        scale.y = 0.1f;
        transform.localScale = scale;
        if (timeAlive > decayTime) {
            Destroy(gameObject);
        }
	}
}
