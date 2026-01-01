public static class GameConsts
{
    #region Scene Names
    public const string MAP_GAMEENTRY = "Map_GameEntry";
    public const string MAP_LOGIN = "Map_Login";
    public const string MAP_BATTLESCENE = "Map_BattleScene";
    public const string MAP_LOADING = "Map_Loading";
    public const string MAP_TESTSCENE = "Map_TestScene";
    #endregion

    #region Resource Paths
    // Configs
    public const string PATH_CONFIG_NPC_ATTRIBUTES = "Configs/NpcAttributesConfig";
    public const string PATH_CONFIG_HERO_ATTRIBUTES = "Configs/HeroAttributesConfig";
    public const string PATH_CONFIG_BUFF = "Configs/BuffConfig";
    public const string PATH_CONFIG_MAIN_CHAPTER = ""; // Resources.LoadAll(string.Empty) uses empty string

    // Skills
    public const string PATH_CONFIG_SKILL_PRIMARY = "Configs/Skill/PrimaryAttack";
    public const string PATH_CONFIG_SKILL_ACTIVE = "Configs/Skill/ActiveAoE";
    public const string PATH_CONFIG_SKILL_SECONDARY = "Configs/Skill/ActiveBarrage";
    #endregion
}
