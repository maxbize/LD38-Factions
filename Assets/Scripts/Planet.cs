using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour {

    private LayerMask planetMask = 1 << 12;

	// Use this for initialization
	void Start () {
        // Can't do this in start since other scripts want to call toSurface in start!
        //planetMask = 1 << LayerMask.NameToLayer("Planet");
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    // TODO: Ignore all non-planet layers
    public Vector3 toSurface(Vector3 source) {
        RaycastHit hit;
        Vector3 toPlanet = (transform.position - source).normalized;
        Vector3 newSource = source - toPlanet * 5; // Just in case we're slightly in the planet
        if (Physics.Raycast(newSource, toPlanet, out hit, Mathf.Infinity, planetMask)) {
            if (hit.collider.GetComponent<Planet>() != null) {
                return hit.point - source;
            }
        }
        return Vector3.zero;
    }
}
