﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin.Services;
using ImGuiNET;
using Newtonsoft.Json;
using RadarPlugin.Enums;

namespace RadarPlugin;

[Serializable]
public class Configuration
{
    public class LevelRendering
    {
        public bool LevelRenderingEnabled = false;
        public int RelativeLevelsBelow = 20;
        public ESPOption LevelRenderEspOption = new(mobOptDefault);
    }

    public class FontSettings
    {
        public bool UseCustomFont = false;
        public bool UseAxisFont = false;
        public float FontSize = ImGui.GetFontSize();
    }

    public class LocalMobsUISettings
    {
        public bool Duplicates = false;
        public bool ShowPlayers = false;
        public bool ShowNpcs = true;
    }

    public class HitboxOptions
    {
        public bool HitboxEnabled = false;
        public bool OverrideMobColor = false;
        public uint HitboxColor = UtilInfo.Turquoise;
        public float Thickness = 2.2f;

        public bool DrawInsideCircle = false;
        public uint InsideCircleOpacity = 0xffffffff;
        public bool UseDifferentInsideCircleColor = false;
        public uint InsideCircleColor = UtilInfo.Turquoise & 0x50ffffff;
    }

    public class OffScreenObjectsOptions
    {
        public float Thickness = 2.2f;
        public float DistanceFromEdge = 15f;
        public float Size = 6.0f;
    }

