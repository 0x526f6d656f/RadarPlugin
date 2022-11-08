﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace RadarPlugin;

[Serializable]
public class Configuration
{
    public class ESPOption
    {
        public bool ShowName = true;
        public bool ShowDot = true;
        public uint Color = uint.MinValue;
    }
    public class NpcOption : ESPOption
    {
        public bool ShowHealthBar = true;
        public bool ShowHealthValue = true;
    }
    public class PlayerOption : NpcOption
    {
        public bool ShowFC = false;
    }
    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public bool ShowBaDdObjects { get; set; } = true;
        public bool ShowLoot { get; set; } = false;
        public bool DebugMode { get; set; } = false;
        public bool ShowPlayers { get; set; } = false;
        public bool ShowEnemies { get; set; } = true;
        public bool ShowEvents { get; set; } = false;
        public bool ShowCompanion { get; set; } = false;
        public bool ShowEventNpc { get; set; } = false;
        public bool ShowAreaObjects { get; set; } = false;
        public bool ShowAetherytes { get; set; } = false;
        public NpcOption NpcOption { get; set; } = new();
        public PlayerOption PlayerOption { get; set; } = new();
        public HashSet<uint> DataIdIgnoreList { get; set; } = new HashSet<uint>();
    }
    public Config cfg;

    [NonSerialized] private DalamudPluginInterface pluginInterface;

    public Configuration(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        cfg = this.pluginInterface.GetPluginConfig() as Config ?? new Config();
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(cfg);
    }
}