using System.Collections;
using Comfort.Common;
using CommonAssets.Scripts.ArtilleryShelling;
using EFT;
using MortalStrike.Events;
using MortalStrike.Sound;
using UnityEngine;

namespace MortalStrike
{
    public class MortalStrikeController : MonoBehaviour
    {
        private GameWorld _gameWorld;
        private ServerShellingControllerClass _shellingCtrl;
        private float _raidStartTime;

        // ─── Инициализация ────────────────────────────────────────────────────────

        public void Initialize(GameWorld gameWorld)
        {
            _gameWorld = gameWorld;
            _raidStartTime = Time.time;

            _shellingCtrl = ServerShellingControllerClass.Instance;

            if (_shellingCtrl == null)
            {
                Plugin.Log.LogInfo("[MortalStrike] ServerShellingController отсутствует — мод отключён на этот рейд.");
                return;
            }

            var mapCfg = _shellingCtrl.CurrentMapConfiguration;

            ShellingZoneFactory.EnsureBrigades(_shellingCtrl, Plugin.Log);

            Plugin.Log.LogInfo($"[MortalStrike] Инициализирован. Зон в конфиге: {(mapCfg.ShellingZones != null ? mapCfg.ShellingZones.Length : 0)} (не используются).");

            StartCoroutine(LoadAudioThenDecide());
        }

        // ─── Предзагрузка звука ─────────────────────────────────────────────────

        private IEnumerator LoadAudioThenDecide()
        {
            yield return StartCoroutine(WarningAudio.LoadAll(Plugin.Log));

            Plugin.Log.LogInfo("[MortalStrike] Загрузка звуков завершена.");

            yield return StartCoroutine(DecideAndRunEvent());
        }

        // ─── Выбор ивента ─────────────────────────────────────────────────────────

        private IEnumerator DecideAndRunEvent()
        {
            yield return new WaitForSeconds(3f);

            if (!IsRaidAlive()) yield break;

            bool forced = Plugin.Cfg.ForceEventNextRaid.Value;
            float rollEvent1 = forced ? 0f : Random.value;
            float threshEvent1 = forced ? 1f : Plugin.Cfg.Event1Chance.Value;

            if (forced)
                Plugin.Log.LogInfo("[MortalStrike] DEBUG: ForceEventNextRaid активен — ивент гарантирован.");
            else
                Plugin.Log.LogInfo($"[MortalStrike] Бросок на ивент: {rollEvent1:F3} (порог {threshEvent1:F3})");

            if (rollEvent1 > threshEvent1)
            {
                Plugin.Log.LogInfo("[MortalStrike] Ивент не выпал. Тихий рейд.");
                yield break;
            }

            float rollEvent2 = Random.value;
            float threshEvent2 = Plugin.Cfg.Event2Chance.Value;
            bool isEvent2 = rollEvent2 <= threshEvent2;

            Plugin.Log.LogInfo($"[MortalStrike] Бросок на эскалацию: {rollEvent2:F3} (порог {threshEvent2:F3}) → {(isEvent2 ? "ИВЕНТ 2!" : "Ивент 1")}");

            if (isEvent2)
                yield return StartCoroutine(RunEvent2Sequence());
            else
                yield return StartCoroutine(RunEvent1Sequence());
        }

        // ─── Ивент 1 ──────────────────────────────────────────────────────────────

        private IEnumerator RunEvent1Sequence()
        {
            float minSec = Plugin.Cfg.Event1MinDelayMinutes.Value * 60f;
            float maxSec = Plugin.Cfg.Event1MaxDelayMinutes.Value * 60f;
            float sessionLen = GetSessionLengthSeconds();

            float safeMax = Mathf.Max(minSec + 30f, sessionLen - 300f);
            float upperBound = Mathf.Max(minSec + 30f, Mathf.Min(maxSec, safeMax));
            float delayFromNow = Random.Range(minSec, upperBound);

            Plugin.Log.LogInfo($"[MortalStrike] Ивент 1 запустится через {delayFromNow / 60f:F1} мин.");

            yield return new WaitForSeconds(delayFromNow);

            if (!IsRaidAlive()) yield break;

            yield return StartCoroutine(EventOne.Execute(_shellingCtrl, Plugin.Log, Plugin.Cfg));
        }

        // ─── Ивент 2 ──────────────────────────────────────────────────────────────

        private IEnumerator RunEvent2Sequence()
        {
            float sessionLen = GetSessionLengthSeconds();
            float startBeforeEnd = Plugin.Cfg.Event2StartBeforeEndMinutes.Value * 60f;
            float triggerAtFromRaidStart = sessionLen - startBeforeEnd;
            float alreadyElapsed = Time.time - _raidStartTime;
            float waitMore = Mathf.Max(10f, triggerAtFromRaidStart - alreadyElapsed);

            Plugin.Log.LogInfo($"[MortalStrike] Ивент 2 запустится через {waitMore / 60f:F1} мин. " +
                               $"(за {startBeforeEnd / 60f:F0} мин. до конца рейда [{sessionLen / 60f:F0} мин.])");

            yield return new WaitForSeconds(waitMore);

            if (!IsRaidAlive()) yield break;

            yield return StartCoroutine(EventTwo.Execute(_shellingCtrl, Plugin.Log, Plugin.Cfg));
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────────

        private float GetSessionLengthSeconds()
        {
            var game = Singleton<AbstractGame>.Instance;
            if (game?.GameTimer?.SessionTime != null)
                return (float)game.GameTimer.SessionTime.Value.TotalSeconds;

            Plugin.Log.LogWarning("[MortalStrike] Не удалось определить длительность рейда. Используем 45 мин.");
            return 2700f;
        }

        private bool IsRaidAlive()
        {
            if (_gameWorld == null) return false;
            if (_shellingCtrl == null) return false;
            if (Singleton<GameWorld>.Instance == null) return false;
            return true;
        }
    }
}
