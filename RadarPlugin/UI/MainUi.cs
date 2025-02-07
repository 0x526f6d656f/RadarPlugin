﻿using System;
using ImGuiNET;
using System.Numerics;
using Dalamud.Game.ClientState;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using RadarPlugin.Enums;

namespace RadarPlugin.UI;

public class MainUi : IDisposable
{
    private Configuration configInterface;
    private readonly DalamudPluginInterface dalamudPluginInterface;
    private readonly LocalMobsUi localMobsUi;
    private bool mainWindowVisible = false;
    private readonly IClientState clientState;
    private readonly RadarHelpers radarHelper;
    private const int ChildHeight = 280;
    private readonly TypeConfigurator typeConfigurator;

    public MainUi(DalamudPluginInterface dalamudPluginInterface, Configuration configInterface, LocalMobsUi localMobsUi,
        IClientState clientState, RadarHelpers radarHelpers, TypeConfigurator typeConfigurator)
    {
        this.clientState = clientState;
        this.localMobsUi = localMobsUi;
        this.configInterface = configInterface;
        this.dalamudPluginInterface = dalamudPluginInterface;
        this.radarHelper = radarHelpers;
        this.typeConfigurator = typeConfigurator;
        this.dalamudPluginInterface.UiBuilder.Draw += Draw;
        this.dalamudPluginInterface.UiBuilder.OpenConfigUi += OpenUi;
    }

    public void Dispose()
    {
        this.dalamudPluginInterface.UiBuilder.Draw -= Draw;
        this.dalamudPluginInterface.UiBuilder.OpenConfigUi -= OpenUi;
    }

    public void OpenUi()
    {
        mainWindowVisible = true;
    }

    private void Draw()
    {
        DrawMainWindow();
    }

