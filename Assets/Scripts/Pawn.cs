using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour {

    // Set in editor
    public float height; // Float distance above planet
    public float speed;

    public PlayerNum owner { get; private set; }
    private Planet planet;
    private Rigidbody rb;
    private Vector3 targetPosition;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>(); // TODO: remove FindObjectOfType
        rb = GetComponent<Rigidbody>();

        Init(PlayerNum.One); // TEMP!!
    }

	// Update is called once per frame
	void Update () {
        SnapToPlanet();
	}

    public void Init(PlayerNum owner) {
        this.owner = owner;
    }

    public void SetTargetPos(Vector3 target) {
        targetPosition = target;
    }

    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - height);
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -toSurface);
        transform.rotation = Quaternion.LookRotation(newForward, -toSurface);
        if (targetPosition != Vector3.zero) {
            rb.velocity = Vector3.ProjectOnPlane(targetPosition - transform.position, -toSurface) * speed;
        }
    }
}
