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
        if (!GameManager.playing) {
            selectedPawns.Clear();
            UpdateSelector(Vector2.zero, Vector2.zero);
            return;
        }

        if (selectedPawns.Count == 0) {
            if (Input.GetMouseButtonDown(LEFT_CLICK)) {
                startPoint = Input.mousePosition;
            } else if (Input.GetMouseButtonUp(LEFT_CLICK) && startPoint != Vector2.zero) {
                SelectUnitsV2(startPoint, Input.mousePosition);
                startPoint = Vector2.zero;
                UpdateSelector(Vector2.zero, Vector2.zero);
            } else if (startPoint != Vector2.zero) {
                UpdateSelector(startPoint, Input.mousePosition);
            }
        } else if (Input.GetMouseButtonDown(LEFT_CLICK)) {
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
                    pawn.SetColor(PlayerMethods.GetPlayerColor(pawn.owner), gameManager);
                    if (numAudio++ < 3) {
                        pawn.PlayAudio(commandedClip);
                    }
                }
            }
            selectedPawns.Clear();
            Vector3 targetToPlanet = planet.transform.position - targetPosition;
            Instantiate(markerPrefab, targetPosition - targetToPlanet * 0.05f, Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.one, targetToPlanet), targetToPlanet));
        }
	}


    // Could be cleaned up by using a OverlapBox instead of converting everything to screen space and then filtering out unwanted results
    private void SelectUnits(Vector2 cornerOne, Vector2 cornerTwo) {
        Vector3 toPlanet = planet.transform.position - transform.position;
        Vector3 cornerOneV3 = Camera.main.ScreenToWorldPoint((Vector3)cornerOne + Vector3.forward * toPlanet.magnitude);
        Vector3 cornerTwoV3 = Camera.main.ScreenToWorldPoint((Vector3)cornerTwo + Vector3.forward * toPlanet.magnitude);
        Vector3 cornersCenter = (cornerOneV3 + cornerTwoV3) / 2;
        Vector3 camToCornersCenter = cornersCenter - Camera.main.transform.position;
        Vector3 boxCenter = transform.position + camToCornersCenter / 2;

        Debug.DrawLine(Vector3.zero, cornerOneV3, Color.black, 2);
        Debug.DrawLine(Vector3.zero, cornerTwoV3, Color.black, 2);

        Vector3 cornerToCorner = cornerOneV3 - cornerTwoV3;
        float boxX = Mathf.Abs(Vector3.Dot(cornerToCorner, transform.right));
        float boxY = Mathf.Abs(Vector3.Dot(cornerToCorner, transform.up));

        Vector3 boxExtents = new Vector3(boxX, boxY, toPlanet.magnitude) / 2;
        Quaternion boxRot = Quaternion.LookRotation(toPlanet, Camera.main.transform.up);

        int numAudio = 0;
        foreach (Collider col in Physics.OverlapBox(boxCenter, boxExtents, boxRot)) {
            Pawn pawn = col.GetComponent<Pawn>();
            if (pawn == null || pawn.owner != player) {
                continue;
            }
            selectedPawns.Add(pawn);
            pawn.SetColor(Color.magenta, gameManager); // Temp hack? :)
            if (numAudio++ < 3) {
                pawn.PlayAudio(selectedClip);
            }
        }
    }

    private void SelectUnitsV2(Vector2 cornerOne, Vector2 cornerTwo) {
        Pawn[] allPawns = FindObjectsOfType<Pawn>(); // TODO: Maintain this list somewhere

        Vector2 topLeft = new Vector2(Mathf.Min(cornerOne.x, cornerTwo.x), Mathf.Max(cornerOne.y, cornerTwo.y));
        Vector2 botRight = new Vector2(Mathf.Max(cornerOne.x, cornerTwo.x), Mathf.Min(cornerOne.y, cornerTwo.y));

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
                        selectedPawns.Add(pawn);
                        pawn.SetColor(Color.magenta, gameManager); // Temp hack? :)
                        if (numAudio++ < 3) {
                            pawn.PlayAudio(selectedClip);
                        }
                    }
                }
            }
        }
    }

    private void UpdateSelector(Vector2 cornerOne, Vector2 cornerTwo) {
        Vector2 bottomLeft = new Vector2(Mathf.Min(cornerOne.x, cornerTwo.x), Mathf.Min(cornerOne.y, cornerTwo.y));
        Vector2 topRight = new Vector2(Mathf.Max(cornerOne.x, cornerTwo.x), Mathf.Max(cornerOne.y, cornerTwo.y));
        selector.anchoredPosition = bottomLeft;
        selector.sizeDelta = topRight - bottomLeft;
    }
}