    private void DrawMainWindow()
    {
        if (!mainWindowVisible)
        {
            return;
        }

        var size = new Vector2(480, 440);
        ImGui.SetNextWindowSize(size, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(size, new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("Radar Plugin", ref mainWindowVisible))
        {
            UiHelpers.DrawTabs("radar-settings-tabs",
                ("General", UtilInfo.White, DrawGeneralSettings),
                ("Overview", UtilInfo.Red, DrawVisibilitySettings),
                ("Additional Features", UtilInfo.White, ShowMiscSettings),
                ("Utility", UtilInfo.White, DrawUtilityTab),
                ("Config", UtilInfo.White, DrawConfigTab)
            );
        }

        ImGui.End();
    }

    private void DrawConfigTab()
    {
        var shouldSave = false;
        var configName = configInterface.cfg.ConfigName;
        ImGui.Text("Current Config Name:");
        shouldSave |= ImGui.InputText("", ref configInterface.cfg.ConfigName, 50);

        if (ImGui.Button("Save Current Config"))
        {
            this.configInterface.SaveCurrentConfig();
        }

        ImGui.Text("Saved Configurations:");
        var selectedConfig = configInterface.selectedConfig;
        if (ImGui.Combo("##selectedconfigcombobox", ref selectedConfig, configInterface.configs,
                configInterface.configs.Length))
        {
            if (selectedConfig < configInterface.configs.Length)
            {
                configInterface.selectedConfig = selectedConfig;
            }
        }

        if (ImGui.Button("Load Selected Config"))
        {
            this.configInterface.LoadConfig(configInterface.configs[selectedConfig]);
        }

        UiHelpers.HoverTooltip("This will save your current config when loading!");
        ImGui.SameLine();
        if (ImGui.Button("Delete Selected Config"))
        {
            ImGui.OpenPopup("DeleteConfigPopup");
        }

        if (ImGui.Button("New BLANK Config"))
        {
            this.configInterface.SaveNewDefaultConfig();
        }

        ImGui.PushStyleVar(ImGuiStyleVar.PopupBorderSize, 1f);
        ImGui.PushStyleColor(ImGuiCol.Border, ImGui.GetColorU32(ImGuiCol.TabActive));
        if (ImGui.BeginPopup("DeleteConfigPopup"))
        {
            ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(UtilInfo.White), $"Do you really want to delete the config: \"{configInterface.configs[selectedConfig]}\"?");
            if (ImGui.Button("Yes"))
            {
                this.configInterface.DeleteConfig(configInterface.configs[selectedConfig]);
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();
            ImGui.PushStyleColor(ImGuiCol.Text, UtilInfo.Red);
            if (ImGui.Button("No"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.PopStyleColor();
            ImGui.EndPopup();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor();

        if (shouldSave) configInterface.Save();
    }

    private void DrawUtilityTab()
    {
        var shouldSave = false;

        ImGui.Separator();

        if (ImGui.Button("Load Current Objects Menu"))
        {
            PluginLog.Debug("Pulling Area Objects");
            this.localMobsUi.DrawLocalMobsUi();
        }

        ImGui.Separator();

        ImGui.Text($"Current Map ID: {clientState.TerritoryType}");
        ImGui.Text($"In special zone (dd/eureka?): {radarHelper.IsSpecialZone()}");


        if (configInterface.cfg.DebugMode)
        {
            // Todo: Debug swap text bools
        }

        if (shouldSave) configInterface.Save();
    }

    private void DrawGeneralSettings()
    {
        bool shouldSave = false;
        ImGui.TextColored(new Vector4(0xff, 0xff, 0x00, 0xff),
            "This is made by KangasZ for use in FFXIV.");

        shouldSave |= ImGui.Checkbox("Enabled", ref configInterface.cfg.Enabled);

        shouldSave |= ImGui.Checkbox("Eureka/Deep Dungeons Support", ref configInterface.cfg.ShowBaDdObjects);
        UiHelpers.LabeledHelpMarker("", "This focuses on giving support to eureka and deep dungeons.\n" +
                                        "Will display things such as portals, chests, and traps.");

        shouldSave |= ImGui.Checkbox("Use Background Draw List", ref configInterface.cfg.UseBackgroundDrawList);

        UiHelpers.LabeledHelpMarker("", "This feature will use a background draw list from ImGui to render the 3d radar.\n" +
                                        "It will be under any other Dalamud plugin. This is the original behavior.\n" +
                                        "There should be practically no difference between this and normal operations");


        shouldSave |= UiHelpers.DrawDotSizeSlider(ref configInterface.cfg.DotSize, "default-dot-size");

        shouldSave |= UiHelpers.DrawCheckbox("Debug Text", ref configInterface.cfg.DebugText,
            "Replaces name with a debug string of:\n'Name, DataId, MobType'\n/radar showdebug");

        if (ImGui.CollapsingHeader("Filtering Rules"))
        {
            shouldSave |= UiHelpers.DrawCheckbox("Maximum Distance", ref configInterface.cfg.UseMaxDistance, "Max distance for the esp");
            if (configInterface.cfg.UseMaxDistance)
            {
                ImGui.SameLine();
                shouldSave |= UiHelpers.DrawFloatWithResetSlider(ref configInterface.cfg.MaxDistance, "", "default-max-distance-size", 1f, 2000f,
                    UtilInfo.DefaultMaxEspDistance, "%.0fm");
            }


            shouldSave |= UiHelpers.DrawCheckbox("Only Show Visible Entities", ref configInterface.cfg.ShowOnlyVisible,
                "Show only visible mobs.\nUsually you want to keep this ON\nMay not remove all invisible entities currently. Use the local objects menu.");

            if (configInterface.cfg.ShowOnlyVisible)
            {
                ImGui.SameLine();
                shouldSave |= UiHelpers.DrawCheckbox("Show Invisible Player Characters", ref configInterface.cfg.OverrideShowInvisiblePlayerCharacters,
                    "Will show invisible player characters (like GMs or players loading in).\nI have 0 proof that this works on GMs.");
            }

            shouldSave |= ImGui.Checkbox("Show Nameless", ref configInterface.cfg.ShowNameless);
            UiHelpers.LabeledHelpMarker("", "Show nameless mobs.\nYou probably want to keep this OFF.");

            shouldSave |= ImGui.Checkbox("Show All Entities", ref configInterface.cfg.DebugMode);
            UiHelpers.HoverTooltip("Shows everything no matter what.\n/radar showall");

            shouldSave |= ImGui.Checkbox("Show Rank Text", ref configInterface.cfg.RankText);
            UiHelpers.HoverTooltip("Shows rank text for BNpcs");
        }


        ImGui.Separator();
        ImGui.TextColored(new Vector4(0xff, 0x00, 0x00, 0xff),
            "Thank you for your support!");
        ImGui.Separator();

        ImGui.TextColored(new Vector4(0xff, 0x00, 0x00, 0xff),
            "Issues or Feedback: ");
        ImGui.SameLine();
        UiHelpers.TextURL("GitHub", "https://github.com/KangasZ/RadarPlugin", ImGui.GetColorU32(ImGuiCol.Text));
        ImGui.Indent();
        ImGui.TextColored(new Vector4(0xff, 0xff, 0x00, 0xff),
            "1. Use tabs to customize experience and fix invisible mobs.\n" +
            "2. Bring bugs or feature requests up\n");
        ImGui.Unindent();
        ImGui.Spacing();
        ImGui.TextWrapped(
            "Note 1: Entities that are not on the client are not viewable. For instance, deep dungeon traps are not visible until you unveil them.");
        ImGui.Spacing();
        ImGui.TextWrapped(
            "Note 2: Invisible mobs may be shown. Use the Utility tab to remove these.");
        if (shouldSave) configInterface.Save();
    }

    private bool DrawFontOptions()
    {
        var shouldSave = false;

        shouldSave |= ImGui.Checkbox("Use Custom Font##custom-font-selector-default", ref configInterface.cfg.FontSettings.UseCustomFont);
        UiHelpers.LabeledHelpMarker("",
            "Use a custom font size and potentially type instead of the default ImGui font.\nThis may lag your game slightly when you enable it the first time.");

        shouldSave |= ImGui.Checkbox("Use Axis Font##axis-font-selector-default", ref configInterface.cfg.FontSettings.UseAxisFont);
        UiHelpers.LabeledHelpMarker("", "Uses the axis font instead of default dalamud.\nThis is what most of the game is rendered with.");


        shouldSave |= UiHelpers.DrawFloatWithResetSlider(ref configInterface.cfg.FontSettings.FontSize, "Font Size", "font-scale-default-window", 7f, 36f,
            ImGui.GetFontSize(), "%.0fpx");


        if (ImGui.Button("12px"))
        {
            configInterface.cfg.FontSettings.FontSize = 12f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("16px"))
        {
            configInterface.cfg.FontSettings.FontSize = 16f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("17px"))
        {
            configInterface.cfg.FontSettings.FontSize = 17f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("18px"))
        {
            configInterface.cfg.FontSettings.FontSize = 18f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("20px"))
        {
            configInterface.cfg.FontSettings.FontSize = 20f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("22px"))
        {
            configInterface.cfg.FontSettings.FontSize = 22f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("24px"))
        {
            configInterface.cfg.FontSettings.FontSize = 24f;
            shouldSave = true;
        }

        ImGui.SameLine();
        if (ImGui.Button("36px"))
        {
            configInterface.cfg.FontSettings.FontSize = 36f;
            shouldSave = true;
        }

        return shouldSave;
    }

    private void DrawVisibilitySettings()
    {
        UiHelpers.DrawTabs("radar-visibility-tabs",
            ("Players", UtilInfo.Silver, DrawPlayerGeneralSettings),
            ("Mobs", UtilInfo.Green, DrawMobsVisibilitySettings),
            ("Entities", UtilInfo.LightBlue, DrawEntitiesVisibilitySettings),
            ("Deep Dungeons", UtilInfo.Yellow, DrawDeepDungeonOverviewSettings)
        );
    }

    private void DrawPlayerGeneralSettings()
    {
        var shouldSave = false;
        UiHelpers.DrawSeperator("Players", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.PlayerOption, "Players", mobType: MobType.Player);

        // Custom YOUR PLAYER that I don't want to deal with yet.\
        ImGui.Separator();
        ImGui.PushStyleColor(ImGuiCol.Text, UtilInfo.Red);
        ImGui.Text("Separators");
        ImGui.PopStyleColor();
        UiHelpers.LabeledHelpMarker("", "These options will dissociate the given category with the overriding player configuration.\n" +
                                        "Any player not in one of these categories will default to 'general' player option.");
        ImGui.Separator();
        shouldSave |= DrawBoolSeparatedSettingsOverview(ref configInterface.cfg.SeparatedYourPlayer, "Your Player", MobType.Player);

        shouldSave |= DrawBoolSeparatedSettingsOverview(ref configInterface.cfg.SeparatedParty, "Party", MobType.Player);

        shouldSave |= DrawBoolSeparatedSettingsOverview(ref configInterface.cfg.SeparatedFriends, "Friends", MobType.Player);

        shouldSave |= DrawBoolSeparatedSettingsOverview(ref configInterface.cfg.SeparatedAlliance, "Alliance", MobType.Player);

        if (shouldSave) configInterface.Save();
    }

    private void DrawDeepDungeonOverviewSettings()
    {
        UiHelpers.DrawSeperator($"Enemies Options", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.SpecialUndeadOption, "Special Undead", mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.DefaultEnemyOption, "'Catch All' mobs", mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.AuspiceOption, "Friendly Mobs", mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.EasyMobOption, "Easy Mobs", mobType: MobType.Character,
            displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.MimicOption, "Mimic", mobType: MobType.Character, displayOrigination: DisplayOrigination.DeepDungeon);

        UiHelpers.DrawSeperator($"Loot Options", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.GoldChestOption, "Gold Chest", displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.SilverChestOption, "Silver Chest", displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.BronzeChestOption, "Bronze Chest", displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.AccursedHoardOption, "Accursed Hoard", displayOrigination: DisplayOrigination.DeepDungeon);


        UiHelpers.DrawSeperator($"DD Specific Options", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.TrapOption, "Traps", displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.ReturnOption, "Return", displayOrigination: DisplayOrigination.DeepDungeon);
        DrawSettingsOverview(configInterface.cfg.DeepDungeonOptions.PassageOption, "Passage", displayOrigination: DisplayOrigination.DeepDungeon);
    }

    private void DrawSettingsOverview(Configuration.ESPOption espOption, string tag, string? description = null, MobType mobType = MobType.Object,
        DisplayOrigination displayOrigination = DisplayOrigination.OpenWorld)
    {
        bool shouldSave = false;
        shouldSave |= UiHelpers.DrawDisplayTypesEnumListBox("", $"visibilitygeneralsettings-enum-{tag}", mobType, ref espOption.DisplayType);
        ImGui.SameLine();
        shouldSave |= UiHelpers.Vector4ColorSelector($"##visiblitygeneralsettings-color-{tag}", ref espOption.ColorU);

        ImGui.SameLine();

        shouldSave |= ImGui.Checkbox($"{tag}##enabled-{tag}", ref espOption.Enabled);

        if (!description.IsNullOrWhitespace())
        {
            UiHelpers.LabeledHelpMarker("", description);
        }

        ImGui.SameLine(ImGui.GetWindowWidth() - 100);
        if (ImGui.Button($"More Options##button-for-type-configurator{tag}"))
        {
            typeConfigurator.OpenUiWithType(ref espOption, tag, mobType, displayOrigination);
        }

        if (shouldSave) configInterface.Save();
    }

    private void ShowMiscSettings()
    {
        var shouldSave = false;
        var id = "##miscsettings";
        ImGui.BeginChild($"{id}-child", new Vector2(0, 0));

        if (ImGui.CollapsingHeader($"Hitbox Options{id}"))
        {
            shouldSave |= ImGui.Checkbox($"Show Hitbox{id}-hitbox", ref configInterface.cfg.HitboxOptions.HitboxEnabled);

            shouldSave |= ImGui.DragFloat($"Thickness{id}", ref configInterface.cfg.HitboxOptions.Thickness, 0.1f, 0.1f, 14f);

            shouldSave |= ImGui.Checkbox($"Override Mob Color{id}-hitbox", ref configInterface.cfg.HitboxOptions.OverrideMobColor);

            if (configInterface.cfg.HitboxOptions.OverrideMobColor)
            {
                var hitboxColor = ImGui.ColorConvertU32ToFloat4(configInterface.cfg.HitboxOptions.HitboxColor);
                if (ImGui.ColorEdit4($"Color{id}-hitbox-color", ref hitboxColor, ImGuiColorEditFlags.NoInputs))
                {
                    configInterface.cfg.HitboxOptions.HitboxColor = ImGui.ColorConvertFloat4ToU32(hitboxColor);
                    configInterface.Save();
                }
            }

            shouldSave |= ImGui.Checkbox($"Draw Inside Color{id}-hitbox", ref configInterface.cfg.HitboxOptions.DrawInsideCircle);

            if (configInterface.cfg.HitboxOptions.DrawInsideCircle)
            {
                shouldSave |= ImGui.Checkbox("Use Different Inside Color", ref configInterface.cfg.HitboxOptions.UseDifferentInsideCircleColor);

                if (!configInterface.cfg.HitboxOptions.UseDifferentInsideCircleColor)
                {
                    var circleOpacity = (float)(configInterface.cfg.HitboxOptions.InsideCircleOpacity >> 24) / byte.MaxValue;
                    if (ImGui.DragFloat($"Inside Circle Opacity{id}", ref circleOpacity, 0.005f, 0, 1))
                    {
                        configInterface.cfg.HitboxOptions.InsideCircleOpacity = ((uint)(circleOpacity * 255) << 24) | 0x00FFFFFF;
                        configInterface.Save();
                    }
                }

                if (configInterface.cfg.HitboxOptions.UseDifferentInsideCircleColor)
                {
                    shouldSave |= UiHelpers.Vector4ColorSelector($"Inside Circle Color{id}-hitbox", ref configInterface.cfg.HitboxOptions.InsideCircleColor);
                }
            }
        }

        if (ImGui.CollapsingHeader("Aggro Radius Options"))
        {
            DrawAggroCircleSettings();
        }


        if (ImGui.CollapsingHeader("Off Screen Objects Settings"))
        {
            shouldSave |= ImGui.Checkbox("Show Offscreen Objects", ref configInterface.cfg.ShowOffScreen);
            UiHelpers.HoverTooltip("Show an arrow to the offscreen enemies.");

            shouldSave |= ImGui.DragFloat($"Distance From Edge{id}", ref configInterface.cfg.OffScreenObjectsOptions.DistanceFromEdge, 0.2f, 2f, 80f);

            shouldSave |= ImGui.DragFloat($"Size{id}", ref configInterface.cfg.OffScreenObjectsOptions.Size, 0.1f, 2f, 20f);

            shouldSave |= ImGui.DragFloat($"Thickness{id}", ref configInterface.cfg.OffScreenObjectsOptions.Thickness, 0.1f, 0.4f, 20f);
        }


        if (ImGui.CollapsingHeader("Font Settings"))
        {
            shouldSave |= DrawFontOptions();
        }


        if (ImGui.CollapsingHeader("Level-Based Rendering"))
        {
            var levelRenderingSettings = configInterface.cfg.LevelRendering;
            shouldSave |= UiHelpers.DrawCheckbox("Enabled", ref levelRenderingSettings.LevelRenderingEnabled, "Enable Relative Level-Based Rendering");
            if (levelRenderingSettings.LevelRenderingEnabled)
            {
                shouldSave |= UiHelpers.DrawIntWithResetSlider(ref levelRenderingSettings.RelativeLevelsBelow, "Relative Level To Render", "level-based-rel-level", 1, 89,
                    20);
                DrawSettingsOverview(levelRenderingSettings.LevelRenderEspOption, "Level-Based Enemies", "", MobType.Character);
            }
        }

        ImGui.EndChild();
        if (shouldSave) configInterface.Save();
    }

    private void DrawAggroCircleSettings()
    {
        var shouldSave = false;

        var tag = "aggroradiusoptions";

        shouldSave |= ImGui.Checkbox($"Aggro Circle##{tag}-settings", ref configInterface.cfg.AggroRadiusOptions.ShowAggroCircle);
        UiHelpers.HoverTooltip("Draws aggro circle.");

        shouldSave |= UiHelpers.DrawCheckbox($"Enable Max Distance For Aggro Radius##{tag}-max-dist", ref configInterface.cfg.AggroRadiusOptions.MaxDistanceCapBool,
            "Sets a max distance for aggro circles");
        shouldSave |= UiHelpers.DrawFloatWithResetSlider(ref configInterface.cfg.AggroRadiusOptions.MaxDistance, "", $"##{tag}-max-dist-slider", 1f, 2000f,
            UtilInfo.DefaultMaxAggroRadiusDistance, "%.0fm");

        shouldSave |= ImGui.Checkbox($"Aggro Circle In Combat##{tag}-settings", ref configInterface.cfg.AggroRadiusOptions.ShowAggroCircleInCombat);

        UiHelpers.HoverTooltip("If enabled, always show aggro circle.\nIf disabled, only show aggro circle when enemy is not engaged in combat.");

        shouldSave |= UiHelpers.Vector4ColorSelector($"Front##{tag}", ref configInterface.cfg.AggroRadiusOptions.FrontColor);
        ImGui.SameLine();
        shouldSave |= UiHelpers.Vector4ColorSelector($"Rear##{tag}", ref configInterface.cfg.AggroRadiusOptions.RearColor);

        shouldSave |= UiHelpers.Vector4ColorSelector($"Left##{tag}", ref configInterface.cfg.AggroRadiusOptions.LeftSideColor);
        ImGui.SameLine();
        shouldSave |= UiHelpers.Vector4ColorSelector($"Right##{tag}", ref configInterface.cfg.AggroRadiusOptions.RightSideColor);


        var circleOpacity = (float)(configInterface.cfg.AggroRadiusOptions.CircleOpacity >> 24) / byte.MaxValue;
        if (ImGui.DragFloat($"Circle Opacity##{tag}", ref circleOpacity, 0.005f, 0, 1))
        {
            configInterface.cfg.AggroRadiusOptions.CircleOpacity = ((uint)(circleOpacity * 255) << 24) | 0x00FFFFFF;
            configInterface.Save();
        }

        var coneOpacity = (float)(configInterface.cfg.AggroRadiusOptions.FrontConeOpacity >> 24) / byte.MaxValue;
        if (ImGui.DragFloat($"Cone Opacity##{tag}", ref coneOpacity, 0.005f, 0, 1))
        {
            configInterface.cfg.AggroRadiusOptions.FrontConeOpacity = ((uint)(coneOpacity * 255) << 24) | 0x00FFFFFF;
            configInterface.Save();
        }

        if (shouldSave) configInterface.Save();
    }


    private void DrawMobsVisibilitySettings()
    {
        bool shouldSave = false;
        ImGui.BeginChild($"##visiblitygeneralsettings-radar-tabs-child", new Vector2(0, 0));

        UiHelpers.DrawSeperator("Non-Enemy Npcs", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.CompanionOption, "Companions");
        DrawSettingsOverview(configInterface.cfg.EventNpcOption, "Event NPCs");
        DrawSettingsOverview(configInterface.cfg.RetainerOption, "Retainers");

        UiHelpers.DrawSeperator("Enemy Npcs", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.NpcOption, "Enemies",
            description: "Shows most enemies that are considered battleable", mobType: MobType.Character);
        shouldSave |= DrawBoolSeparatedSettingsOverview(ref configInterface.cfg.SeparatedRankOne, "Rank 1", MobType.Character, "Rank 1 is typically HUNT or NM enemies");
        shouldSave |= DrawBoolSeparatedSettingsOverview(ref configInterface.cfg.SeparatedRankTwoAndSix, "Rank 2 and 6", MobType.Character,
            "Rank 2 and 6 are typically BOSSES");
        ImGui.EndChild();
        if (shouldSave) configInterface.Save();
    }

    private void DrawEntitiesVisibilitySettings()
    {
        bool shouldSave = false;
        ImGui.BeginChild($"##visiblitygeneralsettings-radar-tabs-child", new Vector2(0, 0));

        UiHelpers.DrawSeperator("Objects", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.TreasureOption, "Treasure",
            "Shows most loot. The loot classification is via the dalamud's association.");
        DrawSettingsOverview(configInterface.cfg.EventObjOption, "Event Objects");
        DrawSettingsOverview(configInterface.cfg.AreaOption, "Area Objects");
        DrawSettingsOverview(configInterface.cfg.AetheryteOption, "Aetherytes");
        DrawSettingsOverview(configInterface.cfg.CutsceneOption, "Cutscene Objs",
            "Shows cutscene objects. I have no idea what these are!");

        UiHelpers.DrawSeperator("Misc", UtilInfo.Red);
        DrawSettingsOverview(configInterface.cfg.CardStandOption, "Card Stand",
            "Show card stand. This includes island sanctuary stuff (mostly).");
        DrawSettingsOverview(configInterface.cfg.GatheringPointOption, "Gathering Point",
            "Shows Gathering Points");
        DrawSettingsOverview(configInterface.cfg.MountOption, "Mount", "Shows mounts. Gets a little cluttered");
        DrawSettingsOverview(configInterface.cfg.OrnamentOption, "Ornaments", "Shows ornaments, like wings.");
        ImGui.EndChild();
        if (shouldSave) configInterface.Save();
    }

    private bool DrawBoolSeparatedSettingsOverview(ref Configuration.SeparatedEspOption separatedEspOption, string tag, MobType mobType, string? infoDescription = null)
    {
        var shouldSave = false;
        shouldSave |= ImGui.Checkbox($"Separate {tag}##separated-settings", ref separatedEspOption.Enabled);
        if (infoDescription != null)
        {
            UiHelpers.LabeledHelpMarker($"", infoDescription);
        }

        if (separatedEspOption.Enabled)
        {
            DrawSettingsOverview(separatedEspOption.EspOption, tag, mobType: mobType, description: infoDescription);
        }

        return shouldSave;
    }
}