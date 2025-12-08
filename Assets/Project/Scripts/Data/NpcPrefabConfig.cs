using System;
using System.Collections.Generic;
using UnityEngine;

// NpcPrefabConfig maps NPC ids to prefab references for spawning.
// Author this ScriptableObject to drive id -> prefab resolution at runtime.
[CreateAssetMenu(fileName = "NpcPrefabConfig", menuName = "Configs/Npc Prefab Config", order = 1)]
public class NpcPrefabConfig : ScriptableObject
{
    [Serializable]
    public class NpcPrefabEntry
    {
        public int NpcId;
        public GameObject Prefab;
    }

    public List<NpcPrefabEntry> Entries = new List<NpcPrefabEntry>();
}
