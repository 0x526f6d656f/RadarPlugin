﻿using System;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using RadarPlugin.Enums;

namespace RadarPlugin;

public class RadarHelpers
{
    private readonly Configuration configInterface;
    private readonly ClientState clientState;
    private readonly Condition conditionInterface;

    public RadarHelpers(
        Configuration configInterface,
        ClientState clientState,
        Condition condition
    )
    {
        this.clientState = clientState;
        this.configInterface = configInterface;
        this.conditionInterface = condition;
    }


    public unsafe bool ShouldRender(GameObject obj)
    {
        if (configInterface.cfg.DebugMode)
        {
            return true;
        }

        if (clientState.LocalPlayer != null && obj.Address == clientState.LocalPlayer.Address)
        {
            return configInterface.cfg.ShowYOU;
        }

        if (configInterface.cfg.ShowBaDdObjects)
        {
            // TODO: Check if we need to swap this out with a seperte eureka and potd list
            if (this.IsSpecialZone() && (
                    UtilInfo.RenameList.ContainsKey(obj.DataId) ||
                    UtilInfo.DeepDungeonMobTypesMap.ContainsKey(obj.DataId)))
            {
                return true;
            }
        }

        var clientstructobj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)obj.Address;
        if (this.configInterface.cfg.ShowOnlyVisible && (clientstructobj->RenderFlags != 0))
        {
            return false;
        }

        if (String.IsNullOrWhiteSpace(obj.Name.TextValue) && !configInterface.cfg.ShowNameless) return false;
        switch (obj.ObjectKind)
        {
            case ObjectKind.Treasure:
                return configInterface.cfg.ShowLoot;
            case ObjectKind.Companion:
                return configInterface.cfg.ShowCompanion;
            case ObjectKind.Area:
                return configInterface.cfg.ShowAreaObjects;
            case ObjectKind.Aetheryte:
                return configInterface.cfg.ShowAetherytes;
            case ObjectKind.EventNpc:
                return configInterface.cfg.ShowEventNpc;
            case ObjectKind.EventObj:
                return configInterface.cfg.ShowEvents;
            case ObjectKind.None:
                break;
            case ObjectKind.Player:
                return configInterface.cfg.ShowPlayers;
            case ObjectKind.BattleNpc:
                if (!configInterface.cfg.ShowEnemies) return false;
                if (obj is not BattleNpc { BattleNpcKind: BattleNpcSubKind.Enemy } mob)
                    return false; // This should never trigger
                //if (!clientstructobj->GetIsTargetable()) continue;
                //if (String.IsNullOrWhiteSpace(mob.Name.TextValue)) continue;
                if (mob.IsDead) return false;
                if (UtilInfo.DataIdIgnoreList.Contains(mob.DataId) ||
                    configInterface.cfg.DataIdIgnoreList.Contains(mob.DataId)) return false;
                return true;
            case ObjectKind.GatheringPoint:
                return configInterface.cfg.ShowGatheringPoint;
            case ObjectKind.MountType:
                return configInterface.cfg.ShowMountType;
            case ObjectKind.Retainer:
                return configInterface.cfg.ShowRetainer;
            case ObjectKind.Housing:
                return configInterface.cfg.ShowHousing;
            case ObjectKind.Cutscene:
                return configInterface.cfg.ShowCutscene;
            case ObjectKind.CardStand:
                return configInterface.cfg.ShowCardStand;
        }

