﻿using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Gui;
using Dalamud.Plugin;
using ImGuiNET;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;

namespace RadarPlugin;

public class RadarLogic : IDisposable
{
    private const float PI = 3.14159265359f;
    private readonly DalamudPluginInterface pluginInterface;
    private Configuration configInterface;
    private readonly Condition conditionInterface;
    private Task backgroundLoop;
    private bool keepRunning;
    private readonly ObjectTable objectTable;
    private List<(GameObject, uint, string)> areaObjects; // Game object, color, string
    private readonly ClientState clientState;
    private readonly GameGui gameGui;
    private readonly Helpers helpers;


    public RadarLogic(DalamudPluginInterface pluginInterface, Configuration configuration, ObjectTable objectTable,
        Condition condition, ClientState clientState, GameGui gameGui, Helpers helpers)
    {
        // Creates Dependencies
        this.objectTable = objectTable;
        this.pluginInterface = pluginInterface;
        this.configInterface = configuration;
        this.conditionInterface = condition;
        this.gameGui = gameGui;
        this.helpers = helpers;

        // Loads plugin
        PluginLog.Debug("Radar Loaded");
        keepRunning = true;
        // TODO: In the future adjust this
        areaObjects = new List<(GameObject, uint, string)>();

        this.clientState = clientState;
        this.pluginInterface.UiBuilder.Draw += OnTick;
        backgroundLoop = Task.Run(BackgroundLoop);

        this.clientState.TerritoryChanged += CleanupZoneTerritoryWrapper;
        this.clientState.Logout += CleanupZoneLogWrapper;
        this.clientState.Login += CleanupZoneLogWrapper;
    }

    private void OnTick()
    {
        if (!configInterface.cfg.Enabled) return;
        if (objectTable.Length == 0) return;
        if (CheckDraw()) return;
        DrawRadar();
    }

    private void DrawRadar()
    {
        if (!Monitor.TryEnter(areaObjects))
        {
            PluginLog.Error("Try Enter Failed. This is not an error");
            return;
        }

        foreach (var areaObject in areaObjects)
        {
            DrawEsp(areaObject.Item1, areaObject.Item2, areaObject.Item3);
        }

        Monitor.Exit(areaObjects);
    }

    /**
     * Returns true if you should not draww
     */
    private bool CheckDraw()
    {

        return conditionInterface[ConditionFlag.LoggingOut] || conditionInterface[ConditionFlag.BetweenAreas] ||
               conditionInterface[ConditionFlag.BetweenAreas51] || !configInterface.cfg.Enabled ||
               clientState.LocalContentId == 0 || clientState.LocalPlayer == null;
    }

    private void DrawEsp(GameObject gameObject, uint color, string name)
    {
        var visibleOnScreen = gameGui.WorldToScreen(gameObject.Position, out var onScreenPosition);
        switch (gameObject)
        {
            // Mobs
            case BattleNpc mob:
                var npcOpt = configInterface.cfg.NpcOption;
                if (visibleOnScreen)
                {
                    if (npcOpt.ShowHealthBar)
                    {
                        DrawHealthCircle(onScreenPosition, mob.MaxHp, mob.CurrentHp, color);
                    }

                    if (npcOpt.ShowHealthValue)
                    {
                        DrawHealthValue(onScreenPosition, mob.MaxHp, mob.CurrentHp, color);
                    }

                    if (npcOpt.ShowName)
                    {
                        DrawName(onScreenPosition, name, color);
                    }

                    if (npcOpt.ShowDot)
                    {
                        DrawDot(onScreenPosition, npcOpt.DotSize, color);
                    }
                }

                if (npcOpt.ShowAggroCircle)
                {
                    if (!npcOpt.ShowAggroCircleInCombat && (mob.StatusFlags & StatusFlags.WeaponOut) != 0) return;
                    DrawAggroRadius(gameObject.Position, 10 + gameObject.HitboxRadius, gameObject.Rotation,
                        uint.MaxValue);
                }

                break;
            // Players
            case PlayerCharacter chara:
                var playerOpt = configInterface.cfg.PlayerOption;
                if (!visibleOnScreen) break;
                //var hp = chara.CurrentHp / chara.MaxHp;
                if (playerOpt.ShowHealthBar)
                {
                    DrawHealthCircle(onScreenPosition, chara.MaxHp, chara.CurrentHp, color);
                }

                if (playerOpt.ShowHealthValue)
                {
                    DrawHealthValue(onScreenPosition, chara.MaxHp, chara.CurrentHp, color);
                }

                if (playerOpt.ShowName)
                {
                    DrawName(onScreenPosition, name, color);
                }

                if (playerOpt.ShowDot)
                {
                    DrawDot(onScreenPosition, playerOpt.DotSize, color);
                }

                break;
            // Event Objects
            case EventObj chara:
            // Npcs
            case Npc npc:
            // Objects
            default:
                if (!visibleOnScreen) break;
                var objectOption = configInterface.cfg.ObjectOption;
                if (objectOption.ShowName)
                {
                    DrawName(onScreenPosition, name, color);
                }

                if (objectOption.ShowDot)
                {
                    DrawDot(onScreenPosition, objectOption.DotSize, color);
                }

                break;
        }
    }

