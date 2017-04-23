using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

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

    public PlayerNum owner;// { get; private set; }
    private Planet planet;
    private Rigidbody rb;
    private Vector3 targetPosition;
    private Pawn trackingOpponent;
    private Pawn attackingOpponent;
    private float attackTimer;
    public int healthRemaining { get; private set; }
    private LayerMask targetingLayerMask;

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

    public void Init(PlayerNum owner, GameManager gameManager) {
        this.owner = owner;
        SetColor(PlayerMethods.GetPlayerColor(owner), gameManager);
        
        // Ugly hack - could be cleaned up
        if (owner == PlayerNum.One) {
            targetingLayerMask = PlayerMethods.allButP1;
            gameObject.layer = LayerMask.NameToLayer("Pawn1");
        } else if (owner == PlayerNum.Two) {
            targetingLayerMask = PlayerMethods.allButP2;
            gameObject.layer = LayerMask.NameToLayer("Pawn2");
        } else if (owner == PlayerNum.Three) {
            targetingLayerMask = PlayerMethods.allButP3;
            gameObject.layer = LayerMask.NameToLayer("Pawn3");
        } else if (owner == PlayerNum.Four) {
            targetingLayerMask = PlayerMethods.allButP4;
            gameObject.layer = LayerMask.NameToLayer("Pawn4");
        }
    }

    public void SetTargetPos(Vector3 target) {
        targetPosition = target;
    }

    public void TakeDamage(int amount) {
        healthRemaining -= amount;
        ParticleSystem blood = Instantiate(bloodPS, transform.position, Quaternion.LookRotation(transform.up)).GetComponent<ParticleSystem>();
        blood.startColor = PlayerMethods.GetPlayerColor(owner);
        if (healthRemaining < 0) {
            for (int i = 0; i < 2; i++) {
                blood = Instantiate(bloodPS, transform.position, Quaternion.LookRotation(transform.up)).GetComponent<ParticleSystem>();
                blood.startColor = PlayerMethods.GetPlayerColor(owner);
                blood.gameObject.transform.localScale *= 2;
            }
            Destroy(gameObject);
        }
    }

    public void SetColor(Color color, GameManager gameManager) {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) {
            renderer.sharedMaterial = gameManager.GetPlayerSharedMat(color);
        }
    }

    private void HandleAttacking() {
        // Check if our targets are still good
        if (trackingOpponent != null && Vector3.Distance(trackingOpponent.transform.position, targetPosition) > trackingRange) {
            trackingOpponent = null;
        }
        if (attackingOpponent != null && Vector3.Distance(attackingOpponent.transform.position, transform.position) > attackRange) {
            attackingOpponent = null;
        }

        // Find new targets if necessary
        if (attackingOpponent == null) {
            foreach (Collider col in Physics.OverlapSphere(transform.position, attackRange, targetingLayerMask)) { // Only seek opponents that are within range of our target position
                attackingOpponent = col.GetComponent<Pawn>();
                if (Vector3.Distance(attackingOpponent.transform.position, targetPosition) < trackingRange) {
                    trackingOpponent = attackingOpponent;
                }
            }
        }
        if (trackingOpponent == null) {
            foreach (Collider col in Physics.OverlapSphere(targetPosition, trackingRange, targetingLayerMask)) { // Only seek opponents that are within range of our target position
                trackingOpponent = col.GetComponent<Pawn>();
            }
        }
        
        // Do the attack
        attackTimer += Time.deltaTime;
        if (attackTimer > attackSpeed && attackingOpponent != null) {
            attackingOpponent.TakeDamage(damage);
            attackTimer = 0;
        }

    }

    private void SnapToPlanet() {
        Vector3 toSurface = planet.toSurface(transform.position);
        transform.position += toSurface.normalized * (toSurface.magnitude - height);
        Vector3 newForward = Vector3.ProjectOnPlane(transform.forward, -toSurface);
        transform.rotation = Quaternion.LookRotation(newForward, -toSurface);
        if (trackingOpponent != null) {
            rb.velocity = Vector3.ProjectOnPlane(trackingOpponent.transform.position - transform.position, -toSurface) * moveSpeed;
        } else if (targetPosition != Vector3.zero) {
            rb.velocity = Vector3.ProjectOnPlane(targetPosition - transform.position, -toSurface) * moveSpeed;
        }
    }
}
