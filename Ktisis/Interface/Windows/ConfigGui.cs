﻿using System.Numerics;

using ImGuiNET;

using Dalamud.Interface.Components;
using Dalamud.Interface;

using Ktisis.Util;
using Ktisis.Localization;
using Ktisis.Structs.Bones;
using Ktisis.Structs.Actor.Equip;
using Ktisis.Structs.Actor.Equip.SetSources;

namespace Ktisis.Interface.Windows {
	internal static class ConfigGui {
		public static bool Visible = false;

		// Toggle visibility

		public static void Show() {
			Visible = true;
		}

		public static void Hide() {
			Visible = false;
		}

		// Draw

		public static void Draw() {
			if (!Visible)
				return;

			var size = new Vector2(-1, -1);
			ImGui.SetNextWindowSize(size, ImGuiCond.Always);
			ImGui.SetNextWindowSizeConstraints(size, size);

			ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));

			if (ImGui.Begin("Ktisis Settings", ref Visible, ImGuiWindowFlags.NoResize)) {
				if (ImGui.BeginTabBar("Settings")) {
					var cfg = Ktisis.Configuration;
					if (ImGui.BeginTabItem("Interface"))
						DrawInterfaceTab(cfg);
					if (ImGui.BeginTabItem("Overlay"))
						DrawOverlayTab(cfg);
					if (ImGui.BeginTabItem("Gizmo"))
						DrawGizmoTab(cfg);
					if (ImGui.BeginTabItem("Language"))
						DrawLanguageTab(cfg);
					if (ImGui.BeginTabItem("Data"))
						DrawDataTab(cfg);

					ImGui.EndTabBar();
				}
			}

			ImGui.PopStyleVar(1);
			ImGui.End();
		}

		// Interface

		public static void DrawInterfaceTab(Configuration cfg) {
			var displayCharName = !cfg.DisplayCharName;
			if (ImGui.Checkbox("Hide character name", ref displayCharName))
				cfg.DisplayCharName = !displayCharName;

			ImGui.EndTabItem();
		}

		// Overlay

		public static void DrawOverlayTab(Configuration cfg) {
			var drawLines = cfg.DrawLinesOnSkeleton;
			if (ImGui.Checkbox("Draw lines on skeleton", ref drawLines))
				cfg.DrawLinesOnSkeleton = drawLines;

			var lineThickness = cfg.SkeletonLineThickness;
			if (ImGui.SliderFloat("Lines thickness", ref lineThickness, 0.01F, 15F, "%.1f"))
				cfg.SkeletonLineThickness = lineThickness;

			ImGui.Separator();

			bool linkBoneCategoriesColors = cfg.LinkBoneCategoryColors;
			if (ImGuiComponents.IconButton(FontAwesomeIcon.Link, linkBoneCategoriesColors ? new Vector4(0.0F, 1.0F, 0.0F, 0.4F) : null))
				cfg.LinkBoneCategoryColors = !linkBoneCategoriesColors;

			ImGui.SameLine();
			ImGui.Text(linkBoneCategoriesColors ? "Unlink bones colors" : "Link bones colors");

			ImGui.SameLine();
			if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Eraser, "Hold Control and Shift to erase colors.", ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift))
			{
				Vector4 eraseColor = new(1.0F, 1.0F, 1.0F, 0.5647059F);
				if (linkBoneCategoriesColors) {
					cfg.LinkedBoneCategoryColor = eraseColor;
				} else {
					foreach (Category category in Category.Categories.Values) {
						if (category.ShouldDisplay || cfg.BoneCategoryColors.ContainsKey(category.Name))
							cfg.BoneCategoryColors[category.Name] = eraseColor;
					}
				}
			}

			if (linkBoneCategoriesColors)
			{
				Vector4 linkedBoneColor = cfg.LinkedBoneCategoryColor;
				if (ImGui.ColorEdit4("Bones color", ref linkedBoneColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
					cfg.LinkedBoneCategoryColor = linkedBoneColor;
			} else {

				ImGui.SameLine();
				if (GuiHelpers.IconButtonHoldConfirm(FontAwesomeIcon.Rainbow, "Hold Control and Shift to reset colors to their default values.", ImGui.GetIO().KeyCtrl && ImGui.GetIO().KeyShift))
				{
					foreach ((string categoryName, Category category) in Category.Categories)
					{
						if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
							continue;
						cfg.BoneCategoryColors[category.Name] = category.DefaultColor;
					}
				}

				ImGui.Text("Bone colors by category");

				bool hasShownAnyCategory = false;
				foreach (Category category in Category.Categories.Values)
				{
					if (!category.ShouldDisplay && !cfg.BoneCategoryColors.ContainsKey(category.Name))
						continue;

					if (!cfg.BoneCategoryColors.TryGetValue(category.Name, out Vector4 categoryColor))
						categoryColor = cfg.LinkedBoneCategoryColor;

					if (ImGui.ColorEdit4(category.Name, ref categoryColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.AlphaBar))
						cfg.BoneCategoryColors[category.Name] = categoryColor;
					hasShownAnyCategory = true;
				}
				if (!hasShownAnyCategory)
					ImGui.TextWrapped("Categories will be added after bones are displayed once.");
			}

			ImGui.EndTabItem();
		}

		// Gizmo

		public static void DrawGizmoTab(Configuration cfg) {
			var allowAxisFlip = cfg.AllowAxisFlip;
			if (ImGui.Checkbox("Flip axis to face camera", ref allowAxisFlip))
				cfg.AllowAxisFlip = allowAxisFlip;

			ImGui.EndTabItem();
		}

		// Language

		public static void DrawLanguageTab(Configuration cfg) {
			var selected = "";
			foreach (var lang in Locale.Languages) {
				if (lang == cfg.Localization) {
					selected = $"{lang}";
					break;
				}
			}

			if (ImGui.BeginCombo("Language", selected)) {
				foreach (var lang in Locale.Languages) {
					var name = $"{lang}";
					if (ImGui.Selectable(name, name == selected))
						cfg.Localization = lang;
				}

				ImGui.SetItemDefaultFocus();
				ImGui.EndCombo();
			}

			var translateBones = cfg.TranslateBones;
			if (ImGui.Checkbox("Translate bone names", ref translateBones))
				cfg.TranslateBones = translateBones;

			ImGui.EndTabItem();
		}
		public static void DrawDataTab(Configuration cfg) {
			ImGui.Spacing();
			var validGlamPlatesFound = GlamourDresser.CountValid();
			GuiHelpers.TextTooltip($"Glamour Plates in memory: {validGlamPlatesFound}  ", $"Found {validGlamPlatesFound} valid Glamour Plates");
			ImGui.SameLine();

			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Sync, "Refresh Glamour Plate memory for the Sets lookups.\nThis memory is kept after a restart.\n\nRequirements:\n One of these windows must be opened: \"Glamour Plate Creation\" (by the Glamour Dresser) or \"Plate Selection\" (by the Glamour Plate skill)."))
				GlamourDresser.PopulatePlatesData();

			Components.Equipment.CreateGlamourQuestionPopup();

			ImGui.SameLine();
			if (GuiHelpers.IconButtonTooltip(FontAwesomeIcon.Trash, "Dispose of the Glamour Plates memory and remove configurations for ALL characters.")) {
				Sets.Dispose();
				cfg.GlamourPlateData = null;
			}

			ImGui.Spacing();
		}
	}
}
