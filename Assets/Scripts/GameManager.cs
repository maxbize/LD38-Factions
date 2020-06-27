// Choose web platform
//#define KONG
//#define NEWGROUNDS
#define ARMORGAMES
//#define CRAZYGAMES
//#define MINICLIP
//#define ITCH
//#define GAMEJOLT

using System;
using System.Linq;
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
    public RectTransform settingsPanel;
    public GameObject settingsButtonUI;
    public GameObject colorPickerUI;
    public List<Image> playerColorPickers;
    public List<GameObject> colorblindOptions;
    public Toggle graphicsToggle;
    public Toggle cameraToggle;
    public Toggle colorblindToggle;
    public Toggle markersToggle;
    public Slider saturationSlider;
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
    private Dictionary<string, Material> sharedMats = new Dictionary<string, Material>();
    private Base[] allBases = new Base[0];
    private AudioSource audioSource;
    private LevelText levelText;
    private CameraManager cam;
    private float slowmoStartMarker;
    private float elapsedTime;
    private float levelStartTime;
    private int colorPickerIndex;
    private GameObject planet;
    private List<GameObject> clouds;
    private List<Color> originalEnvColors; // 0-3 = planet, 4 = clouds
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

        // Shared Materials - Pawns
        foreach (PlayerNum num in Enum.GetValues(typeof(PlayerNum))) {
            Material sharedMat = new Material(playerMat);
            sharedMat.color = GetPlayerColor(num);
            sharedMat.enableInstancing = true;
            sharedMats[num.ToString()] = sharedMat;
        }
        Material highlightSharedMat = new Material(playerMat);
        highlightSharedMat.color = GetHighlightedColor();
        highlightSharedMat.enableInstancing = true;
        sharedMats["highlight"] = highlightSharedMat;

        // Shared Materials - Environment
        originalEnvColors = new List<Color>();
        planet = FindObjectOfType<Planet>().gameObject;
        clouds = FindObjectsOfType<Cloud>().Select(c => c.gameObject).ToList();
        originalEnvColors.AddRange(planet.GetComponentInChildren<Renderer>().materials.Select(m => m.color));
        Material cloudMat = clouds[0].GetComponentInChildren<Renderer>().material;
        originalEnvColors.Add(cloudMat.color);
        cloudMat.color = cloudMat.color; // Force the material to instance so that we can share the single instance
        clouds.ForEach(c => c.GetComponentInChildren<Renderer>().sharedMaterial = cloudMat);

        // Settings
        int graphics = PlayerPrefs.GetInt("Quality", 3);
        graphicsToggle.isOn = graphics == 0;
        QualitySettings.SetQualityLevel(graphics);
        bool inverse = PlayerPrefs.GetInt("Inverse", 0) == 1;
        cameraToggle.isOn = inverse;
        cam.invertDir = inverse;
        colorblindToggle.isOn = PlayerPrefs.GetInt("colorblind", 0) == 1;
        bool markers = PlayerPrefs.GetInt("markers", 0) == 1;
        markersToggle.isOn = markers;
        ToggleMarkers(markers);
        float saturation = PlayerPrefs.GetFloat("saturation", 1);
        saturationSlider.value = saturation;
        SaturateColors(saturation);

        // UI
        startScreenUI.SetActive(true);
        instructionScreenUI.SetActive(false);
        victoryScreenUI.SetActive(false);
        finalVictoryScreenUI.SetActive(false);
        defeatScreenUI.SetActive(false);
        timerText.gameObject.SetActive(false);
        settingsScreenUI.SetActive(false);
        settingsButtonUI.SetActive(false);

        string version = "v 1.11   ";
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
#elif GAMEJOLT
        versionText.text = version + "Game Jolt";
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
        return sharedMats[playerNum.ToString()];
    }

    public Material GetPlayerSharedMat(int pickerIndex) {
        if (pickerIndex == 0) {
            return sharedMats[PlayerNum.One.ToString()];
        } else if (pickerIndex == 1) {
            return GetHighlightedMaterial();
        } else if (pickerIndex == 2) {
            return sharedMats[PlayerNum.Two.ToString()];
        } else if (pickerIndex == 3) {
            return sharedMats[PlayerNum.Three.ToString()];
        } else {
            return sharedMats[PlayerNum.Four.ToString()];
        }
    }

    public Base[] GetAllBases() {
        return allBases;
    }

    public Pawn[] GetAllPawns() {
        return FindObjectsOfType<Pawn>();
    }

    public Color GetPlayerColor(PlayerNum playerNum) {
        if (playerNum == PlayerNum.One) {
            return FromHex(PlayerPrefs.GetString("color0", "#B71C1C"));
        } else if (playerNum == PlayerNum.Two) {
            return FromHex(PlayerPrefs.GetString("color2", "#0D47A1"));
        } else if (playerNum == PlayerNum.Three) {
            return FromHex(PlayerPrefs.GetString("color3", "#1B5E20"));
        } else if (playerNum == PlayerNum.Four) {
            return FromHex(PlayerPrefs.GetString("color4", "#F57F17"));
        } else {
            return FromHex(0xFAFAFA);
        }
    }

    private Color GetPlayerColor(int pickerIndex) {
        if (pickerIndex == 0) {
            return GetPlayerColor(PlayerNum.One);
        } else if (pickerIndex == 1) {
            return GetHighlightedColor();
        } else if (pickerIndex == 2) {
            return GetPlayerColor(PlayerNum.Two);
        } else if (pickerIndex == 3) {
            return GetPlayerColor(PlayerNum.Three);
        } else {
            return GetPlayerColor(PlayerNum.Four);
        }
    }

    public Material GetHighlightedMaterial() {
        return sharedMats["highlight"];
    }

    public Color GetHighlightedColor() {
        return FromHex(PlayerPrefs.GetString("color1", "#FF00FF"));
    }

    public static Color FromHex(int hex) {
        return new Color(
            ((hex & 0xFF0000) >> 16) / (float)0xFF,
            ((hex & 0x00FF00) >> 8) / (float)0xFF,
            ((hex & 0x0000FF) >> 0) / (float)0xFF
            );
    }

    public static Color FromHex(string hex) {
        Color color = new Color();
        if (!ColorUtility.TryParseHtmlString(hex, out color)) {
            Debug.LogWarning("Could not parse hex to color: " + hex);
        }
        return color;
    }

    public void SaturateColors(float saturation) {
        // planet
        Material[] planetMats = planet.GetComponentInChildren<Renderer>().materials;
        for (int i = 0; i < planetMats.Length; i++) {
            Color originalColor = originalEnvColors[i];
            Color.RGBToHSV(originalColor, out float h, out float s, out float v);
            planetMats[i].color = Color.HSVToRGB(h, s * saturation, v);
        }

        // clouds
        Color originalCloudColor = originalEnvColors.Last();
        Color.RGBToHSV(originalCloudColor, out float h2, out float s2, out float v2);
        clouds[0].GetComponentInChildren<Renderer>().sharedMaterial.color = Color.HSVToRGB(h2, s2 * saturation, v2);
    }

    public bool MarkersEnabled() {
        return PlayerPrefs.GetInt("markers", 0) == 1;
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
        settingsPanel.gameObject.SetActive(false);
        startScreenUI.SetActive(false);
    }

    public void HideInstructions() {
        if (gameState == GameState.Menu) {
            startScreenUI.SetActive(true);
        }
        instructionScreenUI.SetActive(false);
        settingsPanel.gameObject.SetActive(true);
    }

    public void FactionsEvolvedSignup() {
#if UNITY_WEBGL
        Application.ExternalEval("window.open(\"https://www.factionsevolvedgame.com\")");
#else
        Application.OpenURL("https://www.factionsevolvedgame.com");
#endif
    }

    public void ToggleQuality(bool enabled) {
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
        if (gameState == GameState.Menu) {
            return; // Setting up toggles at start can mess this up
        }

        settingsScreenUI.SetActive(true);
        settingsPanel.gameObject.SetActive(true);
        colorPickerUI.SetActive(false);
        bool colorblind = PlayerPrefs.GetInt("colorblind") == 1;
        foreach (GameObject go in colorblindOptions) {
            go.SetActive(colorblind);
        }
        for (int i = 0; i < playerColorPickers.Count; i++) {
            playerColorPickers[i].color = GetPlayerColor(i);
        }
        settingsPanel.sizeDelta = new Vector2(550, colorblind ? 700 : 360);
        gameState = GameState.InGamePaused;
        Time.timeScale = 0;
    }

    public void HideSettings() {
        if (colorPickerUI.activeSelf) {
            ShowSettings();
        } else {
            settingsScreenUI.SetActive(false);
            if (!victoryScreenUI.activeSelf && !defeatScreenUI.activeSelf && !finalVictoryScreenUI.activeSelf) {
                gameState = GameState.InGamePlaying;
            }
            Time.timeScale = 1;
        }
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

        int savedIndex = PlayerPrefs.GetInt("level");
        if (savedIndex == 0) {
            continueGameButton.interactable = false;
            continueGameButton.GetComponentInChildren<Text>().color = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        } else {
            continueGameButton.interactable = true;
            continueGameButton.GetComponentInChildren<Text>().color = new Color(1f, 1f, 1f, 1f);
        }
    }

    // Accessability UI. Doesn't belong here but whatever, this game is almost done :)
    public void ToggleColorblind(bool enabled) {
        PlayerPrefs.SetInt("colorblind", enabled ? 1 : 0);
        ShowSettings();
    }

    public void ToggleMarkers(bool enabled) {
        PlayerPrefs.SetInt("markers", enabled ? 1 : 0);
        Array.ForEach(GetAllPawns(), p => p.SetMarkers(enabled));
    }

    public void OpenColorPicker(int playerIndex) {
        colorPickerIndex = playerIndex;
        settingsPanel.gameObject.SetActive(false);
        colorPickerUI.SetActive(true);
        colorPickerUI.GetComponent<ColorPicker>().CurrentColor = GetPlayerColor(playerIndex);
    }

    public void SetPlayerColor(Color color) {
        GetPlayerSharedMat(colorPickerIndex).color = color;
        PlayerPrefs.SetString("color" + colorPickerIndex, "#" + ColorUtility.ToHtmlStringRGB(color));
    }

    public void SetWorldSaturation(float amount) {
        PlayerPrefs.SetFloat("saturation", amount);
        SaturateColors(amount);
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
