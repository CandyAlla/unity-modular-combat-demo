using System.Collections.Generic;

// BuffLayerMgr manages buff instances for a single actor and pushes stat changes to MPAttributeComponent.
public class BuffLayerMgr
{
    private readonly Dictionary<BuffType, BuffInstance> _buffs = new Dictionary<BuffType, BuffInstance>();
    private readonly Dictionary<BuffType, BuffConfig.BuffEntry> _configLookup;
    private readonly MPAttributeComponent _attributeComponent;

    public BuffLayerMgr(Dictionary<BuffType, BuffConfig.BuffEntry> configLookup, MPAttributeComponent attributeComponent)
    {
        _configLookup = configLookup;
        _attributeComponent = attributeComponent;
    }

    public System.Action<BuffConfig.BuffEntry> OnBuffAdded;

    public void TryAddStack(BuffType type)
    {
        if (_configLookup == null || !_configLookup.TryGetValue(type, out var cfg) || cfg == null)
        {
            return;
        }

        if (!_buffs.TryGetValue(type, out var instance))
        {
            instance = new BuffInstance(cfg);
            _buffs[type] = instance;
        }

        instance.AddStack();
        RecalculateAttributes();
        OnBuffAdded?.Invoke(cfg);
    }

    public void Tick(float deltaTime)
    {
        var expired = new List<BuffType>();
        bool changed = false;

        foreach (var kv in _buffs)
        {
            kv.Value.Tick(deltaTime);
            if (kv.Value.IsExpired)
            {
                expired.Add(kv.Key);
                changed = true;
            }
        }

        for (int i = 0; i < expired.Count; i++)
        {
            _buffs.Remove(expired[i]);
        }

        if (changed)
        {
            RecalculateAttributes();
        }
    }

    public void ClearAll()
    {
        _buffs.Clear();
        RecalculateAttributes();
    }

    private void RecalculateAttributes()
    {
        if (_attributeComponent == null) return;

        // 1. Reset all buffs in component
        _attributeComponent.ResetAllBuffModifiers();

        // 2. Aggregate all buffs
        float speedAdd = 0f, speedMul = 0f;
        float atkAdd = 0f, atkMul = 0f; // currently only have entries for these in BuffConfig

        foreach (var buff in _buffs.Values)
        {
            int stacks = buff.Stacks;
            // Config currently only has 'MultiplierPerStack' for speed, let's assume it maps to multiplier
            // But usually 'Multiplier' in games means (1 + x), so 0.1f means +10%.
            
            if (buff.Config.MoveSpeedMultiplierPerStack != 0)
            {
                speedMul += buff.Config.MoveSpeedMultiplierPerStack * stacks;
            }
            
            if (buff.Config.AttackBonusPerStack != 0)
            {
                // Assuming 'Bonus' is additive attack power
                atkAdd += buff.Config.AttackBonusPerStack * stacks;
            }
        }

        // 3. Push to component
        _attributeComponent.UpdateBuffModifiers(AttributeType.MoveSpeed, speedAdd, speedMul);
        _attributeComponent.UpdateBuffModifiers(AttributeType.AttackPower, atkAdd, atkMul);
    }
}
