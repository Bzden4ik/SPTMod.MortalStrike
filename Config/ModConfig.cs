using BepInEx.Configuration;

namespace MortalStrike.Config
{
    public class ModConfig
    {
        // ─── Event 1: Random Mortar Strike ───────────────────────────────────────
        public ConfigEntry<float> Event1Chance { get; }
        public ConfigEntry<float> Event1MinDelayMinutes { get; }
        public ConfigEntry<float> Event1MaxDelayMinutes { get; }
        public ConfigEntry<int>   Event1TargetCount { get; }
        public ConfigEntry<float> Event1DelayBetweenStrikes { get; }

        // ─── Debug ────────────────────────────────────────────────────────────────
        public ConfigEntry<bool> ForceEventNextRaid { get; }

        // ─── Damage ───────────────────────────────────────────────────────────────
        public ConfigEntry<float> DamageRadius { get; }
        public ConfigEntry<float> DamageMax { get; }
        public ConfigEntry<float> DamageMin { get; }
        public ConfigEntry<float> DamageLethalRadius { get; }

        // ─── Event 2: Full Map Purge ──────────────────────────────────────────────
        public ConfigEntry<float> Event2Chance { get; }
        public ConfigEntry<float> Event2StartBeforeEndMinutes { get; }
        public ConfigEntry<float> Event2WaveDelay { get; }
        public ConfigEntry<int>   Event2ZonesPerWave { get; }

        public ModConfig(ConfigFile cfg)
        {
            // Event 1
            Event1Chance = cfg.Bind(
                "Event1_RandomStrike", "Chance", 0.20f,
                "Вероятность ивента 1 за рейд (0.0 - 1.0). По умолч. 0.20 = 1 раз в 5 рейдов.");

            Event1MinDelayMinutes = cfg.Bind(
                "Event1_RandomStrike", "MinDelayMinutes", 5f,
                "Минимальная задержка в минутах от начала рейда до обстрела.");

            Event1MaxDelayMinutes = cfg.Bind(
                "Event1_RandomStrike", "MaxDelayMinutes", 10f,
                "Максимальная задержка в минутах от начала рейда до обстрела.");

            Event1TargetCount = cfg.Bind(
                "Event1_RandomStrike", "TargetCount", 12,
                "Количество случайных целей (точек) для обстрела.");

            Event1DelayBetweenStrikes = cfg.Bind(
                "Event1_RandomStrike", "DelayBetweenStrikes", 10f,
                "Задержка в секундах между каждым отдельным ударом.");

            // Debug
            ForceEventNextRaid = cfg.Bind(
                "Debug", "ForceEventNextRaid", false,
                "Включить чтобы в следующем рейде ивент гарантированно сработал (100% шанс). Сбросить после теста.");

            // Damage
            DamageRadius = cfg.Bind(
                "Damage", "DamageRadius", 15f,
                "Радиус поражения взрыва в метрах. Игроки дальше этого расстояния урон не получают.");

            DamageMax = cfg.Bind(
                "Damage", "DamageMax", 120f,
                "Максимальный урон (в упор, в зоне LethalRadius).");

            DamageMin = cfg.Bind(
                "Damage", "DamageMin", 15f,
                "Минимальный урон (на краю DamageRadius).");

            DamageLethalRadius = cfg.Bind(
                "Damage", "DamageLethalRadius", 4f,
                "Радиус летальной зоны (метры). Внутри — максимальный урон.");

            // Event 2
            Event2Chance = cfg.Bind(
                "Event2_FullMapPurge", "Chance", 0.20f,
                "Вероятность того что Ивент 1 превратится в Ивент 2 (20%).");

            Event2StartBeforeEndMinutes = cfg.Bind(
                "Event2_FullMapPurge", "StartBeforeEndMinutes", 10f,
                "Ивент 2 начинается за X минут до конца рейда.");

            Event2WaveDelay = cfg.Bind(
                "Event2_FullMapPurge", "WaveDelaySec", 90f,
                "Задержка в секундах между волнами зачистки карты.");

            Event2ZonesPerWave = cfg.Bind(
                "Event2_FullMapPurge", "ZonesPerWave", 2,
                "Количество зон, расстреливаемых за одну волну.");
        }
    }
}
