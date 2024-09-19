using System;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Analytics;

namespace MyseIfRDPatches
{
	public static class ShowAccuracy
	{
		internal static int[] P1Hits = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
		internal static int[] P2Hits = { 0, 0, 0, 0, 0, 0, 0, 0, 0 };

		internal static double[] lastMistakes = new double[2];

		public static void AddAccuracy(RDPlayer player, double timeOffset)
		{
			// miliseconds -> milliseconds
			int milliseconds = (int)Math.Abs(timeOffset * 1000);

			int num = milliseconds / 40 + 1;

			if (milliseconds <= 25) num = 4;
			else if (milliseconds >= 120) num = 4 + 4 * Math.Sign(timeOffset);
			else num = 4 + Math.Sign(timeOffset) * num;

			switch (player)
			{
				case RDPlayer.P1:
					P1Hits[num]++;
					break;
				case RDPlayer.P2:
					P2Hits[num]++;
					break;
				default:
					break;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(scrPlayerbox), "Pulse")]
		public static bool Prefix(scrPlayerbox __instance, float timeOffset, bool CPUTriggered, bool bomb)
		{
			if (CPUTriggered || Time.frameCount == Row.lastHitFrame[(int)__instance.ent.row.playerProp.GetCurrentPlayer()]) return true;
			if (bomb) AddAccuracy(__instance.ent.row.playerProp.GetCurrentPlayer(), 999);
			else AddAccuracy(__instance.ent.row.playerProp.GetCurrentPlayer(), (double)timeOffset);
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(scrPlayerbox), "SpaceBarReleased")]
		public static bool Prefix(RDPlayer player, scrPlayerbox __instance, bool cpuTriggered)
		{
			if (player != __instance.ent.row.playerProp.GetCurrentPlayer() || !(bool)__instance.currentHoldBeat || cpuTriggered) return true;
			double timeOffset = __instance.conductor.audioPos - __instance.currentHoldBeat.releaseTime;
			AddAccuracy(player, timeOffset);
			return true;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Beat), "Update")]
		public static bool Prefix(Beat __instance)
		{
			if (__instance.row.playerBox != null && __instance.row.ent != null && !__instance.row.dead
			 && __instance.conductor.audioPos > __instance.inputTime + 0.4 && !__instance.playerDrives7thBeat
			 && !__instance.hasPulsed7thBeat && !__instance.bomb && !__instance.unhittable)
			{
				int player = (int)__instance.row.playerProp.GetCurrentPlayer();
				if (lastMistakes[player] != __instance.inputTime) {
					AddAccuracy(__instance.row.playerProp.GetCurrentPlayer(), 999);
				}
				lastMistakes[player] = __instance.inputTime;
			}
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(HUD), "ShowRankDescription")]
		public static void Postfix(HUD __instance)
		{
			if (__instance.game.currentLevel.levelType == LevelType.Boss) return;
			if (GC.showAbsoluteOffsets)
			{
#warning change accuracy formula
				double P1Accuracy = (P1Hits[0] + P1Hits[1] + P1Hits[2] * 0.75 + P1Hits[3] * 0.5) / (P1Hits[0] + P1Hits[1] + P1Hits[2] + P1Hits[3] + P1Hits[4]) * 100;
				double P2Accuracy = (P2Hits[0] + P2Hits[1] + P2Hits[2] * 0.75 + P2Hits[3] * 0.5) / (P2Hits[0] + P2Hits[1] + P2Hits[2] + P2Hits[3] + P2Hits[4]) * 100;
				string SingleplayerResults = "";
				string P1Results = "";
				string P2Results = "";
				if (!GC.twoPlayerMode)
				{
					if (Main.configAccuracyMode.Value == Main.AccuracyOptions.Spread)
					{
						int[] c = P1Hits;
						SingleplayerResults = $"{__instance.resultsSingleplayer.text}\n(-) {c[0]} {c[1]} {c[2]} {c[3]} ({c[4]}) {c[5]} {c[6]} {c[7]} {c[8]} (+)";
					}
					else
					{
						SingleplayerResults = __instance.resultsSingleplayer.text +
							"\nAccuracy: " + Math.Round(P1Accuracy, 2).ToString("0.00") + "%" +
							(Main.configAccuracyMode.Value == Main.AccuracyOptions.ADOFAI && P1Hits[0] > 0 ? " + " + (P1Hits[0] * 0.01).ToString("0.00") + "%" : "");
					}

				}
				else
				{
					if (Main.configAccuracyMode.Value == Main.AccuracyOptions.Spread)
					{
						int[] c = P1Hits;
						P1Results = $"{__instance.resultsP1.text}\n(-) {c[0]} {c[1]} {c[2]} {c[3]} ({c[4]}) {c[5]} {c[6]} {c[7]} {c[8]} (+)";
						int[] c2 = P2Hits;
						P1Results = $"{__instance.resultsP2.text}\n(-) {c2[0]} {c2[1]} {c2[2]} {c2[3]} ({c2[4]}) {c2[5]} {c2[6]} {c2[7]} {c2[8]} (+)";
					}
					else
					{
						P1Results = __instance.resultsP1.text +
												"\nAccuracy: " + Math.Round(P1Accuracy, 2).ToString("0.00") + "%" +
												(Main.configAccuracyMode.Value == Main.AccuracyOptions.ADOFAI && P1Hits[0] > 0 ? " + " + (P1Hits[0] * 0.01).ToString("0.00") + "%" : "");
						P2Results = __instance.resultsP2.text +
							"\nAccuracy: " + Math.Round(P2Accuracy, 2).ToString("0.00") + "%" +
							(Main.configAccuracyMode.Value == Main.AccuracyOptions.ADOFAI && P2Hits[0] > 0 ? " + " + (P2Hits[0] * 0.01).ToString("0.00") + "%" : "");
					}

				}
				__instance.resultsSingleplayer.text = SingleplayerResults;
				__instance.resultsP1.text = P1Results;
				__instance.resultsP2.text = P2Results;
			}
		}
	}
}
