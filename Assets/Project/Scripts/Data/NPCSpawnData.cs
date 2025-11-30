using System;
using UnityEngine;

// NPCSpawnData describes a single spawn wave entry in chapter configs.
// It is serializable to allow authoring in ScriptableObjects.
[Serializable]
public class NPCSpawnData
{
    #region Fields
    public int Time;
    public int NpcId;
    public int NpcCount;
    public int SpawnPointIndex;
    public Vector3 SpawnPosition;
    #endregion
}
