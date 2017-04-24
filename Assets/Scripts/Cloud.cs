using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloud : MonoBehaviour {

    public float height;
    public float speed;

    private Planet planet;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();
	}
	
	// Update is called once per frame
	void Update () {
        transform.position += transform.right * speed * Time.deltaTime;
        SnapToPlanet();
	}

    private void SnapToPlanet() {
        Vector3 toPlanet = planet.transform.position - transform.position;
        transform.position += toPlanet.normalized * (toPlanet.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toPlanet, transform.up);
    }
}
