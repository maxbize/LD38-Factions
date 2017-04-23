using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutodestroyPS : MonoBehaviour {

	// Use this for initialization
	void Start () {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        Invoke("Die", ps.main.startLifetime.constant + ps.main.duration);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    private void Die() {
        Destroy(gameObject);
    }
}
