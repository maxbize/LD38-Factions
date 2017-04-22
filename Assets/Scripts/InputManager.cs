using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Takes care of selecting units and passing out orders
public class InputManager : MonoBehaviour {

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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                foreach (Pawn pawn in selectedPawns) {
                    pawn.SetTargetPos(hit.point);
                    pawn.GetComponent<Renderer>().material.color = Color.red;
                }
                selectedPawns.Clear();
                startPoint = Vector2.zero;
            }
        }
	}

    // Could be cleaned up by using a boxcast instead of converting everything to screen space and then filtering out unwanted results
    private void SelectUnits(Vector2 cornerOne, Vector2 cornerTwo) {
        Pawn[] allPawns = FindObjectsOfType<Pawn>(); // TODO: Maintain this list somewhere

        Vector2 topLeft = new Vector2(Mathf.Min(cornerOne.x, cornerTwo.x), Mathf.Max(cornerOne.y, cornerTwo.y));
        Vector2 botRight = new Vector2(Mathf.Max(cornerOne.x, cornerTwo.x), Mathf.Min(cornerOne.y, cornerTwo.y));

        foreach (Pawn pawn in allPawns) {
            Vector2 pawnScreenPos = Camera.main.WorldToScreenPoint(pawn.transform.position);
            if (pawnScreenPos.x > topLeft.x && 
                pawnScreenPos.x < botRight.x && 
                pawnScreenPos.y > botRight.y && 
                pawnScreenPos.y < topLeft.y) {
                
                selectedPawns.Add(pawn);
                pawn.GetComponent<Renderer>().material.color = Color.magenta; // Temp hack? :)
                // TODO: Filter out enemy pawns, pawns on other side of planet
                
            }
        }
    }


}
