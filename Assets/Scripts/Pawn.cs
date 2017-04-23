using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour {

    // Set in editor
    public float height; // Float distance above planet
    public float moveSpeed;
    public float attackSpeed;
    public float attackRange; // Range we need to be at to physically attack
    public float trackingRange; // Range at which we'll autoattack any enemies
    public int startingHealth;
    public int damage;
    public GameObject bloodPS;

    public PlayerNum owner { get; private set; }
    private Planet planet;
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Pawn targetOpponent;
    private float attackTimer;
    public int healthRemaining { get; private set; }

	// Use this for initialization
	void Start () {
        planet = FindObjectOfType<Planet>(); // TODO: remove FindObjectOfType
        rb = GetComponent<Rigidbody>();
        healthRemaining = startingHealth;
    }

	// Update is called once per frame
	void Update () {
        HandleAttacking();
        SnapToPlanet();

        Debug.DrawRay(transform.position + transform.up * 1.2f + transform.right, -transform.right * 2 * ((float)healthRemaining / startingHealth), PlayerMethods.GetPlayerColor(owner));
	}

    public void Init(PlayerNum owner) {
        this.owner = owner;
        SetColor(PlayerMethods.GetPlayerColor(owner));
    }

    public void SetTargetPos(Vector3 target) {
        targetPosition = target;
    }

    public void TakeDamage(int amount) {
        healthRemaining -= amount;
        ParticleSystem blood = Instantiate(bloodPS, transform.position, transform.rotation).GetComponent<ParticleSystem>();
        blood.startColor = PlayerMethods.GetPlayerColor(owner);
        if (healthRemaining < 0) {
            for (int i = 0; i < 3; i++) {
                blood = Instantiate(bloodPS, transform.position, transform.rotation).GetComponent<ParticleSystem>();
                blood.startColor = PlayerMethods.GetPlayerColor(owner);
                blood.gameObject.transform.localScale *= 2;
            }
            Destroy(gameObject);
        }
    }

    private void HandleAttacking() {
        // Pick a target
        targetOpponent = null; // Weakest opponent in tracking range
        Pawn attackOpponent = null; // Weakest opponent in attacking range
        foreach (Collider col in Physics.OverlapSphere(targetPosition, trackingRange)) { // Only seek opponents that are within range of our target position
            Pawn pawn = col.GetComponent<Pawn>();
            if (pawn != null && pawn.owner != owner && (targetOpponent == null || targetOpponent.healthRemaining > pawn.healthRemaining)) {
                targetOpponent = pawn;
                if (Vector3.Distance(targetOpponent.transform.position, transform.position) < attackRange) {
                    attackOpponent = targetOpponent;
                }
            }
        }

        // Do the attack
        attackTimer += Time.deltaTime;
        if (attackTimer > attackSpeed && attackOpponent != null) {
            attackOpponent.TakeDamage(damage);
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

    public void SetColor(Color color) {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) {
            renderer.material.color = color;
        }
    }
}
