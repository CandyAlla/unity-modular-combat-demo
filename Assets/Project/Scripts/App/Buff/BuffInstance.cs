// BuffInstance tracks runtime state for a specific BuffType.
public class BuffInstance
{
    public BuffType Type { get; }
    public BuffConfig.BuffEntry Config { get; }
    public int Stacks { get; private set; }
    public float RemainingTime { get; private set; }

    public BuffInstance(BuffConfig.BuffEntry config)
    {
        Config = config;
        Type = config.Type;
        Stacks = 0;
        RemainingTime = 0f;
    }

    public void AddStack()
    {
        Stacks = System.Math.Min(Config.MaxStacks, Stacks + 1);
        if (Config.RefreshDurationOnAdd || RemainingTime <= 0f)
        {
            RemainingTime = Config.Duration;
        }
    }

    public void Tick(float deltaTime)
    {
        if (RemainingTime > 0f)
        {
            RemainingTime -= deltaTime;
        }
    }

    public bool IsExpired => RemainingTime <= 0f || Stacks <= 0;
}
