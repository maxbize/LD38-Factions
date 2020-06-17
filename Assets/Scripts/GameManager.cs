// Choose web platform
//#define KONG
//#define NEWGROUNDS
//#define ARMORGAMES
//#define CRAZYGAMES
//#define MINICLIP
#define ITCH

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
    public GameObject startScreenUI;
    public GameObject victoryScreenUI;
    public GameObject defeatScreenUI;
    public GameObject finalVictoryScreenUI;
    public GameObject instructionScreenUI;
    public GameObject settingsScreenUI;
    public GameObject settingsButtonUI;
    public Toggle graphicsToggle;
    public Toggle cameraToggle;
    public Button continueGameButton;
    public Text versionText;
    public Text timerText;
    public AudioClip victoryClip;
    public AudioClip defeatClip;
    public GameObject themeSong;
    public float slowDownTime;

    public enum GameState
    {
        Menu,
        InGamePaused,
        InGamePlaying
    }
    public GameState gameState { get; private set; }

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
#if KONG
    private bool kongApiInitialized;
#elif NEWGROUNDS
    private io.newgrounds.core ngio;
    private bool ngioReady;
#endif

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

        // Transforms are manually moved every frame to snap to the planet. With auto-sync, they
        //  are synced to the Physics engine with every raycast/overlap. Since we're barely moving
        //  every frame, it's OK to turn this off - sync will happen once at the beginning of the frame
        Physics.autoSyncTransforms = false;

        int savedIndex = PlayerPrefs.GetInt("level");
        if (savedIndex == 0) {
            continueGameButton.interactable = false;
            continueGameButton.GetComponentInChildren<Text>().color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        }

        audioSource = GetComponent<AudioSource>();
        levelText = FindObjectOfType<LevelText>();
        cam = FindObjectOfType<CameraManager>();

        // Settings
        int graphics = PlayerPrefs.GetInt("Quality", 3);
        graphicsToggle.isOn = graphics == 0;
        QualitySettings.SetQualityLevel(graphics);
        bool inverse = PlayerPrefs.GetInt("Inverse", 0) == 1;
        cameraToggle.isOn = inverse;
        cam.invertDir = inverse;

        startScreenUI.SetActive(true);
        instructionScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        finalVictoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);
        timerText.gameObject.SetActive(false);
        settingsScreenUI.SetActive(false);
        settingsButtonUI.SetActive(false);

        string version = "v 1.10   ";
#if KONG
        KongRegisterAPI();
        versionText.text = version + "Kongregate";
#elif NEWGROUNDS
        NgRegisterAPI();
        versionText.text = version + "Newgrounds";
#elif ARMORGAMES
        versionText.text = version + "Armor Games";
#elif CRAZYGAMES
        versionText.text = version + "Crazy Games";
#elif MINICLIP
        versionText.text = version + "Miniclip";
#elif ITCH
        versionText.text = version + "itch.io";
#endif
    }

    // Update is called once per frame
    void Update () {
        if (gameState == GameState.InGamePlaying) {
            HandleTimeScale();
            CheckVictory();
            CheckDefeat();
            if (Input.GetKeyDown(KeyCode.R)) {
                if (elapsedTime != -1) {
                    elapsedTime += Time.realtimeSinceStartup - levelStartTime;
                    PlayerPrefs.SetInt("elapsed", Mathf.CeilToInt(elapsedTime));
                    ActivateTimerText();
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
            ActivateTimerText();
        }
        if (levelIndex == levels.Length) {
            finalVictoryScreenUI.SetActive(true);
            int bestTime = PlayerPrefs.GetInt("bestComplete");
            if (bestTime == 0 || bestTime > elapsedTime) {
                PlayerPrefs.SetInt("bestComplete", Mathf.CeilToInt(elapsedTime));
            }
        } else {
            victoryScreenUI.SetActive(true);
        }
        audioSource.PlayOneShot(victoryClip);
        gameState = GameState.InGamePaused;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;

#if KONG
        KongReportCompletionTime();
        KongReportLevelsCompleted();
#elif NEWGROUNDS
        NgReportCompletionTime();
        NgReportLevelsCompleted();
#endif
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
            ActivateTimerText();
        }
        defeatScreenUI.SetActive(true);
        audioSource.PlayOneShot(defeatClip);
        gameState = GameState.InGamePaused;
        Time.timeScale = 1;
        Time.fixedDeltaTime = 0.02f;
    }

    // Enable and update text
    private void ActivateTimerText() {
        timerText.gameObject.SetActive(true);
        TimeSpan t = TimeSpan.FromSeconds(elapsedTime);
        timerText.text = t.ToString(elapsedTime >= 3600 ? @"hh\:mm\:ss\.f" : @"mm\:ss\.f").Replace(":", "<b>:</b>");
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
        levelIndex = 0;
        RestartLevel();
    }

    public void ContinueGame() {
        elapsedTime = PlayerPrefs.GetInt("elapsed", -1);
        levelIndex = Mathf.Min(11, PlayerPrefs.GetInt("level"));
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
        gameState = GameState.InGamePlaying;
        themeSong.SetActive(false);
        startScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);
        timerText.gameObject.SetActive(false);
        settingsScreenUI.SetActive(false);
        settingsButtonUI.SetActive(true);

        levelText.DisplayText(levelIndex + 1, levels[levelIndex].gameObject.name);
        levelStartTime = Time.realtimeSinceStartup;
    }

    public void ShowInstructions() {
        instructionScreenUI.SetActive(true);
        startScreenUI.SetActive(false);
    }

    public void HideInstructions() {
        if (gameState == GameState.Menu) {
            startScreenUI.SetActive(true);
        }
        instructionScreenUI.SetActive(false);
    }

    public void FactionsEvolvedSignup() {
        Application.ExternalEval("window.open(\"https://www.factionsevolvedgame.com\")");
    }

    public void ToggleQuality(bool enabled) {
        Debug.Log("Toggle quality " + enabled);
        int quality = enabled ? 0 : 3;
        QualitySettings.SetQualityLevel(quality);
        PlayerPrefs.SetInt("Quality", quality);
    }

    public void ToggleCameraInverse(bool enabled) {
        Debug.Log("Toggle inverse " + enabled);
        cam.invertDir = enabled;
        PlayerPrefs.SetInt("Inverse", enabled ? 1 : 0);
    }

    public void ShowSettings() {
        settingsScreenUI.SetActive(true);
        gameState = GameState.InGamePaused;
        Time.timeScale = 0;
    }

    public void HideSettings() {
        settingsScreenUI.SetActive(false);
        if (!victoryScreenUI.activeSelf && !defeatScreenUI.activeSelf && !finalVictoryScreenUI.activeSelf) {
            gameState = GameState.InGamePlaying;
        }
        Time.timeScale = 1;
    }

    public void ReturnToMenu() {
        // Reset UI state
        startScreenUI.SetActive(true);
        instructionScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        finalVictoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);
        timerText.gameObject.SetActive(false);
        settingsScreenUI.SetActive(false);
        settingsButtonUI.SetActive(false);

        // Clear existing level
        foreach (Pawn pawn in FindObjectsOfType<Pawn>()) {
            Destroy(pawn.gameObject);
        }
        Destroy(currentLevel);

        // Other state
        gameState = GameState.Menu;
        Time.timeScale = 1;
        levelText.EndDisplay();
        themeSong.SetActive(true);
    }

