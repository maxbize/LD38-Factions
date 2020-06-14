using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/*
 * A pretty basic AI for the enemies. Every frame it updates its understanding of the world and makes
 *  decisions based on it. Its goal is to maximize the amounts of bases captured. It will prioritize
 *  the following activities:
 *     1) Capture bases without owners
 *     2) Defend existing bases
 *     3) Capture enemy bases
 */
public class AI : MonoBehaviour
{

    // Set in editor
    public PlayerNum playerNumber;

    // These are lists instead of sets so that we can put them in priority order
    private List<Base> myBases = new List<Base>();
    private List<Base> enemyBases = new List<Base>();
    private List<Base> freeBases = new List<Base>();

    private HashSet<Pawn> myPawns = new HashSet<Pawn>();
    private GameManager gameManager;
    private Array allNums;
    private int frameIndex;
    private int updateFrame;

    // In-order list of prioritized bases to target, with the ideal number of units per target
    private List<KeyValuePair<Base, int>> idealTargets = new List<KeyValuePair<Base, int>>();

    // Use this for initialization
    void Start() {
        gameManager = FindObjectOfType<GameManager>();

        // Reflection is surprisingly expensive. Cache this
        allNums = Enum.GetValues(typeof(PlayerNum));

        updateFrame = (int)playerNumber - 1; // Null is 0
    }

    // Update is called once per frame
    void Update() {
        // Space out the updates as a hack to decrease CPU load :)
        frameIndex = (frameIndex + 1) % 4;
        if (frameIndex == updateFrame) {
            UpdateWorldState();
            ChooseTargets();
            DistributePawns();
        }
    }

    // Update the AI's understanding of the world
    private void UpdateWorldState() {
        myBases.Clear();
        freeBases.Clear();
        enemyBases.Clear();
        foreach (Base b in gameManager.GetAllBases()) {
            if (b.owningPlayer == playerNumber) {
                myBases.Add(b);
            } else if (b.owningPlayer == PlayerNum.Null) {
                freeBases.Add(b);
            } else {
                enemyBases.Add(b);
            }
        }

        myPawns.Clear();
        // TODO: Maintain the list of all pawns somewhere
        foreach (Pawn pawn in FindObjectsOfType<Pawn>()) {
            if (pawn.owner == playerNumber) {
                myPawns.Add(pawn);
            }
        }
    }

    // Prioritize the bases to target and specify how many pawns we'd like to send to each target
    private void ChooseTargets() {
        idealTargets.Clear();

        foreach (Base bas in gameManager.GetAllBases()) {
            if (bas.pawnsInRange == null) {
                return; // Base just got created. This won't get initialized until next frame during Start...
            }
            int numFriendlies = bas.pawnsInRange.ContainsKey(playerNumber) ? bas.pawnsInRange[playerNumber].Count : 0;
            int numEnemies = 0;
            foreach (PlayerNum num in allNums) {
                if (num != playerNumber && bas.pawnsInRange.ContainsKey(num)) {
                    numEnemies += bas.pawnsInRange[num].Count;
                }
            }

            if (bas.owningPlayer == playerNumber && numEnemies == 0) {
                continue; // No point sending people to defend a base that's not under attack
            }

            // Prioritize according to the lowest amount of units we think we'll need to send
            // TODO: Do we want to prioritize bases where we already have units?
            // TODO: Do we want to prioritize bases that are closer to existing bases?
            int insertionIndex = 0;
            int numToSend = numEnemies + 1;

            // First priority - base owner
            while (insertionIndex < idealTargets.Count &&
                BasePriority(idealTargets[insertionIndex].Key) < BasePriority(bas)) {
                insertionIndex++;
            }
            // Second priority - units required
            while (insertionIndex < idealTargets.Count &&
                idealTargets[insertionIndex].Value < numToSend) {
                insertionIndex++;
            }

            idealTargets.Insert(insertionIndex, new KeyValuePair<Base, int>(bas, numToSend));
        }
    }

    /*
     * Simple way to get different AI behavior
     *  Blue = offense > defense > free
     *  Green = free > offense > defense
     *  Yellow = defense > free > offense
     */
    private int BasePriority(Base bas) {
        if (bas.owningPlayer == PlayerNum.Null) {
            return ((int)playerNumber + 0) % 3;
        } else if (bas.owningPlayer == playerNumber) {
            return ((int)playerNumber + 1) % 3;
        } else {
            return ((int)playerNumber + 2) % 3;
        }
    }

    // Send the pawns to their nearest high priority targets
    private void DistributePawns() {
        if (idealTargets.Count == 0) {
            return; // Nothing to do!
        }

        int leastRequired = 999;
        foreach (KeyValuePair<Base, int> target in idealTargets) {
            if (target.Value < leastRequired) {
                leastRequired = target.Value;
            }
        }

        while (myPawns.Count > 0) { // If we still have resources after a pass just keep sending units at the same priorities
            foreach (KeyValuePair<Base, int> target in idealTargets) {
                Base bas = target.Key;
                int numToSend = target.Value;

                if (numToSend > myPawns.Count) { // We'll be outnumbered, pick other targets
                    if (leastRequired > myPawns.Count) { // Nothing else available - fall back to bases
                        foreach (Pawn pawn in myPawns) {
                            if (myBases.Count > 0) {
                                pawn.SetTargetPos(FindClosestFriendlyBase(pawn).transform.position);
                            } else { // All hope is lost. Last ditch effort - go for a base
                                pawn.SetTargetPos(enemyBases[0].transform.position);
                            }
                        }

                        return; // All resources distributed
                    } else { // Some other targets could use our help!
                        continue;
                    }
                }

                // Create an ordered list of pawns according to distance from the target
                List<Pawn> pawnsToSend = new List<Pawn>(numToSend);
                foreach (Pawn pawn in myPawns) {
                    int insertionIndex = 0;
                    while (insertionIndex < numToSend &&
                        insertionIndex < pawnsToSend.Count &&
                        Vector3.Distance(pawnsToSend[insertionIndex].transform.position, bas.transform.position) < Vector3.Distance(pawn.transform.position, bas.transform.position)) {
                        insertionIndex++;
                    }
                    if (insertionIndex < numToSend) {
                        pawnsToSend.Insert(insertionIndex, pawn);
                    }
                }

                // Send the first n pawns from the list
                for (int i = 0; i < numToSend; i++) {
                    if (myPawns.Count == 0) {
                        return; // All resources distributed
                    }
                    pawnsToSend[i].SetTargetPos(bas.transform.position);
                    myPawns.Remove(pawnsToSend[i]); // No longer available to send somewhere else
                }
            }
        }
    }

    private Base FindClosestFriendlyBase(Pawn pawn) {
        Base closestBase = myBases[0];
        foreach (Base bas in myBases) {
            if (Vector3.Distance(pawn.transform.position, bas.transform.position) < Vector3.Distance(pawn.transform.position, closestBase.transform.position)) {
                closestBase = bas;
            }
        }
        return closestBase;
    }
}
