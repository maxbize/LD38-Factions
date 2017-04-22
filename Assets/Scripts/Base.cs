using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bases can be captured and spawn units for their owner
public class Base : MonoBehaviour {

    // Set in editor
    public float height; // Above planet surface
    public float captureRange;
    public float captureTime;
    public float spawnTime;
    public GameObject pawnPrefab;
    public PlayerNum owningPlayer;

    private Planet planet;
    public PlayerNum capturingPlayer { get; private set; }
    private float capturingTime;
    private float spawningTime;
    public Dictionary<PlayerNum, HashSet<Pawn>> pawnsInRange { get; private set; }

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();
        SnapToPlanet();
        GetComponent<Renderer>().material.color = PlayerMethods.GetPlayerColor(owningPlayer);
        pawnsInRange = new Dictionary<PlayerNum, HashSet<Pawn>>();
    }
	
	// Update is called once per frame
	void Update () {
        HandleCapturing();
        HandleSpawning();
    }

    private void HandleCapturing() {
        Collider[] allPawns = Physics.OverlapSphere(transform.position, captureRange);

        // Partition the pawns by owner
        pawnsInRange.Clear();
        foreach (Collider coll in allPawns) {
            Pawn pawn = coll.GetComponent<Pawn>();
            if (pawn == null) {
                continue;
            }
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
                Debug.Log(capturingPlayer + " started capturing point " + gameObject);
            } else if (capturingPlayer != PlayerNum.Null) { // Capturing player is losing their progress
                capturingTime -= Time.deltaTime;
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
            Debug.Log("Base " + gameObject + " stolen from " + owningPlayer + " by " + capturingPlayer);
            owningPlayer = capturingPlayer;
            capturingPlayer = PlayerNum.Null;
            GetComponent<Renderer>().material.color = PlayerMethods.GetPlayerColor(owningPlayer);
            capturingTime = 0;
            spawningTime = 0;
        }

        Debug.DrawRay(transform.position - transform.forward * height, -transform.forward * (capturingTime / captureTime) * 3, PlayerMethods.GetPlayerColor(capturingPlayer));
        Debug.DrawRay(transform.position - transform.forward * height + transform.up * 0.1f, -transform.forward * 3, Color.black);
    }

    private void HandleSpawning() {
        if (owningPlayer != PlayerNum.Null) {
            spawningTime += Time.deltaTime;
            if (spawningTime > spawnTime) {
                Pawn pawn = Instantiate(pawnPrefab, transform.position, Quaternion.identity).GetComponent<Pawn>();
                pawn.Init(owningPlayer);
                pawn.SetTargetPos(transform.position);
                spawningTime = 0;
            }
        }
    }

    // Mostly copied from Pawn
    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toSurface, transform.up);
    }
}
