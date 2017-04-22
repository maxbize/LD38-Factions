using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour {

    // Set in editor
    public float height; // Float distance above planet
    public float moveSpeed;
    public float attackSpeed;
    public float attackRange;
    public int health;
    public int damage;

    public PlayerNum owner ;//{ get; private set; }
    private Planet planet;
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Pawn targetOpponent;
    private float attackTimer;

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>(); // TODO: remove FindObjectOfType
        rb = GetComponent<Rigidbody>();

        Init(PlayerNum.One); // TEMP!!
    }

	// Update is called once per frame
	void Update () {
        HandleAttacking();
        SnapToPlanet();
	}

    public void Init(PlayerNum owner) {
        this.owner = owner;
    }

    public void SetTargetPos(Vector3 target) {
        targetPosition = target;
    }

    public void TakeDamage(int amount) {
        health -= amount;
        if (health < 0) {
            Destroy(gameObject);
        }
    }

    private void HandleAttacking() {
        // Pick a target
        targetOpponent = null;
        foreach (Collider col in Physics.OverlapSphere(transform.position, attackRange)) {
            Pawn pawn = col.GetComponent<Pawn>();
            if (pawn != null && pawn.owner != owner) {
                targetOpponent = pawn;
                break;
            }
        }

        // Do the attack
        attackTimer += Time.deltaTime;
        if (attackTimer > attackSpeed && targetOpponent != null) {
            targetOpponent.TakeDamage(damage);
            attackTimer = 0;
        }
    }

    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - height);
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -toSurface);
        transform.rotation = Quaternion.LookRotation(newForward, -toSurface);
        if (targetOpponent != null) {
            rb.velocity = Vector3.ProjectOnPlane(targetOpponent.transform.position - transform.position, -toSurface) * moveSpeed;
        } else if (targetPosition != Vector3.zero) {
            rb.velocity = Vector3.ProjectOnPlane(targetPosition - transform.position, -toSurface) * moveSpeed;
        }
    }
}
