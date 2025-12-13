using UnityEngine;

using System;
using System.Collections.Generic;

// HeroAttributesConfig stores base stats for heroes, keyed by heroId.
[CreateAssetMenu(fileName = "HeroAttributesConfig", menuName = "Configs/Hero Attributes Config", order = 20)]
public class HeroAttributesConfig : ScriptableObject
{
    [Serializable]
    public class HeroAttributesEntry
    {
        public int HeroId = 1;
        public int MaxHp = 100;
        public float MoveSpeed = 5f;
        public int AttackDamage = 10;
        public float AttackCooldown = 0.5f;
        public float AttackRange = 10f;
        public GameObject Prefab;
    }

    public List<HeroAttributesEntry> Entries = new List<HeroAttributesEntry>();
}
