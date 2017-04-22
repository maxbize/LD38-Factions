using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerNum
{
    Null,
    One,
    Two,
    Three, // Unused?
    Four   // Unused?
}

public static class PlayerMethods
{
    public static Color GetPlayerColor(PlayerNum playerNum) {
        if (playerNum == PlayerNum.One) {
            return Color.red;
        } else if (playerNum == PlayerNum.Two) {
            return Color.blue;
        } else if (playerNum == PlayerNum.Three) {
            return Color.green;
        } else if (playerNum == PlayerNum.Four) {
            return Color.yellow;
        } else {
            return Color.white;
        }
    }
}
