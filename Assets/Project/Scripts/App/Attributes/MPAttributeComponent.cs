using System.Collections.Generic;
using UnityEngine;

// Manages numeric attributes (Speed, Attack, etc.) with support for Base value + Buff Multipliers + Buff Additives.
// This implements the "Push" model where values are recalculated when buffs change.
public class MPAttributeComponent
{
    // A single attribute's data
    public class AttributeValue
    {
        public float BaseValue;
        public float BuffAdditive;
        public float BuffMultiplier; // 1.0 = no change

        public float FinalValue => (BaseValue + BuffAdditive) * BuffMultiplier;

        public AttributeValue(float baseVal)
        {
            BaseValue = baseVal;
            BuffAdditive = 0f;
            BuffMultiplier = 1f;
        }

        public void ResetBuffs()
        {
            BuffAdditive = 0f;
            BuffMultiplier = 1f;
        }
    }

    private readonly Dictionary<AttributeType, AttributeValue> _attributes = new Dictionary<AttributeType, AttributeValue>();

    public void Initialize(float baseMoveSpeed, int baseAttack, int maxHp)
    {
        _attributes[AttributeType.MoveSpeed] = new AttributeValue(baseMoveSpeed);
        _attributes[AttributeType.AttackPower] = new AttributeValue(baseAttack);
        _attributes[AttributeType.MaxHp] = new AttributeValue(maxHp);
    }

    public float GetValue(AttributeType type)
    {
        if (_attributes.TryGetValue(type, out var attr))
        {
            return attr.FinalValue;
        }
        return 0f;
    }

    // Set base value (e.g. from config or upgrades)
    public void SetBaseValue(AttributeType type, float value)
    {
        if (!_attributes.TryGetValue(type, out var attr))
        {
            attr = new AttributeValue(value);
            _attributes[type] = attr;
        }
        attr.BaseValue = value;
    }

    // Called by BuffSystem to apply modifications
    public void UpdateBuffModifiers(AttributeType type, float additive, float multiplier)
    {
        if (!_attributes.TryGetValue(type, out var attr))
        {
            return;
        }
        attr.BuffAdditive = additive;
        attr.BuffMultiplier = 1.0f + multiplier;
    }
    
    public void ResetAllBuffModifiers()
    {
        foreach(var attr in _attributes.Values)
        {
            attr.ResetBuffs();
        }
    }
}
