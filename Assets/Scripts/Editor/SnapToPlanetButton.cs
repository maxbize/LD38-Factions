using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(Base))]
[CanEditMultipleObjects]
public class SnapToPlanetButton : Editor
{
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        Base myScript = (Base)target;
        if (GUILayout.Button("Snap To Planet")) {
            foreach (Base bas in FindObjectsOfType<Base>()) {
                bas.SnapToPlanet();
            }
        }
    }
}