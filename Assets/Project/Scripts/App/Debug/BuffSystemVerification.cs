using UnityEngine;
using System.Collections.Generic;

// Attach this script to any GameObject in the scene to verify the Buff System logic.
public class BuffSystemVerification : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== [BuffSystemVerification] START ===");

        // 1. Initialize Attribute System
        var attrComp = new MPAttributeComponent();
        float baseSpeed = 5.0f;
        attrComp.Initialize(baseSpeed, 10, 100);

        Assert("Initial Speed", attrComp.GetValue(AttributeType.MoveSpeed), baseSpeed);

        // 2. Setup Mock Config
        var configLookup = new Dictionary<BuffType, BuffConfig.BuffEntry>();
        
        // Define a Speed Up Buff: +50% speed (Multiplier 0.5)
        var speedBuffEntry = new BuffConfig.BuffEntry
        {
            Type = BuffType.MoveSpeedUp,
            BuffName = "SpeedUp!",
            Duration = 5.0f,
            MaxStacks = 5,
            MoveSpeedMultiplierPerStack = 0.5f, 
            RefreshDurationOnAdd = true
        };
        configLookup[BuffType.MoveSpeedUp] = speedBuffEntry;

        var buffMgr = new BuffLayerMgr(configLookup, attrComp);

        // 3. Add Buff Test
        Debug.Log("--- Identifying Buff: MoveSpeedUp (+50%) ---");
        buffMgr.TryAddStack(BuffType.MoveSpeedUp);

        // Base 5.0 * (1.0 + 0.5) = 7.5
        Assert("Speed after 1 stack", attrComp.GetValue(AttributeType.MoveSpeed), 7.5f);

        // 4. Stack Buff Test
        buffMgr.TryAddStack(BuffType.MoveSpeedUp);
        // Base 5.0 * (1.0 + 0.5 + 0.5) = 10.0
        Assert("Speed after 2 stacks", attrComp.GetValue(AttributeType.MoveSpeed), 10.0f);

        // 5. Clear Test
        Debug.Log("--- Clearing All Buffs ---");
        buffMgr.ClearAll();
        Assert("Speed after clear", attrComp.GetValue(AttributeType.MoveSpeed), 5.0f);

        Debug.Log("=== [BuffSystemVerification] COMPLETE ===");
    }

    private void Assert(string label, float actual, float expected)
    {
        bool pass = Mathf.Abs(actual - expected) < 0.001f;
        string color = pass ? "green" : "red";
        Debug.Log($"<color={color}>[{label}] Expected: {expected}, Actual: {actual} - {(pass ? "PASS" : "FAIL")}</color>");
    }
}
