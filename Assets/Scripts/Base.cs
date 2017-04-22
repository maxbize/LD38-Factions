using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bases can be captured and spawn units for their owner
public class Base : MonoBehaviour {

    // Set in editor
    public float height; // Above planet surface
    public float captureRange;
    public float captureTime;

    private Planet planet;
    private PlayerNum owningPlayer;
    private PlayerNum capturingPlayer;
    private float capturingTime;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();
        SnapToPlanet();
    }
	
	// Update is called once per frame
	void Update () {
        HandleCapturing();
        
    }

    private void HandleCapturing() {
        Pawn[] allPawns = FindObjectsOfType<Pawn>(); // TODO: Maintain this somewhere

        // Partition the pawns by owner
        Dictionary<PlayerNum, HashSet<Pawn>> pawnsInRange = new Dictionary<PlayerNum, HashSet<Pawn>>();
        foreach (Pawn pawn in allPawns) {
            if (Vector3.Distance(pawn.transform.position, transform.position) > captureRange) {
                continue;
            }
            if (!pawnsInRange.ContainsKey(pawn.owner)) {
                pawnsInRange.Add(pawn.owner, new HashSet<Pawn>());
            }
            pawnsInRange[pawn.owner].Add(pawn);
        }

        // If there's only one player in range they should be capturing the base.
        // Bases are captured by standing next to them for a period of time. If someone else has been attempting
        // capture, you have to let their capture time expire first
        if (pawnsInRange.Count == 0) { // No one at the point - capture time eroding
            capturingTime -= Time.deltaTime;
        } else if (pawnsInRange.Count == 1) {
            var enumerator = pawnsInRange.Keys.GetEnumerator();
            enumerator.MoveNext();
            PlayerNum onlyPlayerInRange = enumerator.Current;
            if (onlyPlayerInRange == capturingPlayer) { // Player is continuing to capture the point
                capturingTime += Time.deltaTime;
            } else if (onlyPlayerInRange != owningPlayer && capturingTime <= 0) { // Player is beginning to capture the point
                capturingTime = Time.deltaTime;
                capturingPlayer = onlyPlayerInRange;
                Debug.Log(capturingPlayer + " started capturing the point");
            }
        } else { // Multiple players at the point - make sure the capturing player is one of them or they'll lose their capture progress
            if (!pawnsInRange.ContainsKey(capturingPlayer)) {
                capturingTime -= Time.deltaTime;
            }
        }

        if (capturingTime < 0) {
            capturingTime = 0;
            capturingPlayer = PlayerNum.Null;
        } else if (capturingTime > captureTime) {
            Debug.Log("Base stolen from " + owningPlayer + " by " + capturingPlayer);
            owningPlayer = capturingPlayer;
            capturingPlayer = PlayerNum.Null;
            capturingTime = 0;
        }
    }

    // Mostly copied from Pawn
    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toSurface, transform.up);
    }
}
