using UnityEngine;

// Define your game events here

public struct PlayerDeadEvent : IEvent
{
    public MPSoulActor Actor;
}

public struct EnemyDeadEvent : IEvent
{
    public MPNpcSoulActor Actor;
    public MPCharacterSoulActorBase Killer;
}

public struct LevelWinEvent : IEvent
{
    public float Duration;
}

public struct LevelFailEvent : IEvent
{
    public float Duration;
}