        return false;
    }

    public bool IsSpecialZone()
    {
        return UtilInfo.DeepDungeonMapIds.Contains(this.clientState.TerritoryType) || this.conditionInterface[ConditionFlag.InDeepDungeon];
    }
    
    /**
     * TODO: Refactor this to be done once per second instead of on each render.
     */
    public string GetText(GameObject obj)
    {
        var text = "";
        if (obj.DataId != 0 && UtilInfo.DeepDungeonMapIds.Contains(this.clientState.TerritoryType) &&
            UtilInfo.RenameList.ContainsKey(obj.DataId))
        {
            text = UtilInfo.RenameList[obj.DataId];
        }
        else if (string.IsNullOrWhiteSpace(obj.Name.TextValue))
        {
            text = "''";
        }
        else
        {
            text = obj.Name.TextValue;
        }

        return configInterface.cfg.DebugMode ? $"{obj.Name}, {obj.DataId}, {obj.ObjectKind}" : $"{text}";
    }

    public uint GetColor(GameObject gameObject)
    {
        // Override over all
        if (configInterface.cfg.ColorOverride.ContainsKey(gameObject.DataId))
        {
            return configInterface.cfg.ColorOverride[gameObject.DataId];
        }

        // If Deep Dungeon
        if (configInterface.cfg.ShowBaDdObjects && UtilInfo.DeepDungeonMobTypesMap.ContainsKey(gameObject.DataId) &&
            IsSpecialZone() &&
            gameObject.ObjectKind != ObjectKind.Player)
        {
            switch (UtilInfo.DeepDungeonMobTypesMap[gameObject.DataId])
            {
                case DeepDungeonMobTypes.SpecialUndead:
                    return configInterface.cfg.DeepDungeonOptions.SpecialUndeadOption.ColorU;
                case DeepDungeonMobTypes.Auspice:
                    return configInterface.cfg.DeepDungeonOptions.AuspiceOption.ColorU;
                case DeepDungeonMobTypes.EasyMobs:
                    return configInterface.cfg.DeepDungeonOptions.EasyMobOption.ColorU;
                case DeepDungeonMobTypes.Traps:
                    return configInterface.cfg.DeepDungeonOptions.TrapOption.ColorU;
                case DeepDungeonMobTypes.Return:
                    return configInterface.cfg.DeepDungeonOptions.ReturnOption.ColorU;
                case DeepDungeonMobTypes.Passage:
                    return configInterface.cfg.DeepDungeonOptions.PassageOption.ColorU;
                case DeepDungeonMobTypes.GoldChest:
                    return configInterface.cfg.DeepDungeonOptions.GoldChestOption.ColorU;
                case DeepDungeonMobTypes.SilverChest:
                    return configInterface.cfg.DeepDungeonOptions.SilverChestOption.ColorU;
                case DeepDungeonMobTypes.BronzeChest:
                    return configInterface.cfg.DeepDungeonOptions.BronzeChestOption.ColorU;
                case DeepDungeonMobTypes.AccursedHoard:
                    return configInterface.cfg.DeepDungeonOptions.AccursedHoardOption.ColorU;
                case DeepDungeonMobTypes.Mimic:
                    return configInterface.cfg.DeepDungeonOptions.MimicOption.ColorU;
            }
            // If no specific config interface, it defaults
        }

        // Finally if nothing else
        switch (gameObject.ObjectKind)
        {
            case ObjectKind.Player:
                return configInterface.cfg.PlayerOption.ColorU;
            case ObjectKind.BattleNpc:
                return configInterface.cfg.NpcOption.ColorU;
            case ObjectKind.EventNpc:
                return configInterface.cfg.EventNpcOption.ColorU;
            case ObjectKind.Treasure:
                return configInterface.cfg.TreasureOption.ColorU;
            case ObjectKind.Aetheryte:
                return configInterface.cfg.AetheryteOption.ColorU;
            case ObjectKind.GatheringPoint:
                return configInterface.cfg.GatheringPointOption.ColorU;
            case ObjectKind.EventObj:
                return configInterface.cfg.EventObjOption.ColorU;
            case ObjectKind.MountType:
                return configInterface.cfg.MountOption.ColorU;
            case ObjectKind.Companion:
                return configInterface.cfg.CompanionOption.ColorU;
            case ObjectKind.Retainer:
                return configInterface.cfg.RetainerOption.ColorU;
            case ObjectKind.Area:
                return configInterface.cfg.AreaOption.ColorU;
            case ObjectKind.Housing:
                return configInterface.cfg.HousingOption.ColorU;
            case ObjectKind.Cutscene:
                return configInterface.cfg.CutsceneOption.ColorU;
            case ObjectKind.CardStand:
                return configInterface.cfg.CardStandOption.ColorU;
            case ObjectKind.None:
            default:
                PluginLog.Error($"Game Object Got Invalid Color: {gameObject.DataId}");
                return UtilInfo.Yellow;
        }
    }

    public Configuration.ESPOption GetParams(GameObject areaObject)
    {
        // If Deep Dungeon
        if (configInterface.cfg.ShowBaDdObjects && UtilInfo.DeepDungeonMobTypesMap.ContainsKey(areaObject.DataId) &&
            UtilInfo.DeepDungeonMapIds.Contains(this.clientState.TerritoryType) &&
            areaObject.ObjectKind != ObjectKind.Player)
        {
            switch (UtilInfo.DeepDungeonMobTypesMap[areaObject.DataId])
            {
                case DeepDungeonMobTypes.SpecialUndead:
                    return configInterface.cfg.DeepDungeonOptions.SpecialUndeadOption;
                case DeepDungeonMobTypes.Auspice:
                    return configInterface.cfg.DeepDungeonOptions.AuspiceOption;
                case DeepDungeonMobTypes.EasyMobs:
                    return configInterface.cfg.DeepDungeonOptions.EasyMobOption;
                case DeepDungeonMobTypes.Traps:
                    return configInterface.cfg.DeepDungeonOptions.TrapOption;
                case DeepDungeonMobTypes.Return:
                    return configInterface.cfg.DeepDungeonOptions.ReturnOption;
                case DeepDungeonMobTypes.Passage:
                    return configInterface.cfg.DeepDungeonOptions.PassageOption;
                case DeepDungeonMobTypes.GoldChest:
                    return configInterface.cfg.DeepDungeonOptions.GoldChestOption;
                case DeepDungeonMobTypes.SilverChest:
                    return configInterface.cfg.DeepDungeonOptions.SilverChestOption;
                case DeepDungeonMobTypes.BronzeChest:
                    return configInterface.cfg.DeepDungeonOptions.BronzeChestOption;
                case DeepDungeonMobTypes.AccursedHoard:
                    return configInterface.cfg.DeepDungeonOptions.AccursedHoardOption;
                case DeepDungeonMobTypes.Mimic:
                    return configInterface.cfg.DeepDungeonOptions.MimicOption;
            }
            // If no specific config interface, it defaults
        }

        switch (areaObject.ObjectKind)
        {
            case ObjectKind.Player:
                return configInterface.cfg.PlayerOption;
            case ObjectKind.BattleNpc:
                return configInterface.cfg.NpcOption;
            case ObjectKind.EventNpc:
                return configInterface.cfg.EventNpcOption;
            case ObjectKind.Treasure:
                return configInterface.cfg.TreasureOption;
            case ObjectKind.Aetheryte:
                return configInterface.cfg.AetheryteOption;
            case ObjectKind.GatheringPoint:
                return configInterface.cfg.GatheringPointOption;
            case ObjectKind.EventObj:
                return configInterface.cfg.EventObjOption;
            case ObjectKind.MountType:
                return configInterface.cfg.MountOption;
            case ObjectKind.Companion:
                return configInterface.cfg.CompanionOption;
            case ObjectKind.Retainer:
                return configInterface.cfg.RetainerOption;
            case ObjectKind.Area:
                return configInterface.cfg.AreaOption;
            case ObjectKind.Housing:
                return configInterface.cfg.HousingOption;
            case ObjectKind.Cutscene:
                return configInterface.cfg.CutsceneOption;
            case ObjectKind.CardStand:
                return configInterface.cfg.CardStandOption;
            case ObjectKind.None:
            default:
                return configInterface.cfg.TreasureOption;
        }
    }
}