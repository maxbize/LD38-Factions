using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Simulates the effect of planet gravity on an object
public class PlanetGravity : MonoBehaviour {

    public float gravityForce;

    private Rigidbody rb;
    private Planet planet;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();
        rb = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 gravityDir = (planet.transform.position - transform.position).normalized;
        rb.AddForce(gravityDir * gravityForce * Time.deltaTime);
	}
}
