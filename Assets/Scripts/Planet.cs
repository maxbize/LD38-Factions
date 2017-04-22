using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour {

    private LayerMask planetMask;

	// Use this for initialization
	void Start () {
        planetMask = LayerMask.NameToLayer("Planet");
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    // TODO: Ignore all non-planet layers
    public Vector3 toSurface(Vector3 source) {
        RaycastHit hit;
        Vector3 toPlanet = (transform.position - source).normalized;
        if (Physics.Raycast(source, toPlanet, out hit)) {
            if (hit.collider.GetComponent<Planet>() != null) {
                return toPlanet * hit.distance;
            }
        }
        return Vector3.zero;
    }
}
