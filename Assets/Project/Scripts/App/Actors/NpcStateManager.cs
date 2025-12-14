// NpcStateManager provides a minimal state machine for NPCs (birth/idle/chase/stun/dead).
public class NpcStateManager
{
    public enum NpcState
    {
        Birth,
        Idle,
        Chasing,
        Attack,
        Stunned,
        Dead
    }

    public NpcState CurrentState { get; private set; } = NpcState.Birth;

    // Only allow movement logic when Chasing. Idle means waiting, Attack means performing action.
    public bool CanMove => CurrentState == NpcState.Chasing;

    public void ChangeState(NpcState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }

        CurrentState = newState;
    }
}
