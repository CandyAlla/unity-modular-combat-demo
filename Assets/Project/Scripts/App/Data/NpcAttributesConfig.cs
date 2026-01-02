using System;
using System.Collections.Generic;
using UnityEngine;

// NpcAttributesConfig stores per-npcId base stats for NPCs.
[CreateAssetMenu(fileName = "NpcAttributesConfig", menuName = "Configs/NPC Attributes Config", order = 21)]
public class NpcAttributesConfig : ScriptableObject
{
    [Serializable]
    public class NpcAttributesEntry
    {
        public int NpcId;
        public int MaxHp = 30;
        public float MoveSpeed = 3.5f;
        public int AttackDamage = 10;
        public float AttackInterval = 1.0f;
        public float AttackRange = 1.2f;
        public float SearchRange = 10.0f;
        public PoolKey PoolKey;
        public GameObject Prefab;
    }

    public List<NpcAttributesEntry> Entries = new List<NpcAttributesEntry>();
}