    public class DeepDungeonOptions
    {
        public ESPOption SpecialUndeadOption { get; set; } = new(mobOptDefault) { ColorU = UtilInfo.Yellow };
        public ESPOption AuspiceOption { get; set; } = new(mobOptDefault) { ColorU = UtilInfo.Green };
        public ESPOption EasyMobOption { get; set; } = new(mobOptDefault) { ColorU = UtilInfo.LightBlue };
        public ESPOption TrapOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Orange };
        public ESPOption ReturnOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Blue };
        public ESPOption PassageOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Blue };
        public ESPOption GoldChestOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Gold };
        public ESPOption SilverChestOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Silver };
        public ESPOption BronzeChestOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Bronze };
        public ESPOption MimicOption { get; set; } = new(mobOptDefault) { ColorU = UtilInfo.Red };
        public ESPOption AccursedHoardOption { get; set; } = new(objectOptDefault) { ColorU = UtilInfo.Turquoise };
        public ESPOption DefaultEnemyOption { get; set; } = new(mobOptDefault) { ColorU = UtilInfo.White };
    }

    public class AggroRadiusOptions
    {
        public bool ShowAggroCircle = false;
        public bool ShowAggroCircleInCombat = false;
        public bool MaxDistanceCapBool = true;
        public float MaxDistance = UtilInfo.DefaultMaxAggroRadiusDistance;
        public uint FrontColor = UtilInfo.Red;
        public uint RearColor = UtilInfo.Green;
        public uint RightSideColor = UtilInfo.Yellow;
        public uint LeftSideColor = UtilInfo.Yellow;
        public uint FrontConeColor = UtilInfo.Red;
        public uint CircleOpacity = 0xBEFFFFFF;
        public uint FrontConeOpacity = 0x30FFFFFF;
    }

    public class SeparatedEspOption
    {
        public bool Enabled = false;
        public ESPOption EspOption = new(objectOptDefault);
    }

    public class ESPOption
    {
        public ESPOption()
        {
        }

        // Copy Constructor
        public ESPOption(ESPOption espOption)
        {
            Enabled = espOption.Enabled;
            DisplayType = espOption.DisplayType;
            ColorU = espOption.ColorU;
            ShowFC = espOption.ShowFC;
            DrawDistance = espOption.DrawDistance;
            AppendLevelToName = espOption.AppendLevelToName;
        }

        public bool Enabled = true;

        /*
        public bool ShowDot = true;
        public bool ShowHp = false;
        public bool ReplaceDotWithHP = false;
        public bool ShowName = true;*/
        public bool ShowMp = false;

        public DisplayTypes DisplayType = DisplayTypes.NameOnly;
        public uint ColorU = 0xffffffff;
        public bool ShowFC = false; // Unused
        public bool DrawDistance = false;
        public bool DotSizeOverride = false;
        public float DotSize = UtilInfo.DefaultDotSize;
        public bool ReplaceWithJobName = false;
        public bool AppendLevelToName = false;
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 2;
        public string ConfigName = "default";
        public bool Enabled = true;
        public bool UseBackgroundDrawList = false;
        public bool ShowBaDdObjects = true;
        public bool DebugMode = false;
        public bool RankText = false;
        public bool DebugText = false;
        public bool ShowNameless = false;
        public bool ShowOnlyVisible = true;
        public bool OverrideShowInvisiblePlayerCharacters = true;
        public bool ShowOffScreen = false;
        public OffScreenObjectsOptions OffScreenObjectsOptions { get; set; } = new();
        public DeepDungeonOptions DeepDungeonOptions { get; set; } = new();
        public AggroRadiusOptions AggroRadiusOptions { get; set; } = new();
        public ESPOption NpcOption { get; set; } = new(mobOptDefault) { Enabled = true, AppendLevelToName = false };
        public ESPOption PlayerOption { get; set; } = new(playerOptDefault);
        public ESPOption YourPlayerOption { get; set; } = new(playerOptDefault) { ColorU = UtilInfo.Turquoise };
        public ESPOption FriendOption { get; set; } = new(playerOptDefault) { ColorU = UtilInfo.Orange };
        public ESPOption AllianceOption { get; set; } = new(playerOptDefault) { ColorU = UtilInfo.Gold };
        public ESPOption PartyOption { get; set; } = new(playerOptDefault) { ColorU = UtilInfo.Turquoise };
        public ESPOption TreasureOption { get; set; } = new(objectOptDefault) { Enabled = true };
        public ESPOption CompanionOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption AreaOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption AetheryteOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption EventNpcOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption EventObjOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption GatheringPointOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption MountOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption RetainerOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption HousingOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption CutsceneOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption CardStandOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public ESPOption OrnamentOption { get; set; } = new(objectOptDefault) { Enabled = false };
        public HashSet<uint> DataIdIgnoreList { get; set; } = new HashSet<uint>();
        public Dictionary<uint, uint> ColorOverride { get; set; } = new Dictionary<uint, uint>();
        public HitboxOptions HitboxOptions { get; set; } = new();
        public LocalMobsUISettings LocalMobsUiSettings { get; set; } = new();
        public float DotSize = UtilInfo.DefaultDotSize;
        public bool SeparateAlliance = false;
        public bool SeparateYourPlayer = false;
        public bool SeparateParty = false;
        public bool SeparateFriends = false;
        public bool UseMaxDistance = false;
        public float MaxDistance = UtilInfo.DefaultMaxEspDistance;
        public FontSettings FontSettings { get; set; } = new();
        public LevelRendering LevelRendering { get; set; } = new();
        public float EspPadding = UtilInfo.DefaultEspPadding;

        public SeparatedEspOption SeparatedAlliance = new()
            { EspOption = new ESPOption(playerOptDefault) { ColorU = UtilInfo.Gold } };

        public SeparatedEspOption SeparatedYourPlayer = new()
            { EspOption = new ESPOption(playerOptDefault) { ColorU = UtilInfo.Turquoise } };

        public SeparatedEspOption SeparatedParty = new()
            { EspOption = new ESPOption(playerOptDefault) { ColorU = UtilInfo.Turquoise } };

        public SeparatedEspOption SeparatedFriends = new()
            { EspOption = new ESPOption(playerOptDefault) { ColorU = UtilInfo.Orange } };

        public SeparatedEspOption SeparatedRankOne = new()
            { EspOption = new ESPOption(mobOptDefault) { ColorU = UtilInfo.Gold } };

        public SeparatedEspOption SeparatedRankTwoAndSix = new()
            { EspOption = new ESPOption(mobOptDefault) { ColorU = UtilInfo.Yellow } };
    }

    public Config cfg;

    [NonSerialized] private DalamudPluginInterface pluginInterface;

    [NonSerialized] private static readonly ESPOption playerOptDefault = new ESPOption
    {
        Enabled = true,
        ColorU = 0xffff00ff,
        DisplayType = DisplayTypes.DotAndName,
        ShowFC = false,
        DrawDistance = false
    };

    [NonSerialized] private static readonly ESPOption objectOptDefault = new ESPOption
    {
        Enabled = true,
        ColorU = 0xffFFFF00,
        DisplayType = DisplayTypes.NameOnly,
        ShowFC = false,
        DrawDistance = false
    };

    [NonSerialized] private static readonly ESPOption mobOptDefault = new ESPOption
    {
        Enabled = true,
        ColorU = 0xffffffff,
        DisplayType = DisplayTypes.HealthValueAndName,
        ShowFC = false,
        DrawDistance = false,
    };

    [NonSerialized] public string[] configs = new[] { "" };

    [NonSerialized] public int selectedConfig = 0;
    [NonSerialized] private readonly IPluginLog pluginLog;

    public Configuration(DalamudPluginInterface pluginInterface, IPluginLog pluginLog)
    {
        this.pluginInterface = pluginInterface;
        cfg = this.pluginInterface.GetPluginConfig() as Config ?? new Config();
        MigrateCfg(ref cfg);

        var configDirectory = this.pluginInterface.ConfigDirectory;
        if (!configDirectory.Exists)
        {
            configDirectory.Create();
        }

        this.pluginLog = pluginLog;
        UpdateConfigs();
    }

    private void MigrateCfg(ref Config oldConfig)
    {
        // Migrate version 1 to 2

        if (oldConfig.Version == 1)
        {
            oldConfig.Version = 2;
            oldConfig.SeparatedAlliance.EspOption = oldConfig.AllianceOption;
            oldConfig.SeparatedAlliance.Enabled = oldConfig.SeparateAlliance;
            
            oldConfig.SeparatedFriends.EspOption = oldConfig.FriendOption;
            oldConfig.SeparatedFriends.Enabled = oldConfig.SeparateFriends;
            
            oldConfig.SeparatedParty.EspOption = oldConfig.PartyOption;
            oldConfig.SeparatedParty.Enabled = oldConfig.SeparateParty;
            
            oldConfig.SeparatedYourPlayer.EspOption = oldConfig.YourPlayerOption;
            oldConfig.SeparatedYourPlayer.Enabled = oldConfig.SeparateYourPlayer;
        }
    }

    public void SaveCurrentConfig()
    {
        pluginLog.Debug($"Saving config {cfg.ConfigName}");
        SavePluginConfig(cfg, cfg.ConfigName);
    }

    public bool LoadConfig(string configName)
    {
        pluginLog.Debug($"Loading config {configName}");
        SavePluginConfig(cfg, cfg.ConfigName);
        UpdateConfigs();
        var tempConfig = Load(configName);
        if (tempConfig != null)
        {
            this.cfg = tempConfig;
            MigrateCfg(ref cfg);
            Save();
            return true;
        }

        pluginLog.Error("Config was NOT loaded!");
        return false;
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(cfg);
    }

    public void UpdateConfigs()
    {
        configs = this.pluginInterface.ConfigDirectory.GetFiles().Select(x => x.Name.Substring(0, x.Name.Length - 5)).ToArray();
        if (selectedConfig >= configs.Length)
        {
            selectedConfig = 0;
        }
    }

    public void SaveNewDefaultConfig()
    {
        var count = 1;
        var newName = "new config";
        while (configs.Any(x => x == newName))
        {
            newName = $"new config {count}";
            count++;
        }

        var newConfig = new Config() { ConfigName = newName };

        SavePluginConfig(newConfig, newConfig.ConfigName);
    }

    public void DeleteConfig(string configName)
    {
        pluginLog.Debug($"Deleting config {configName}");
        var path = this.pluginInterface.ConfigDirectory.FullName + "/" + configName + ".json";
        var configFile = new FileInfo(path);
        if (configFile.Exists)
        {
            configFile.Delete();
        }

        UpdateConfigs();
    }

    private Config? Load(string configName)
    {
        var path = this.pluginInterface.ConfigDirectory.FullName + "/" + configName + ".json";
        FileInfo configFile = new FileInfo(path);
        pluginLog.Debug(configFile.FullName);
        return !configFile.Exists ? null : DeserializeConfig(File.ReadAllText(configFile.FullName));
    }

    internal void SavePluginConfig(Config? currentConfig, string configName)
    {
        if (currentConfig == null)
            return;
        var path = this.pluginInterface.ConfigDirectory.FullName + "/" + configName + ".json";
        this.Save(currentConfig, path);
        UpdateConfigs();
    }

    internal void Save(Config config, string path) =>
        this.WriteAllTextSafe(path, this.SerializeConfig(config));

    internal string SerializeConfig(Config config) => JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings()
    {
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.Objects
    });

    internal void WriteAllTextSafe(string path, string text)
    {
        var str = path + ".tmp";
        if (File.Exists(str))
            File.Delete(str);
        File.WriteAllText(str, text);
        File.Move(str, path, true);
    }

    internal static Config? DeserializeConfig(string data) => JsonConvert.DeserializeObject<Config>(data, new JsonSerializerSettings()
    {
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None
    });
}