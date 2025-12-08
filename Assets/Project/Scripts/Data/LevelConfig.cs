using UnityEngine;

// LevelConfig defines minimal level timing configuration.
[CreateAssetMenu(fileName = "LevelConfig", menuName = "Configs/Level Config", order = 1)]
public class LevelConfig : ScriptableObject
{
    public int Duration = 60;
}
