using System.Collections.Generic;

// MainChapterInfo is the runtime snapshot version of MainChapterConfig.
// It is a plain C# container used during battle flow.
public class MainChapterInfo
{
    #region Fields
    public int StageId;
    public int Duration;
    public List<NPCSpawnData> Monsters = new List<NPCSpawnData>();
    #endregion
}
