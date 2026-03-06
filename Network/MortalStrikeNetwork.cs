using System;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using MortalStrike.Network;
using MortalStrike.Sound;
using UnityEngine;

namespace MortalStrike
{
    /// <summary>
    /// Отвечает за отправку/приём MortalStrikePacket через Fika.
    ///
    /// Headless: вызывает SendToClients() из EventOne при каждом этапе обстрела.
    /// Клиент:   подписывается на пакет и воспроизводит звук + показывает уведомление.
    /// </summary>
    public class MortalStrikeNetwork : MonoBehaviour
    {
        private ManualLogSource _log;
        private bool _isServer; // headless = true, клиент = false

        // Ссылки на Fika сетевые компоненты (получаем через FindObjectOfType)
        private FikaServer _fikaServer;
        private FikaClient _fikaClient;

        private ClientAudioListener _audioListener;

        public void Initialize(ManualLogSource log, bool isServer)
        {
            _log  = log;
            _isServer = isServer;

            if (isServer)
            {
                _fikaServer = FindObjectOfType<FikaServer>();
                if (_fikaServer == null)
                {
                    _log.LogWarning("[MortalStrike] MortalStrikeNetwork: FikaServer не найден — пакеты не будут отправляться.");
                    return;
                }
                _fikaServer.RegisterPacket<MortalStrikePacket>(OnServerReceived);
                _log.LogInfo("[MortalStrike] MortalStrikeNetwork: headless готов отправлять пакеты.");
            }
            else
            {
                _fikaClient = FindObjectOfType<FikaClient>();
                if (_fikaClient == null)
                {
                    _log.LogWarning("[MortalStrike] MortalStrikeNetwork: FikaClient не найден.");
                    return;
                }
                _fikaClient.RegisterPacket<MortalStrikePacket>(OnClientReceived);
                _log.LogInfo("[MortalStrike] MortalStrikeNetwork: клиент подписан на пакеты обстрела.");
            }
        }

        public void SetAudioListener(ClientAudioListener listener)
        {
            _audioListener = listener;
        }

        // ─── Headless: отправка пакета всем клиентам ─────────────────────────────

        public void SendToClients(byte eventType)
        {
            if (_fikaServer == null) return;

            var packet = new MortalStrikePacket { EventType = eventType };
            _fikaServer.SendData<MortalStrikePacket>(ref packet, DeliveryMethod.ReliableOrdered);
            _log.LogInfo($"[MortalStrike] Отправлен пакет клиентам: eventType={eventType}");
        }

        /// <summary>
        /// Headless → клиент: приказать конкретному игроку применить урон к себе.
        /// Шлём всем (broadcast), клиент фильтрует по ProfileId.
        /// </summary>
        public void SendDamageToClient(string profileId, float damage)
        {
            if (_fikaServer == null) return;

            var packet = new MortalStrikePacket
            {
                EventType = MortalStrikePacket.Damage_t,
                ProfileId = profileId,
                Damage    = damage,
            };
            _fikaServer.SendData<MortalStrikePacket>(ref packet, DeliveryMethod.ReliableOrdered);
        }

        // ─── Headless: эхо от клиентов (на всякий случай ignore) ─────────────────

        private void OnServerReceived(MortalStrikePacket packet)
        {
            // headless не должен получать этот пакет, но если случайно — игнорируем
        }

        // ─── Клиент: получение пакета от headless ────────────────────────────────

        private void OnClientReceived(MortalStrikePacket packet)
        {
            // ─── Пакет урона ──────────────────────────────────────────────────────
            if (packet.EventType == MortalStrikePacket.Damage_t)
            {
                try
                {
                    var localPlayer = GamePlayerOwner.MyPlayer;
                    if (localPlayer == null) return;
                    if (localPlayer.ProfileId != packet.ProfileId) return; // не наш пакет

                    var healthCtrl = localPlayer.ActiveHealthController;
                    if (healthCtrl == null || !healthCtrl.IsAlive) return;

                    float total = packet.Damage;
                    _log.LogInfo($"[MortalStrike] 💥 Получен урон от обстрела: {total:F0} hp");

                    var damageInfo = new DamageInfoStruct
                    {
                        DamageType       = EDamageType.Artillery,
                        ArmorDamage      = 80f,
                        PenetrationPower = 50f,
                        StaminaBurnRate  = 1f,
                        Direction        = Vector3.down,
                        IsForwardHit     = true,
                        HitPoint         = localPlayer.Position,
                        HitNormal        = Vector3.up,
                    };

                    // Распределение по частям тела: грудь 35%, голова 20%, конечности 45%
                    (EBodyPart part, float weight)[] parts =
                    {
                        (EBodyPart.Chest,    0.35f),
                        (EBodyPart.Head,     0.20f),
                        (EBodyPart.LeftLeg,  0.15f),
                        (EBodyPart.RightLeg, 0.15f),
                        (EBodyPart.LeftArm,  0.08f),
                        (EBodyPart.RightArm, 0.07f),
                    };

                    foreach (var (part, weight) in parts)
                    {
                        float d = total * weight;
                        if (d < 1f) continue;
                        damageInfo.Damage = d;
                        healthCtrl.ApplyDamage(part, d, damageInfo);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogError($"[MortalStrike] Ошибка применения урона на клиенте: {ex.Message}");
                }
                return;
            }

            // ─── Звуковые/уведомительные пакеты ─────────────────────────────────
            _log.LogInfo($"[MortalStrike] Получен пакет от headless: eventType={packet.EventType}");

            string notifText = null;
            WarningAudio.SoundGroup? soundGroup = null;

            switch (packet.EventType)
            {
                case MortalStrikePacket.MinuteBefore:
                    notifText  = "⚠️ Обстрел через 1 минуту";
                    soundGroup = WarningAudio.SoundGroup.MinuteBefore;
                    break;
                case MortalStrikePacket.TenSeconds:
                    notifText  = "🚨 Обстрел через 10 секунд";
                    soundGroup = WarningAudio.SoundGroup.TenSeconds;
                    break;
                case MortalStrikePacket.Bombing:
                    notifText  = "💥 Обстрел начался!";
                    soundGroup = WarningAudio.SoundGroup.Bombing;
                    break;
                case MortalStrikePacket.End:
                    notifText  = "✅ Обстрел завершён";
                    soundGroup = WarningAudio.SoundGroup.End;
                    break;
            }

            if (notifText != null)
                NotificationManagerClass.DisplayWarningNotification(notifText, ENotificationDurationType.Long);

            if (soundGroup.HasValue && _audioListener != null)
                _audioListener.PlayGroup(soundGroup.Value);
        }
    }
}
