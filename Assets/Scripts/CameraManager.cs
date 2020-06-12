using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour {

    // Set in editor
    public float height;
    public float maxSpeed;
    public float accel;
    public float mouseAccel;

    [HideInInspector]
    public bool invertDir;

    private const int NUM_DELTAS = 2;

    private GameManager gameManager;
    private Planet planet;
    private Vector2 dir;
    private Vector2 lastMousePos;
    private List<Vector2> lastMouseDeltas;
    private int deltaIndex;

	// Use this for initialization
	void Start () {
        gameManager = FindObjectOfType<GameManager>();
        planet = FindObjectOfType<Planet>();
        lastMouseDeltas = new List<Vector2>(NUM_DELTAS);
        for (int i = 0; i < NUM_DELTAS; i++) {
            lastMouseDeltas.Add(Vector2.zero);
        }
    }
	
	// Update is called once per frame
	void Update () {
        HandleInput();
        SnapToPlanet();
	}

    private void HandleInput() {
        if (Time.timeScale == 0) {
            return;
        } else if (gameManager.gameState == GameManager.GameState.InGamePlaying) {
            if (Input.GetMouseButton(2)) {
                if (Input.GetMouseButtonDown(2)) {
                    dir = Vector2.zero;
                    deltaIndex = 0;
                    for (int i = 0; i < NUM_DELTAS; i++) {
                        lastMouseDeltas[i] = Vector2.zero;
                    }
                }
                Vector2 mousePos = Input.mousePosition;
                Vector2 mouseDelta = (lastMousePos - mousePos) * (invertDir ? -1 : 1);
                Vector3 delta = transform.right * mouseDelta.x * mouseAccel + transform.up * mouseDelta.y * mouseAccel;
                transform.position += delta;

                lastMouseDeltas[deltaIndex] = mouseDelta;
                deltaIndex = (deltaIndex + 1) % NUM_DELTAS;

            } else if (Input.GetMouseButtonUp(2)) {
                Vector2 averageDelta = Vector2.zero;
                for (int i = 0; i < NUM_DELTAS; i++) {
                    averageDelta += lastMouseDeltas[i];
                }
                averageDelta = averageDelta / NUM_DELTAS;
                dir = averageDelta * mouseAccel;
                if (Mathf.Abs(dir.x) > 1) {
                    dir /= Mathf.Abs(dir.x);
                }
                if (Mathf.Abs(dir.y) > 1) {
                    dir /= Mathf.Abs(dir.y);
                }
            } else {
                float horizontal = Input.GetAxisRaw("Horizontal") * (invertDir ? -1 : 1);
                float vertical = Input.GetAxisRaw("Vertical") * (invertDir ? -1 : 1);

                if (horizontal == 0) {
                    float brake = -dir.x * 10f * Time.unscaledDeltaTime;
                    dir.x = Mathf.Abs(brake) > Mathf.Abs(dir.x) ? 0 : dir.x + brake;
                } else {
                    dir.x = Mathf.Clamp(dir.x + horizontal * accel * Time.unscaledDeltaTime, -1, 1);
                }

                if (vertical == 0) {
                    float brake = -dir.y * 10f * Time.unscaledDeltaTime;
                    dir.y = Mathf.Abs(brake) > Mathf.Abs(dir.y) ? 0 : dir.y + brake;
                } else {
                    dir.y = Mathf.Clamp(dir.y + vertical * accel * Time.unscaledDeltaTime, -1, 1);
                }

                Vector3 delta = transform.right * dir.x + transform.up * dir.y;
                transform.position += delta * maxSpeed * Time.unscaledDeltaTime;
            }
            lastMousePos = Input.mousePosition;
        } else {
            transform.position += transform.right * 3f * Time.unscaledDeltaTime;
        }
    }

    public void SnapToPlanet() {
        Vector3 toPlanet = planet.transform.position - transform.position;
        transform.position += toPlanet.normalized * (toPlanet.magnitude - height);
        transform.rotation = Quaternion.LookRotation(toPlanet, transform.up);
    }
}