#if KONG
    ////////////////////
    //  KONGREGATE STATS METHODS
    ////////////////////
    private void KongReportLevelsCompleted() {
        if (kongApiInitialized) {
            Application.ExternalCall("kongregate.stats.submit", "Levels Completed", PlayerPrefs.GetInt("maxLevel"));
        }
    }

    private void KongReportCompletionTime() {
        if (kongApiInitialized && PlayerPrefs.GetInt("bestComplete") > 0) {
            Application.ExternalCall("kongregate.stats.submit", "Completion Time", PlayerPrefs.GetInt("bestComplete"));
        }
    }

    private void KongRegisterAPI() {
        Application.ExternalEval(
          @"if(typeof(kongregateUnitySupport) != 'undefined'){
            kongregateUnitySupport.initAPI('GameManager', 'KongApiRegisteredCallback');
          };"
        );
    }

    private void KongApiRegisteredCallback(string userInfo) {
        kongApiInitialized = true;

        KongReportLevelsCompleted();
        KongReportCompletionTime();
    }

#elif NEWGROUNDS
    ////////////////////
    //  NEWGROUNDS STATS METHODS
    ////////////////////
    private void NgRegisterAPI() {
        GameObject ngioObj = new GameObject("Newgrounds IO");
        ngio = ngioObj.AddComponent<io.newgrounds.core>();
        ngio.app_id = "50429:7gkqTTnQ";
        ngio.aes_base64_key = "lXZKjQui8a/LZ3YVG5+jGw==";
        ngio.onReady(NgOnReady);
    }

    private void NgOnReady() {
        Debug.Log("NG IO ready");
        ngioReady = true;

        NgReportCompletionTime();
        NgReportLevelsCompleted();
    }

    private void NgReportLevelsCompleted() {
        if (ngioReady) {
            int maxLevel = PlayerPrefs.GetInt("maxLevel");
            if (maxLevel >= 1) {
                NgUnlockMedal(59724);
            }
            if (maxLevel >= 6) {
                NgUnlockMedal(59725);
            }
            if (maxLevel >= 12) {
                NgUnlockMedal(59726);
            }
        }
    }

    private void NgReportCompletionTime() {
        int bestComplete = PlayerPrefs.GetInt("bestComplete");
        if (ngioReady && bestComplete > 0) {
            if (bestComplete <= 600) {
                NgUnlockMedal(59727);
            }
            NgSubmitStat(9088, bestComplete * 1000); // convert to ms
        }
    }

    private void NgUnlockMedal(int id) {
        io.newgrounds.components.Medal.unlock unlock = new io.newgrounds.components.Medal.unlock();
        unlock.id = id;
        //unlock.callWith(ngio, NgUnlockMedalCallback);
        unlock.callWith(ngio, NgUnlockMedalCallback);
    }

    private void NgUnlockMedalCallback(io.newgrounds.results.Medal.unlock result) {
        io.newgrounds.objects.medal medal = result.medal;
        Debug.LogFormat("Unlocked medal '{0}' - {1} points", medal.name, medal.value);
    }

    private void NgSubmitStat(int id, int stat) {
        io.newgrounds.components.ScoreBoard.postScore post = new io.newgrounds.components.ScoreBoard.postScore();
        post.id = 9088;
        post.value = stat;
        post.callWith(ngio, NgSubmitStatCallback);
    }

    private void NgSubmitStatCallback(io.newgrounds.results.ScoreBoard.postScore result) {
        io.newgrounds.objects.score score = result.score;
        Debug.LogFormat("Submitted stat with value {0}", score.value);
    }

#endif
}
