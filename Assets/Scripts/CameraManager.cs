using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

    // Set in editor
    public float height;
    public float speed;

    private Planet planet;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();
	}
	
	// Update is called once per frame
	void Update () {
        HandleInput();
        SnapToPlanet();
	}

    private void HandleInput() {
        if (GameManager.playing) {
            float vertical = Input.GetAxis("Vertical");
            float horizontal = Input.GetAxis("Horizontal");
            Vector3 delta = transform.up * vertical + transform.right * horizontal;
            transform.position += delta * speed * Time.deltaTime;
        } else {
            transform.position += transform.right * 3f * Time.deltaTime;
        }
    }

    public void SnapToPlanet() {
        Vector3 toPlanet = planet.transform.position - transform.position;
        transform.position += toPlanet.normalized * (toPlanet.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toPlanet, transform.up);
    }
}
