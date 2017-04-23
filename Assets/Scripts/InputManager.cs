using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Takes care of selecting units and passing out orders
public class InputManager : MonoBehaviour {

    // Set in editor
    public PlayerNum player;

    private const int LEFT_CLICK = 0;

    private HashSet<Pawn> selectedPawns = new HashSet<Pawn>();
    private Planet planet;
    private Vector2 startPoint;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>();	
	}
	
	// Update is called once per frame
	void Update () {
        if (selectedPawns.Count == 0) {
            if (Input.GetMouseButtonDown(LEFT_CLICK)) {
                startPoint = Input.mousePosition;
            } else if (Input.GetMouseButtonUp(LEFT_CLICK) && startPoint != Vector2.zero) {
                SelectUnits(startPoint, Input.mousePosition);
            }
        } else if (Input.GetMouseButtonDown(LEFT_CLICK)) {
            Vector3 targetPosition;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                targetPosition = hit.point;
            } else {
                targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition + Vector3.forward * Vector3.Distance(transform.position, planet.transform.position));
                targetPosition += planet.toSurface(targetPosition);
            }
            foreach (Pawn pawn in selectedPawns) {
                if (pawn != null) {
                    pawn.SetTargetPos(targetPosition);
                    pawn.SetColor(PlayerMethods.GetPlayerColor(pawn.owner));
                }
            }
            selectedPawns.Clear();
            startPoint = Vector2.zero;
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
        
        foreach (Collider col in Physics.OverlapBox(boxCenter, boxExtents, boxRot)) {
            Pawn pawn = col.GetComponent<Pawn>();
            if (pawn == null || pawn.owner != player) {
                continue;
            }
            selectedPawns.Add(pawn);
            pawn.SetColor(Color.magenta); // Temp hack? :)
        }
    }

}
