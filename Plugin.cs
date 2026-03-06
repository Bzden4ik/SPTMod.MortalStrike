using BepInEx;
using BepInEx.Logging;
using MortalStrike.Config;
using MortalStrike.Network;
using MortalStrike.Patches;

namespace MortalStrike
{
    [BepInPlugin("com.mortalstrike.mod", "MortalStrike", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;
        public static ModConfig Cfg;

        /// <summary>
        /// Сетевой менеджер на headless — используется EventOne для отправки пакетов клиентам.
        /// </summary>
        public static MortalStrikeNetwork NetworkInstance;

        /// <summary>
        /// Контроллер урона — создаётся на headless и в соло-режиме.
        /// Используется EventOne/EventTwo для прямого нанесения урона (fallback на headless).
        /// </summary>
        public static ArtilleryDamageController DamageController;

        private void Awake()
        {
            Log = Logger;
            Cfg = new ModConfig(base.Config);

            new GameTimerStartPatch().Enable();

            Log.LogInfo("[MortalStrike] Plugin loaded! 💣");
        }
    }
}