    private void DrawDot(Vector2 position, float radius, uint npcOptColor)
    {
        ImGui.GetForegroundDrawList().AddCircleFilled(position, radius, npcOptColor, 100);
    }

    private void DrawHealthValue(Vector2 position, uint maxHp, uint currHp, uint playerOptColor)
    {
        var healthText = ((int)(((double)currHp / maxHp) * 100)).ToString();
        var healthTextSize = ImGui.CalcTextSize(healthText);
        ImGui.GetForegroundDrawList().AddText(
            new Vector2((position.X - healthTextSize.X / 2.0f), (position.Y - healthTextSize.Y / 2.0f)),
            playerOptColor,
            healthText);
    }

    private void DrawName(Vector2 position, string tagText, uint objectOptionColor)
    {
        var tagTextSize = ImGui.CalcTextSize(tagText);
        ImGui.GetForegroundDrawList().AddText(
            new Vector2(position.X - tagTextSize.X / 2f, position.Y + tagTextSize.Y / 2f),
            objectOptionColor,
            tagText);
    }


    private void DrawHealthCircle(Vector2 position, uint maxHp, uint currHp, uint playerOptColor)
    {
        const float radius = 13f;

        var v1 = (float)currHp / (float)maxHp;
        var aMax = PI * 2.0f;
        var difference = v1 - 1.0f;
        ImGui.GetForegroundDrawList().PathArcTo(position, radius,
            (-(aMax / 4.0f)) + (aMax / maxHp) * (maxHp - currHp), aMax - (aMax / 4.0f), 200 - 1);
        ImGui.GetForegroundDrawList().PathStroke(playerOptColor, ImDrawFlags.None, 2.0f);
    }

    private void DrawAggroRadius(Vector3 position, float radius, float rotation, uint objectOptionColor)
    {
        var opacity = configInterface.cfg.AggroRadiusOptions.CircleOpacity;
        rotation += MathF.PI / 4;
        var numSegments = 200;
        var segmentAngle = 2 * MathF.PI / numSegments;
        var points = new Vector2[numSegments];
        var onScreens = new bool[numSegments];
        var seg = 2 * MathF.PI / numSegments;
        var rot = rotation + 0 * MathF.PI;

        var originPointOnScreen = gameGui.WorldToScreen(
            new(position.X + radius * MathF.Sin(rot),
                position.Y,
                position.Z + radius * MathF.Cos(rot)),
            out var originPoint);

        for (int i = 0; i < numSegments; i++)
        {
            var a = rot - i * segmentAngle;
            var onScreen = gameGui.WorldToScreen(
                new(position.X + radius * MathF.Sin(a),
                    position.Y,
                    position.Z + radius * MathF.Cos(a)),
                out var p);
            points[i] = p;
            onScreens[i] = onScreen;
            if (onScreen)
            {
                ImGui.GetForegroundDrawList().PathLineTo(p);
            }

            switch (i)
            {
                case 50:
                    ImGui.GetForegroundDrawList().PathStroke(configInterface.cfg.AggroRadiusOptions.FrontColor & opacity, ImDrawFlags.RoundCornersAll, 4f);
                    // this forloop should only happen when cone shows (always right now)
                    for (int j = 0; j <= 50; j++)
                    {
                        ImGui.GetForegroundDrawList().PathLineTo(points[j]);
                    }
                    
                    var centeOnScreen = gameGui.WorldToScreen(
                        position,
                        out var centerPosition);
                    if (centeOnScreen)
                    {
                        ImGui.GetForegroundDrawList().PathLineTo(centerPosition);
                    }
                    else
                    {
                        ImGui.GetForegroundDrawList().PathClear();
                    }

                    ImGui.GetForegroundDrawList().PathFillConvex(configInterface.cfg.AggroRadiusOptions.FrontConeColor & configInterface.cfg.AggroRadiusOptions.FrontConeOpacity);
                    ImGui.GetForegroundDrawList().PathLineTo(p);
                    break;
                case 100:
                    ImGui.GetForegroundDrawList()
                        .PathStroke(configInterface.cfg.AggroRadiusOptions.RightSideColor & opacity, ImDrawFlags.RoundCornersAll, 2f);
                    ImGui.GetForegroundDrawList().PathLineTo(p);
                    break;
                case 150:
                    ImGui.GetForegroundDrawList().PathStroke(configInterface.cfg.AggroRadiusOptions.RearColor & opacity, ImDrawFlags.RoundCornersAll, 2f);
                    ImGui.GetForegroundDrawList().PathLineTo(p);
                    break;
                case 199:
                    if (originPointOnScreen)
                    {
                        ImGui.GetForegroundDrawList().PathLineTo(originPoint);
                    }

                    ImGui.GetForegroundDrawList()
                        .PathStroke(configInterface.cfg.AggroRadiusOptions.LeftSideColor & opacity, ImDrawFlags.RoundCornersAll, 2f);
                    break;
            }
        }

        ImGui.GetForegroundDrawList().PathClear();
    }

