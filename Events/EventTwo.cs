using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using EFT.Communications;
using MortalStrike.Config;
using MortalStrike.Sound;
using UnityEngine;

namespace MortalStrike.Events
{
    public static class EventTwo
    {
        private static readonly Vector2[] MapSectors =
        {
            new Vector2(   0f,    0f),
            new Vector2(-150f, -150f),
            new Vector2( 150f, -150f),
            new Vector2(-150f,  150f),
            new Vector2( 150f,  150f),
            new Vector2(-200f,    0f),
            new Vector2( 200f,    0f),
            new Vector2(   0f, -200f),
            new Vector2(   0f,  200f),
        };

        public static IEnumerator Execute(ServerShellingControllerClass ctrl, ManualLogSource log, ModConfig cfg)
        {
            int zonesPerWave = Mathf.Max(1, cfg.Event2ZonesPerWave.Value);
            float waveDelay  = cfg.Event2WaveDelay.Value;
            bool isHeadless  = RuntimeContext.IsHeadless;

            var shuffled = new List<Vector2>(MapSectors);
            ShuffleList(shuffled);
            var waves = SplitWaves(shuffled, zonesPerWave);

            log.LogInfo($"[MortalStrike] 🔥 ИВЕНТ 2 — {shuffled.Count} точек, {waves.Count} волн, интервал {waveDelay}с.");

            NotificationManagerClass.DisplayMessageNotification(
                "🔥 ТОТАЛЬНЫЙ ОБСТРЕЛ — Вся карта под огнём!",
                ENotificationDurationType.Long,
                ENotificationIconType.Alert,
                null
            );

            yield return new WaitForSeconds(3f);

            for (int waveIdx = 0; waveIdx < waves.Count; waveIdx++)
            {
                if (!IsRaidAlive()) yield break;

                var wave = waves[waveIdx];
                log.LogInfo($"[MortalStrike] Ивент 2: волна {waveIdx + 1}/{waves.Count} ({wave.Count} точек)");

                yield return WarningAudio.Play(WarningAudio.SoundGroup.MinuteBefore);

                if (!IsRaidAlive()) yield break;

                if (waveIdx > 0)
                {
                    NotificationManagerClass.DisplayMessageNotification(
                        $"Волна обстрела {waveIdx + 1}/{waves.Count}",
                        ENotificationDurationType.Default,
                        ENotificationIconType.Alert,
                        null
                    );
                }

                foreach (var sector in wave)
                {
                    if (!IsRaidAlive()) yield break;

                    Vector2 jitter = Random.insideUnitCircle * 30f;
                    Vector3 pos = new Vector3(sector.x + jitter.x, 0f, sector.y + jitter.y);

                    log.LogInfo($"[MortalStrike]   → удар @ ({pos.x:F0}, {pos.z:F0})");

                    var zone = ShellingZoneFactory.Create(ctrl, pos);
                    _ = ctrl.StartEventShelling(zone, 15f);

                    // Headless fallback: применяем урон напрямую
                    if (isHeadless && Plugin.DamageController != null)
                        Plugin.DamageController.ApplyAreaDamage(pos);

                    yield return new WaitForSeconds(0.5f);
                }

                if (waveIdx < waves.Count - 1)
                {
                    log.LogInfo($"[MortalStrike] Ивент 2: следующая волна через {waveDelay}с...");
                    yield return new WaitForSeconds(waveDelay);
                }
            }

            NotificationManagerClass.DisplayMessageNotification(
                "Обстрел завершён.",
                ENotificationDurationType.Default,
                ENotificationIconType.Default,
                null
            );

            log.LogInfo("[MortalStrike] Ивент 2: завершён.");
        }

        private static List<List<Vector2>> SplitWaves(List<Vector2> points, int perWave)
        {
            var result = new List<List<Vector2>>();
            for (int i = 0; i < points.Count; i += perWave)
                result.Add(points.GetRange(i, Mathf.Min(perWave, points.Count - i)));
            return result;
        }

        private static void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static bool IsRaidAlive() =>
            ServerShellingControllerClass.Instance != null;
    }
}
