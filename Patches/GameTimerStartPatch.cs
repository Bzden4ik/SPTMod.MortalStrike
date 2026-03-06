using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using MortalStrike.Network;
using UnityEngine;

namespace MortalStrike.Patches
{
    public class GameTimerStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(GameTimerClass), nameof(GameTimerClass.Start));
        }

        [PatchPostfix]
        static void Postfix()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null)
            {
                Plugin.Log.LogWarning("[MortalStrike] GameWorld not found at timer start — skipping.");
                return;
            }

            bool isHeadless = RuntimeContext.IsHeadless;
            Plugin.Log.LogInfo($"[MortalStrike] Режим: {(isHeadless ? "Headless (сервер)" : "Клиент / Соло")}");

            // ─── MortalStrikeNetwork: кастомные пакеты Fika ──────────────────────
            // Создаём компонент немного отложенно чтобы FikaServer/FikaClient успел инициализироваться.
            // Используем один тик через StaticManager.
            var netObj = new GameObject("MortalStrikeNetwork");
            Object.DontDestroyOnLoad(netObj);
            var network = netObj.AddComponent<MortalStrikeNetwork>();

            if (!isHeadless)
            {
                // ─── Клиент ───────────────────────────────────────────────────────
                var existingListener = gameWorld.GetComponent<ClientAudioListener>();
                if (existingListener != null) Object.Destroy(existingListener);

                var listener = gameWorld.gameObject.AddComponent<ClientAudioListener>();
                listener.Initialize(Plugin.Log);

                network.SetAudioListener(listener);
                network.Initialize(Plugin.Log, isServer: false);

                // Соло (без Fika): урон применяем локально, headless отсутствует
                if (!RuntimeContext.IsFikaLoaded)
                {
                    Plugin.Log.LogInfo("[MortalStrike] Соло-режим: создаём ArtilleryDamageController локально.");
                    var existingDmg = gameWorld.GetComponent<ArtilleryDamageController>();
                    if (existingDmg != null) Object.Destroy(existingDmg);

                    var dmg = gameWorld.gameObject.AddComponent<ArtilleryDamageController>();
                    dmg.Initialize(Plugin.Log);
                    Plugin.DamageController = dmg;
                }
            }
            else
            {
                // ─── Headless ─────────────────────────────────────────────────────
                var existingDmg = gameWorld.GetComponent<ArtilleryDamageController>();
                if (existingDmg != null) Object.Destroy(existingDmg);

                var dmg = gameWorld.gameObject.AddComponent<ArtilleryDamageController>();
                dmg.Initialize(Plugin.Log);
                Plugin.DamageController = dmg;

                network.Initialize(Plugin.Log, isServer: true);

                // Передаём network в MortalStrikeController (чтобы слать пакеты)
                Plugin.NetworkInstance = network;
            }

            // ─── MortalStrikeController ───────────────────────────────────────────
            var existing = gameWorld.GetComponent<MortalStrikeController>();
            if (existing != null) Object.Destroy(existing);

            var controller = gameWorld.gameObject.AddComponent<MortalStrikeController>();
            controller.Initialize(gameWorld);
        }
    }
}
