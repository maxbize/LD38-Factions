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
    public static int p1Bit = 1 << 13;
    public static int p2Bit = 1 << 14;
    public static int p3Bit = 1 << 15;
    public static int p4Bit = 1 << 16;
    public static LayerMask allButP1 = p2Bit | p3Bit | p4Bit;
    public static LayerMask allButP2 = p1Bit | p3Bit | p4Bit;
    public static LayerMask allButP3 = p1Bit | p2Bit | p4Bit;
    public static LayerMask allButP4 = p1Bit | p2Bit | p3Bit;
    public static LayerMask allP = p1Bit | p2Bit | p3Bit | p4Bit;
}
