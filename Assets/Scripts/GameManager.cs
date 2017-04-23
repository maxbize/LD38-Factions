using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    // Set in editor
    public Material playerMat;

    private Dictionary<Color, Material> sharedMats = new Dictionary<Color, Material>();

	// Use this for initialization
	void Start () {


	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Material GetPlayerSharedMat(Color color) {
        if (!sharedMats.ContainsKey(color)) {
            Material sharedMat = new Material(playerMat);
            sharedMat.color = color;
            sharedMats[color] = sharedMat;
        }
        return sharedMats[color];
    }
}
