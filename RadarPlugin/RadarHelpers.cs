﻿using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using Lumina.Excel.GeneratedSheets;
using RadarPlugin.Enums;

namespace RadarPlugin;

public class RadarHelpers
{
    private readonly Configuration configInterface;
    private readonly IClientState clientState;
    private readonly ICondition conditionInterface;
    private Dictionary<uint, float> distanceDictionary;
    private readonly IDataManager dataManager;
    public readonly Dictionary<uint, byte> RankDictionary = new();

    public RadarHelpers(
        Configuration configInterface,
        IClientState clientState,
        ICondition condition,
        IDataManager dataManager
    )
    {
        this.clientState = clientState;
        this.configInterface = configInterface;
        this.conditionInterface = condition;
        this.distanceDictionary = new Dictionary<uint, float>();
        this.dataManager = dataManager;

        var excelBnpcs = this.dataManager.GetExcelSheet<BNpcBase>();
        if (excelBnpcs != null)
        {
            RankDictionary = excelBnpcs.ToDictionary(x => x.RowId, x => x.Rank);
        }
    }


    public unsafe bool ShouldRender(GameObject obj)
    {
        // Objest valid check
        if (!obj.IsValid()) return false;
        //if (obj.DataId == GameObject.InvalidGameObjectId) return false;

        // Object within ignore lists
        if (UtilInfo.DataIdIgnoreList.Contains(obj.DataId) || configInterface.cfg.DataIdIgnoreList.Contains(obj.DataId)) return false;

        // Object visible & config check
        var clientstructobj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)obj.Address;
        if (configInterface.cfg.ShowOnlyVisible && (clientstructobj->RenderFlags != 0))// || !clientstructobj->GetIsTargetable()))
        {
            // If override is not enabled, return false, otherwise check if the object kind is a player, and if not, return false still. 
            if (!configInterface.cfg.OverrideShowInvisiblePlayerCharacters) return false;
            if (obj.ObjectKind != ObjectKind.Player) return false;
        }
        // Distance check

        // Eureka DD STQ
        if (configInterface.cfg.ShowBaDdObjects && IsSpecialZone())
        {
            // UtilInfo.RenameList.ContainsKey(obj.DataId) || UtilInfo.DeepDungeonMobTypesMap.ContainsKey(obj.DataId)))
            if (UtilInfo.DeepDungeonMobTypesMap.ContainsKey(obj.DataId)) return true;
            if (string.IsNullOrWhiteSpace(obj.Name.TextValue) && !configInterface.cfg.ShowNameless) return false;
            if (obj.ObjectKind != ObjectKind.BattleNpc || obj is not BattleNpc { BattleNpcKind: BattleNpcSubKind.Enemy } mob) return true;
            if (!configInterface.cfg.DeepDungeonOptions.DefaultEnemyOption.Enabled) return false;
            return !mob.IsDead;
        }


        if (obj is BattleChara mobNpc)
        {
            //if (!clientstructobj->GetIsTargetable()) continue;
            //if (String.IsNullOrWhiteSpace(mob.Name.TextValue)) continue;
            if (string.IsNullOrWhiteSpace(obj.Name.TextValue) && !configInterface.cfg.ShowNameless) return false;
            if (mobNpc.IsDead) return false;
        }

