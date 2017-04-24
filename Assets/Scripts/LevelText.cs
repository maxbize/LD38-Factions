using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelText : MonoBehaviour {

    public RectTransform textRect;
    public Text levelText;
    public Text levelSubText;
    public AnimationCurve curve;
    public float idealX;

    public float duration;

    private float elapsed;

	// Use this for initialization
	void Start () {
        elapsed = duration;
	}
	
	// Update is called once per frame
	void Update () {
        if (elapsed < duration) {
            elapsed += Time.deltaTime;
            float x = curve.Evaluate(elapsed / duration);
            textRect.anchoredPosition = new Vector2(x, textRect.anchoredPosition.y);
        }
	}

    public void DisplayText(int level, string subText) {
        elapsed = 0;
        levelText.text = "Level " + level;
        levelSubText.text = subText;
    }
}
