using System;
using BepInEx.Logging;
using EFT.GlobalEvents;
using EFT.GlobalEvents.ArtilleryShellingEcents;
using MortalStrike.Sound;
using UnityEngine;

namespace MortalStrike
{
    /// <summary>
    /// Работает на клиенте.
    /// Получает сигналы через MortalStrikeNetwork (кастомный Fika-пакет) и воспроизводит звуки.
    /// </summary>
    public class ClientAudioListener : MonoBehaviour
    {
        private ManualLogSource _log;

        public void Initialize(ManualLogSource log)
        {
            _log = log;
            StartCoroutine(WarningAudio.LoadAll(log));
            log.LogInfo("[MortalStrike] ClientAudioListener: инициализирован, ждёт пакетов от headless.");
        }

        /// <summary>
        /// Вызывается из MortalStrikeNetwork при получении пакета.
        /// </summary>
        public void PlayGroup(WarningAudio.SoundGroup group)
        {
            _log.LogInfo($"[MortalStrike] 🔊 PlayGroup: {group}");
            StartCoroutine(WarningAudio.Play(group));
        }
    }
}
