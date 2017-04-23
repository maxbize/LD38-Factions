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
        if (Physics.Raycast(source, toPlanet, out hit, Mathf.Infinity, planetMask)) {
            if (hit.collider.GetComponent<Planet>() != null) {
                return toPlanet * hit.distance;
            } else {
                Debug.LogWarning("Hit something other than Planet");
                Debug.DrawRay(source, toPlanet * 5, Color.magenta, 10);
            }
        } else {
            Vector3 newSource = source - toPlanet * 10; // Let's try again after pushing out the source since we're inside the planet
            if (Physics.Raycast(newSource, toPlanet, out hit, Mathf.Infinity, planetMask)) {
                if (hit.collider.GetComponent<Planet>() != null) {
                    return hit.point - source;
                } else {
                    Debug.LogWarning("Hit something other than Planet again");
                    Debug.DrawRay(newSource, toPlanet * 5, Color.magenta, 10);
                }
            } else {
                Debug.LogWarning("Still no hits");
                Debug.DrawRay(newSource, toPlanet * 5, Color.cyan, 10);
            }
            Debug.LogWarning("No hits");
            Debug.DrawRay(source, toPlanet * 5, Color.cyan, 10);
        }
        return Vector3.zero;
    }
}
