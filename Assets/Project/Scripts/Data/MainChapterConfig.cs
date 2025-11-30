using System.Collections.Generic;
using UnityEngine;

// MainChapterConfig is a ScriptableObject for authoring chapter waves and duration.
// It can host multiple stages; DataCtrl loads and converts it to runtime info.
[CreateAssetMenu(fileName = "MainChapterConfig", menuName = "Configs/Main Chapter Config", order = 0)]
public class MainChapterConfig : ScriptableObject
{
    #region Fields
    public int StageId;
    public int Duration;
    public List<NPCSpawnData> Waves = new List<NPCSpawnData>();
    #endregion
}
