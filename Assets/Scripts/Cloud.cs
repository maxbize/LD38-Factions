using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour {

    public float height;
    public float speed;

    private Planet planet;
    private Vector3 dir;
    private float startingX;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();
        dir = transform.right;
        startingX = transform.position.x;
        SnapToPlanet();
    }
	
	// Update is called once per frame
	void Update () {
        transform.position += transform.right * speed * Time.deltaTime;
        SnapToPlanet();
	}

    private void SnapToPlanet() {
        Vector3 toPlanet = planet.transform.position - transform.position;
        transform.position = -toPlanet.normalized * height;
        transform.position = new Vector3(startingX, transform.position.y, transform.position.z);
        transform.rotation = Quaternion.LookRotation(-toPlanet, dir);
    }
}
