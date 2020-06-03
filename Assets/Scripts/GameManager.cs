using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    // Set in editor
    public Material playerMat;
    public PlayerNum humanPlayer;
    public GameObject[] levels;
    public static bool playing = false; // Out of time - global state!
    public GameObject startScreenUI;
    public GameObject victoryScreenUI;
    public GameObject defeatScreenUI;
    public GameObject finalVictoryScreenUI;
    public GameObject instructionScreenUI;
    public Button continueGameButton;
    public AudioClip victoryClip;
    public AudioClip defeatClip;
    public GameObject themeSong;
    public float slowDownTime;

    private GameObject currentLevel;
    private int levelIndex;
    private Dictionary<Color, Material> sharedMats = new Dictionary<Color, Material>();
    private Base[] allBases = new Base[0];
    private AudioSource audioSource;
    private LevelText levelText;
    private CameraManager cam;
    private float slowmoStartMarker;
    private float elapsedTime;
    private float levelStartTime;
    private bool kongApiInitialized;

	// Use this for initialization
	void Start() {
        /* Commenting this out while I'm not building levels
        // Make sure we didn't leave a level up
        foreach (Base bas in FindObjectsOfType<Base>()) {
            GameObject level = bas.transform.parent.gameObject;
            for (int i = 0; i < levels.Length; i++) {
                if (levels[i] == level) {
                    levelIndex = i;
                    level.SetActive(false);
                    break;
                }
            }
        }
        */

        int savedIndex = PlayerPrefs.GetInt("level");
        if (savedIndex == 0) {
            continueGameButton.interactable = false;
            continueGameButton.GetComponentInChildren<Text>().color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        }

        audioSource = GetComponent<AudioSource>();
        levelText = FindObjectOfType<LevelText>();
        cam = FindObjectOfType<CameraManager>();

        startScreenUI.SetActive(true);
        instructionScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);

        RegisterAPI();
    }

    // Update is called once per frame
	void Update () {
        if (playing) {
            HandleTimeScale();
            CheckVictory();
            CheckDefeat();
            if (Input.GetKeyDown(KeyCode.R)) {
                if (elapsedTime != -1) {
                    elapsedTime += Time.realtimeSinceStartup - levelStartTime;
                    PlayerPrefs.SetInt("elapsed", Mathf.CeilToInt(elapsedTime));
                }

                RestartLevel();
            }
        } else if (Input.GetKeyDown(KeyCode.R) && defeatScreenUI.activeSelf) {
            RestartLevel();
        }
    }

    private void HandleTimeScale() {
        if (Input.GetKey(KeyCode.Space)) {
            if (Input.GetKeyDown(KeyCode.Space)) {
                float t0 = (1f - Time.timeScale) / 0.9f;
                slowmoStartMarker = Time.realtimeSinceStartup - t0 * slowDownTime;
            }
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - slowmoStartMarker) / slowDownTime);
            Time.timeScale = 1f - 0.9f * t;
            Time.fixedDeltaTime = 0.02f - 0.018f * t;
        } else {
            if (Input.GetKeyUp(KeyCode.Space)) {
                float t0 = (Time.timeScale - 0.1f) / 0.9f;
                slowmoStartMarker = Time.realtimeSinceStartup - t0 * slowDownTime;
            }
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - slowmoStartMarker) / slowDownTime);
            Time.timeScale = 0.1f + 0.9f * t;
            Time.fixedDeltaTime = 0.002f + 0.018f * t;
        }
    }

    private void CheckVictory() {
        foreach (Base bas in allBases) {
            if (bas.owningPlayer != humanPlayer) {
                return;
            }
        }

        levelIndex++;
        PlayerPrefs.SetInt("level", levelIndex);
        if (PlayerPrefs.GetInt("maxLevel") < levelIndex) {
            PlayerPrefs.SetInt("maxLevel", levelIndex);
        }

        themeSong.SetActive(true);
        if (elapsedTime != -1) {
            elapsedTime += Time.realtimeSinceStartup - levelStartTime;
            PlayerPrefs.SetInt("elapsed", Mathf.CeilToInt(elapsedTime));
        }
        if (levelIndex == levels.Length - 1) {
            finalVictoryScreenUI.SetActive(true);
            int bestTime = PlayerPrefs.GetInt("bestComplete");
            if (bestTime == 0 || bestTime > elapsedTime) {
                PlayerPrefs.SetInt("bestComplete", Mathf.CeilToInt(elapsedTime));
            }
        } else {
            victoryScreenUI.SetActive(true);
        }
        audioSource.PlayOneShot(victoryClip);
        playing = false;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;

        ReportCompletionTime();
        ReportLevelsCompleted();
    }

    private void CheckDefeat() {
        foreach (Base bas in allBases) {
            if (bas.owningPlayer == humanPlayer) {
                return;
            }
        }

        if (elapsedTime != -1) {
            elapsedTime += Time.realtimeSinceStartup - levelStartTime;
            PlayerPrefs.SetInt("elapsed", Mathf.CeilToInt(elapsedTime));
        }
        defeatScreenUI.SetActive(true);
        audioSource.PlayOneShot(defeatClip);
        playing = false;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;
    }

    public Material GetPlayerSharedMat(PlayerNum playerNum) {
        return GetPlayerSharedMat(PlayerMethods.GetPlayerColor(playerNum));
    }

    public Material GetPlayerSharedMat(Color color) {
        if (!sharedMats.ContainsKey(color)) {
            Material sharedMat = new Material(playerMat);
            sharedMat.color = color;
            sharedMat.enableInstancing = true;
            sharedMats[color] = sharedMat;
        }
        return sharedMats[color];
    }

    public Base[] GetAllBases() {
        return allBases;
    }

    public Pawn[] GetAllPawns() {
        return FindObjectsOfType<Pawn>();
    }

    ////////////////////
    //  UI METHODS
    ////////////////////
    public void StartGame() {
        elapsedTime = 0;
        RestartLevel();
    }

    public void ContinueGame() {
        elapsedTime = PlayerPrefs.GetInt("elapsed", -1);
        levelIndex = PlayerPrefs.GetInt("level");
        RestartLevel();
    }

    public void NextLevel() {
        RestartLevel();
    }

    public void RestartLevel() {
        // Clear existing level
        foreach (Pawn pawn in FindObjectsOfType<Pawn>()) {
            Destroy(pawn.gameObject);
        }
        Destroy(currentLevel);

        // Setup new level
        currentLevel = Instantiate(levels[levelIndex]);
        currentLevel.SetActive(true);
        allBases = currentLevel.GetComponentsInChildren<Base>();

        // Setup camera
        foreach (Base bas in allBases) {
            if (bas.owningPlayer == humanPlayer) {
                Camera.main.transform.position = bas.transform.position + bas.transform.up * 5;
                cam.SnapToPlanet();
                break;
            }
        }

        // Other state
        playing = true;
        themeSong.SetActive(false);
        startScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);

        levelText.DisplayText(levelIndex + 1, levels[levelIndex].gameObject.name);
        levelStartTime = Time.realtimeSinceStartup;
    }

    public void ShowInstructions() {
        instructionScreenUI.SetActive(true);
        startScreenUI.SetActive(false);
    }

    public void HideInstructions() {
        startScreenUI.SetActive(true);
        instructionScreenUI.SetActive(false);
    }

    public void FactionsEvolvedSignup() {
        Application.ExternalEval("window.open(\"https://www.factionsevolvedgame.com\")");
    }

    ////////////////////
    //  KONGREGATE STATS METHODS
    ////////////////////
    private void ReportLevelsCompleted() {
        if (kongApiInitialized) {
            Application.ExternalCall("kongregate.stats.submit", "Levels Completed", PlayerPrefs.GetInt("maxLevel"));
        }
    }

    private void ReportCompletionTime() {
        if (kongApiInitialized && PlayerPrefs.GetInt("bestComplete") > 0) {
            Application.ExternalCall("kongregate.stats.submit", "Completion Time", PlayerPrefs.GetInt("bestComplete"));
        }
    }

    private void RegisterAPI() {
        Application.ExternalEval(
          @"if(typeof(kongregateUnitySupport) != 'undefined'){
            kongregateUnitySupport.initAPI('GameManager', 'ApiRegisteredCallback');
          };"
        );
    }

    private void ApiRegisteredCallback(string userInfo) {
        kongApiInitialized = true;

        ReportLevelsCompleted();
        ReportCompletionTime();
    }
}
