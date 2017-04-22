using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour {

    // Set in editor
    public float height; // Float distance above planet
    public float speed;

    private Planet planet;
    private Rigidbody rb;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>(); // TODO: remove FindObjectOfType
        rb = GetComponent<Rigidbody>();
    }
	
	// Update is called once per frame
	void Update () {
        SnapToPlanet();
	}

    private void SnapToPlanet() {
        RaycastHit hit;
        Vector3 toPlanet = (planet.transform.position - transform.position).normalized;
        if (Physics.Raycast(transform.position, toPlanet, out hit)) {
            if (hit.collider.GetComponent<Planet>() != null) {
                transform.position += toPlanet * (hit.distance - height);
                Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -toPlanet);
                transform.rotation = Quaternion.LookRotation(newForward, -toPlanet);
                rb.velocity = transform.forward * speed;
            }
        }
    }
}