        return true;
    }

    public void ResetDistance()
    {
        this.distanceDictionary = new Dictionary<uint, float>();
    }

    public float GetDistanceFromPlayer(GameObject obj)
    {
        if (distanceDictionary.TryGetValue(obj.ObjectId, out var value))
        {
            return value;
        }

        var distance = obj.Position.Distance2D(clientState.LocalPlayer!.Position);
        distanceDictionary[obj.ObjectId] = distance;
        return distance;
    }

    public bool IsSpecialZone()
    {
        return UtilInfo.DeepDungeonMapIds.Contains(this.clientState.TerritoryType) ||
               this.conditionInterface[ConditionFlag.InDeepDungeon];
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
        
        return configInterface.cfg.DebugText ? $"{obj.Name}, {obj.DataId}, {obj.ObjectKind}" : $"{text}";
    }

    public uint? GetColorOverride(GameObject gameObject)
    {
        // Override over all
        if (configInterface.cfg.ColorOverride.TryGetValue(gameObject.DataId, out var colorOverride))
        {
            return colorOverride;
        }

        return null;
    }

    public Configuration.ESPOption GetParams(GameObject areaObject)
    {
        // If Deep Dungeon
        if (configInterface.cfg.ShowBaDdObjects && IsSpecialZone())
        {
            if (UtilInfo.DeepDungeonMobTypesMap.TryGetValue(areaObject.DataId, out var value))
            {
                switch (value)
                {
                    case DeepDungeonMobTypes.Default:
                        return configInterface.cfg.DeepDungeonOptions.DefaultEnemyOption;
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
                    default:
                        return configInterface.cfg.DeepDungeonOptions.DefaultEnemyOption;
                }
            }

            if (areaObject.ObjectKind == ObjectKind.BattleNpc && areaObject is BattleNpc { BattleNpcKind: BattleNpcSubKind.Enemy } mob)
            {
                return configInterface.cfg.DeepDungeonOptions.DefaultEnemyOption;
            }
        }

        switch (areaObject.ObjectKind)
        {
            case ObjectKind.Player:
                if (areaObject is PlayerCharacter chara)
                {
                    // Is the object is YOU
                    if (configInterface.cfg.SeparatedYourPlayer.Enabled && clientState.LocalPlayer != null && chara.Address == clientState.LocalPlayer.Address)
                    {
                        return configInterface.cfg.SeparatedYourPlayer.EspOption;
                    }

                    // If is friend
                    if (configInterface.cfg.SeparatedFriends.Enabled && chara.StatusFlags.HasFlag(StatusFlags.Friend)) //0x80
                    {
                        return configInterface.cfg.SeparatedFriends.EspOption;
                    }

                    // Is in party
                    if (configInterface.cfg.SeparatedParty.Enabled && chara.StatusFlags.HasFlag(StatusFlags.PartyMember)) //0x20
                    {
                        return configInterface.cfg.SeparatedParty.EspOption;
                    }

                    // If in alliance
                    if (configInterface.cfg.SeparatedAlliance.Enabled && chara.StatusFlags.HasFlag(StatusFlags.AllianceMember)) // 0x40
                    {
                        return configInterface.cfg.SeparatedAlliance.EspOption;
                    }
                }

                return configInterface.cfg.PlayerOption;
            case ObjectKind.Companion:
                return configInterface.cfg.CompanionOption;
            case ObjectKind.BattleNpc:
                if (areaObject is not BattleChara bnpc)
                {
                    return configInterface.cfg.NpcOption;
                }

                if (configInterface.cfg.SeparatedRankOne.Enabled || configInterface.cfg.SeparatedRankTwoAndSix.Enabled)
                {
                    if (RankDictionary.TryGetValue(bnpc.DataId, out var value))
                    {
                        switch (value)
                        {
                            case 1:
                                if (configInterface.cfg.SeparatedRankOne.Enabled)
                                {
                                    return configInterface.cfg.SeparatedRankOne.EspOption;
                                }
                                break;
                            case 2: case 6:
                                if (configInterface.cfg.SeparatedRankTwoAndSix.Enabled)
                                {
                                    return configInterface.cfg.SeparatedRankTwoAndSix.EspOption;
                                }
                                break;
                        }
                    }
                }

                if (bnpc is BattleNpc { BattleNpcKind: BattleNpcSubKind.Pet or BattleNpcSubKind.Chocobo })
                {
                    return configInterface.cfg.CompanionOption;
                }

                if (configInterface.cfg.LevelRendering.LevelRenderingEnabled)
                {
                    if (clientState.LocalPlayer!.Level - (byte)configInterface.cfg.LevelRendering.RelativeLevelsBelow > bnpc.Level)
                    {
                        return configInterface.cfg.LevelRendering.LevelRenderEspOption;
                    }
                }
                
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
            case ObjectKind.Ornament:
                return configInterface.cfg.OrnamentOption;
            case ObjectKind.None:
            default:
                return configInterface.cfg.NpcOption;
        }
    }
}