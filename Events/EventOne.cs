using System.Collections;
using BepInEx.Logging;
using EFT.Communications;
using MortalStrike.Config;
using MortalStrike.Network;
using MortalStrike.Sound;
using UnityEngine;

namespace MortalStrike.Events
{
    public static class EventOne
    {
        public static IEnumerator Execute(ServerShellingControllerClass ctrl, ManualLogSource log, ModConfig cfg)
        {
            int count = cfg.Event1TargetCount.Value;
            bool isHeadless = RuntimeContext.IsHeadless;

            log.LogInfo($"[MortalStrike] Ивент 1: {count} точек, запускаем отсчёт.");

            // ── 1. Предупреждение за 1 минуту ────────────────────────────────────
            NotificationManagerClass.DisplayMessageNotification(
                "⚠️ Обстрел через 1 минуту. Покиньте открытые зоны!",
                ENotificationDurationType.Long,
                ENotificationIconType.Alert,
                null
            );

            // Headless: шлём пакет клиентам
            if (isHeadless) Plugin.NetworkInstance?.SendToClients(MortalStrikePacket.MinuteBefore);
            // Соло/хост: играем звук сами
            else yield return WarningAudio.Play(WarningAudio.SoundGroup.MinuteBefore);

            if (!IsRaidAlive()) yield break;

            yield return new WaitForSeconds(50f);

            if (!IsRaidAlive()) yield break;

            // ── 2. Предупреждение за 10 секунд ───────────────────────────────────
            NotificationManagerClass.DisplayMessageNotification(
                "🚨 Обстрел через 10 секунд!",
                ENotificationDurationType.Default,
                ENotificationIconType.Alert,
                null
            );

            if (isHeadless) Plugin.NetworkInstance?.SendToClients(MortalStrikePacket.TenSeconds);
            else yield return WarningAudio.Play(WarningAudio.SoundGroup.TenSeconds);

            if (!IsRaidAlive()) yield break;

            yield return new WaitForSeconds(10f);

            if (!IsRaidAlive()) yield break;

            // ── 3. Обстрел начался ───────────────────────────────────────────────
            NotificationManagerClass.DisplayMessageNotification(
                "💥 Обстрел начался!",
                ENotificationDurationType.Long,
                ENotificationIconType.Alert,
                null
            );

            if (isHeadless) Plugin.NetworkInstance?.SendToClients(MortalStrikePacket.Bombing);
            else yield return WarningAudio.Play(WarningAudio.SoundGroup.Bombing);

            var targets = TargetSelector.SelectTargets(count, log);

            for (int i = 0; i < targets.Count; i++)
            {
                if (!IsRaidAlive()) yield break;
                var pos = targets[i];
                log.LogInfo($"[MortalStrike] Ивент 1: точка #{i + 1}/{targets.Count} → ({pos.x:F0}, {pos.z:F0})");
                var zone = ShellingZoneFactory.CreateLong(ctrl, pos);
                _ = ctrl.StartEventShelling(zone, 5f);
            }

            log.LogInfo($"[MortalStrike] Ивент 1: все {targets.Count} точек запущены.");

            if (!IsRaidAlive()) yield break;

            // ── 4. Ждём ~75с (длительность зоны) затем End ───────────────────────
            // Headless fallback: ShellingProjectileExplosionEvent не стреляет на сервере →
            // каждые 4с применяем урон напрямую по всем точкам обстрела.
            if (isHeadless && Plugin.DamageController != null)
            {
                float elapsed = 0f;
                const float total    = 75f;
                const float interval = 4f;

                log.LogInfo("[MortalStrike] Ивент 1: headless fallback — прямое нанесение урона каждые 4с.");

                while (elapsed < total)
                {
                    yield return new WaitForSeconds(interval);
                    elapsed += interval;

                    if (!IsRaidAlive()) yield break;

                    foreach (var pos in targets)
                        Plugin.DamageController.ApplyAreaDamage(pos);
                }
            }
            else
            {
                yield return new WaitForSeconds(75f);
            }

            if (!IsRaidAlive()) yield break;

            NotificationManagerClass.DisplayMessageNotification(
                "✅ Обстрел завершён.",
                ENotificationDurationType.Default,
                ENotificationIconType.Default,
                null
            );

            if (isHeadless) Plugin.NetworkInstance?.SendToClients(MortalStrikePacket.End);
            else yield return WarningAudio.Play(WarningAudio.SoundGroup.End);

            log.LogInfo("[MortalStrike] Ивент 1: завершён.");
        }

        private static bool IsRaidAlive() =>
            ServerShellingControllerClass.Instance != null;
    }
}
