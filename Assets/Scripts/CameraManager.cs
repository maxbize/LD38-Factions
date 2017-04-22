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
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");
        Vector3 delta = transform.up * vertical + transform.right * horizontal;
        transform.position += delta * speed * Time.deltaTime;
    }

    // Mostly copied from Pawn
    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toSurface, transform.up);
    }
}
