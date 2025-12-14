using System;
using System.Collections.Generic;
using UnityEngine;

// BuffConfig is a data container describing base parameters for each buff type.
[CreateAssetMenu(fileName = "BuffConfig", menuName = "Configs/Buff Config", order = 30)]
public class BuffConfig : ScriptableObject
{
    [Serializable]
    public class BuffEntry
    {
        public BuffType Type;
        public string BuffName;
        public float Duration = 5f;
        public int MaxStacks = 5;
        public bool RefreshDurationOnAdd = true;
        public float MoveSpeedMultiplierPerStack = 0f;
        public float AttackBonusPerStack = 0f;
    }

    public List<BuffEntry> Entries = new List<BuffEntry>();
}