    private void BackgroundLoop()
    {
        while (keepRunning)
        {
            if (configInterface.cfg.Enabled)
            {
                if (CheckDraw())
                {
#if DEBUG
                    PluginLog.Verbose("Did not update mob info due to check fail.");
#endif
                }
                else
                {
                    var time = DateTime.Now;
                    UpdateMobInfo();
#if DEBUG
                    PluginLog.Verbose($"Refreshed Mob Info in {(DateTime.Now - time).TotalMilliseconds} ms.");
#endif
                }
            }

            Thread.Sleep(1000);
        }
    }

    private unsafe void UpdateMobInfo()
    {
        var nearbyMobs = new List<(GameObject, uint, string)>();
        foreach (var obj in objectTable)
        {
            if (!obj.IsValid()) continue;
            if (configInterface.cfg.DebugMode)
            {
                nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                continue;
            }
            
            if (configInterface.cfg.ShowBaDdObjects)
            {
                // TODO: Check if we need to swap this out with a seperte eureka and potd list
                if (UtilInfo.RenameList.ContainsKey(obj.DataId) || UtilInfo.DeepDungeonMobTypesMap.ContainsKey(obj.DataId))
                {
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    continue;
                }
            }
            
            if (this.configInterface.cfg.ShowOnlyVisible &&
                ((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)obj.Address)->RenderFlags != 0)
            {
                continue;
            }

            if (String.IsNullOrWhiteSpace(obj.Name.TextValue) && !configInterface.cfg.ShowNameless) continue;

            switch (obj.ObjectKind)
            {
                case ObjectKind.Treasure:
                    if (!configInterface.cfg.ShowLoot) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.Companion:
                    if (!configInterface.cfg.ShowCompanion) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.Area:
                    if (!configInterface.cfg.ShowAreaObjects) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.Aetheryte:
                    if (!configInterface.cfg.ShowAetherytes) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.EventNpc:
                    if (!configInterface.cfg.ShowEventNpc) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.EventObj:
                    if (!configInterface.cfg.ShowEvents) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.None:
                    break;
                case ObjectKind.Player:
                    if (!configInterface.cfg.ShowPlayers) continue;
                    //if (obj is not PlayerCharacter chara) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.BattleNpc:
                    if (obj is not BattleNpc mob) continue;
                    if (!configInterface.cfg.ShowEnemies) continue;
                    //if (String.IsNullOrWhiteSpace(mob.Name.TextValue)) continue;
                    if (mob.BattleNpcKind != BattleNpcSubKind.Enemy) continue;
                    if (mob.IsDead) continue;
                    if (UtilInfo.DataIdIgnoreList.Contains(mob.DataId) ||
                        configInterface.cfg.DataIdIgnoreList.Contains(mob.DataId)) continue;
                    nearbyMobs.Add((obj, helpers.GetColor(obj), helpers.GetText(obj)));
                    break;
                case ObjectKind.GatheringPoint:
                    break;
                case ObjectKind.MountType:
                    break;
                case ObjectKind.Retainer:
                    break;
                case ObjectKind.Housing:
                    break;
                case ObjectKind.Cutscene:
                    break;
                case ObjectKind.CardStand:
                    break;
                default:
                    break;
            }
        }

        Monitor.Enter(areaObjects);
        areaObjects.Clear();
        areaObjects.AddRange(nearbyMobs);
        Monitor.Exit(areaObjects);
    }


    private void CleanupZoneTerritoryWrapper(object? _, ushort __)
    {
        CleanupZone();
    }

    private void CleanupZone()
    {
        PluginLog.Verbose("Clearing because of condition met.");
        Monitor.Enter(areaObjects);
        areaObjects.Clear();
        Monitor.Exit(areaObjects);
    }

    private void CleanupZoneLogWrapper(object? sender, EventArgs e)
    {
        CleanupZone();
    }

    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= OnTick;
        clientState.TerritoryChanged -= CleanupZoneTerritoryWrapper;
        clientState.Logout -= CleanupZoneLogWrapper;
        clientState.Login -= CleanupZoneLogWrapper;
        keepRunning = false;
        while (!backgroundLoop.IsCompleted) ;
        Monitor.Enter(areaObjects);
        Monitor.Exit(areaObjects);
        PluginLog.Information("Radar Unloaded");
    }
}