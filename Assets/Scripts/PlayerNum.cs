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
            return FromHex(0xB71C1C);
        } else if (playerNum == PlayerNum.Two) {
            return FromHex(0x0D47A1);
        } else if (playerNum == PlayerNum.Three) {
            return FromHex(0x1B5E20);
        } else if (playerNum == PlayerNum.Four) {
            return FromHex(0xF57F17);
        } else {
            return FromHex(0xFAFAFA);
        }
    }

    private static Color FromHex(int hex) {
        return new Color(
            ((hex & 0xFF0000) >> 16) / (float)0xFF,
            ((hex & 0x00FF00) >> 8) / (float)0xFF,
            ((hex & 0x0000FF) >> 0) / (float)0xFF
            );
    }
}
