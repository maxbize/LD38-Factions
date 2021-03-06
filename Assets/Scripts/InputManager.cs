﻿using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Takes care of selecting units and passing out orders
public class InputManager : MonoBehaviour {

    // Set in editor
    public PlayerNum player;
    public RectTransform selector;
    public GameObject markerPrefab;
    public AudioClip commandedClip;
    public AudioClip selectedClip;

    private const int LEFT_CLICK = 0;
    private const int RIGHT_CLICK = 1;

    private HashSet<Pawn> selectedPawns = new HashSet<Pawn>();
    private Planet planet;
    private Vector2 startPoint;
    private LayerMask pawnsAndPlanet;
    private LayerMask planetMask;
    private GameManager gameManager;

	// Use this for initialization
	void Start () {
        gameManager = FindObjectOfType<GameManager>();
        planet = FindObjectOfType<Planet>();
        pawnsAndPlanet = (1 << LayerMask.NameToLayer("Pawn1")) | (1 << LayerMask.NameToLayer("Planet"));
        planetMask = (1 << LayerMask.NameToLayer("Planet"));
    }
	
	// Update is called once per frame
	void Update () {
        if (gameManager.gameState != GameManager.GameState.InGamePlaying) {
            UpdateSelector(Vector2.zero, Vector2.zero);
            return;
        }

        if (selectedPawns.All(p => p == null)) {
            selectedPawns.Clear();
        }

        if (Input.GetMouseButtonDown(LEFT_CLICK)) {
            startPoint = Input.mousePosition;
        } else if (!Input.GetMouseButton(LEFT_CLICK) && startPoint != Vector2.zero) {
            SelectUnitsV2(startPoint, Input.mousePosition);
            startPoint = Vector2.zero;
            UpdateSelector(Vector2.zero, Vector2.zero);
        } else if (Input.GetMouseButton(LEFT_CLICK) && startPoint != Vector2.zero) {
            UpdateSelector(startPoint, Input.mousePosition);
        } else if ((Input.GetMouseButtonDown(RIGHT_CLICK) || Input.GetKeyDown(KeyCode.E)) && selectedPawns.Count > 0) {
            Vector3 targetPosition;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, planetMask)) {
                targetPosition = hit.point;
            } else {
                targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * Vector3.Distance(transform.position, planet.transform.position));
                targetPosition += planet.toSurface(targetPosition);
            }
            int numAudio = 0;
            foreach (Pawn pawn in selectedPawns) {
                if (pawn != null) {
                    pawn.SetTargetPos(targetPosition);
                    if (numAudio++ < 3) {
                        pawn.PlayAudio(commandedClip);
                    }
                }
            }
            Vector3 targetToPlanet = planet.transform.position - targetPosition;
            GameObject marker = Instantiate(markerPrefab, targetPosition - targetToPlanet * 0.05f, Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.one, targetToPlanet), targetToPlanet));
            marker.GetComponent<Renderer>().sharedMaterial = gameManager.GetPlayerSharedMat(PlayerNum.One);
        }
	}

    private void SelectUnitsV2(Vector2 cornerOne, Vector2 cornerTwo) {
        Pawn[] allPawns = FindObjectsOfType<Pawn>(); // TODO: Maintain this list somewhere

        Vector2 topLeft = new Vector2(Mathf.Min(cornerOne.x, cornerTwo.x), Mathf.Max(cornerOne.y, cornerTwo.y));
        Vector2 botRight = new Vector2(Mathf.Max(cornerOne.x, cornerTwo.x), Mathf.Min(cornerOne.y, cornerTwo.y));

        HashSet<Pawn> newPawns = new HashSet<Pawn>();
        int numAudio = 0;
        foreach (Pawn pawn in allPawns) {
            if (pawn.owner != player) {
                continue;
            }
            Vector2 pawnScreenPos = Camera.main.WorldToScreenPoint(pawn.transform.position);
            if (pawnScreenPos.x > topLeft.x &&
                pawnScreenPos.x < botRight.x &&
                pawnScreenPos.y > botRight.y &&
                pawnScreenPos.y < topLeft.y) {

                RaycastHit hit;
                if (Physics.Raycast(transform.position, pawn.transform.position - transform.position, out hit, Mathf.Infinity, pawnsAndPlanet)) {
                    if (hit.collider.GetComponent<Pawn>() != null) {
                        newPawns.Add(pawn);
                            pawn.SetMaterial(gameManager.GetHighlightedMaterial());
                        if (numAudio++ < 3) {
                            pawn.PlayAudio(selectedClip);
                        }
                    }
                }
            }
        }

        foreach (Pawn pawn in selectedPawns) {
            if (pawn != null && !newPawns.Contains(pawn)) {
                pawn.SetMaterial(gameManager.GetPlayerSharedMat(pawn.owner));
            }
        }
        selectedPawns = newPawns;
    }

    private void UpdateSelector(Vector2 cornerOne, Vector2 cornerTwo) {
        Vector2 bottomLeft = new Vector2(Mathf.Min(cornerOne.x, cornerTwo.x), Mathf.Min(cornerOne.y, cornerTwo.y));
        Vector2 topRight = new Vector2(Mathf.Max(cornerOne.x, cornerTwo.x), Mathf.Max(cornerOne.y, cornerTwo.y));
        selector.anchoredPosition = bottomLeft;
        selector.sizeDelta = topRight - bottomLeft;
    }
}
