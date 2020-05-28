using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

    // Set in editor
    public float height;
    public float maxSpeed;
    public float accel;

    private Planet planet;
    private Vector2 dir;

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
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            horizontal = horizontal != 0 ? horizontal : -dir.x * 2.5f;
            vertical = vertical != 0 ? vertical : -dir.y * 2.5f;
            dir.x = Mathf.Clamp(dir.x + horizontal * accel * Time.unscaledDeltaTime , -1, 1);
            dir.y = Mathf.Clamp(dir.y + vertical * accel * Time.unscaledDeltaTime, -1, 1);
            Vector3 delta = transform.right * dir.x + transform.up * dir.y;
            transform.position += delta * maxSpeed * Time.unscaledDeltaTime;
        } else {
            transform.position += transform.right * 3f * Time.unscaledDeltaTime;
        }
    }

    public void SnapToPlanet() {
        Vector3 toPlanet = planet.transform.position - transform.position;
        transform.position += toPlanet.normalized * (toPlanet.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toPlanet, transform.up);
    }
}
