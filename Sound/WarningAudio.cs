using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using EFT;
using UnityEngine;
using UnityEngine.Networking;

namespace MortalStrike.Sound
{
    /// <summary>
    /// Управляет четырьмя группами звуков: 1MinuteTo, 10SecondsBefore, Bombing, End.
    /// Внутри каждой группы файлы ротируются — уже сыгравший в этом рейде файл
    /// исключается из следующего до тех пор, пока не сыграют все варианты.
    /// </summary>
    public static class WarningAudio
    {
        // ─── Группы звуков ───────────────────────────────────────────────────────

        public enum SoundGroup { MinuteBefore, TenSeconds, Bombing, End }

        private static readonly Dictionary<SoundGroup, string> GroupPrefix =
            new Dictionary<SoundGroup, string>
            {
                { SoundGroup.MinuteBefore,  "1MinuteTo"        },
                { SoundGroup.TenSeconds,    "10SecondsBefore"  },
                { SoundGroup.Bombing,       "Bombing"          },
                { SoundGroup.End,           "End"              },
            };

        // Загруженные клипы: group → список вариантов
        private static readonly Dictionary<SoundGroup, List<AudioClip>> _clips =
            new Dictionary<SoundGroup, List<AudioClip>>();

        // Файлы, уже сыгравшие в текущей сессии игры: group → использованные имена
        private static readonly Dictionary<SoundGroup, HashSet<string>> _usedThisSession =
            new Dictionary<SoundGroup, HashSet<string>>();

        private static bool _loadAttempted;
        private static ManualLogSource _log;

        // ─── Загрузка ────────────────────────────────────────────────────────────

        public static IEnumerator LoadAll(ManualLogSource log)
        {
            if (_loadAttempted) yield break;
            _loadAttempted = true;
            _log = log;

            if (RuntimeContext.IsHeadless)
            {
                log.LogInfo("[MortalStrike] Headless: загрузка звуков пропущена.");
                yield break;
            }

            string soundDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "MortalStrikeSound");

            if (!Directory.Exists(soundDir))
            {
                log.LogError($"[MortalStrike] Папка со звуками не найдена: {soundDir}");
                yield break;
            }

            foreach (SoundGroup group in Enum.GetValues(typeof(SoundGroup)))
            {
                _clips[group]          = new List<AudioClip>();
                _usedThisSession[group] = new HashSet<string>();

                string prefix = GroupPrefix[group];
                var files = Directory.GetFiles(soundDir, "*.mp3")
                    .Where(f => {
                        string name = Path.GetFileNameWithoutExtension(f);
                        // Совпадает если имя == prefix или начинается с prefix + "("
                        return name == prefix || name.StartsWith(prefix + "(");
                    })
                    .OrderBy(f => f)
                    .ToList();

                log.LogInfo($"[MortalStrike] Группа {group}: найдено файлов {files.Count}");

                foreach (var file in files)
                    yield return LoadClip(file, group, log);
            }
        }

        private static IEnumerator LoadClip(string filePath, SoundGroup group, ManualLogSource log)
        {
            string uri = "file:///" + filePath.Replace('\\', '/');
            using (var req = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    log.LogError($"[MortalStrike] Ошибка загрузки {Path.GetFileName(filePath)}: {req.error}");
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                clip.name = Path.GetFileNameWithoutExtension(filePath);
                _clips[group].Add(clip);
                log.LogInfo($"[MortalStrike]   ✓ {clip.name} ({clip.length:F1}s)");
            }
        }

        // ─── Воспроизведение ─────────────────────────────────────────────────────

        /// <summary>Воспроизвести клип группы и подождать его окончания.</summary>
        public static IEnumerator Play(SoundGroup group)
        {
            // На headless нет аудиосистемы — пропускаем молча
            if (RuntimeContext.IsHeadless)
                yield break;

            var clip = PickClip(group);
            if (clip == null)
            {
                _log?.LogWarning($"[MortalStrike] Нет клипов для группы {group}.");
                yield break;
            }

            _log?.LogInfo($"[MortalStrike] 🔊 [{group}] → {clip.name} ({clip.length:F1}s)");

            try
            {
                var pos = GetLocalPlayerPos();
                var src = MonoBehaviourSingleton<BetterAudio>.Instance.PlayAtPoint(
                    pos, clip, 0f,
                    BetterAudio.AudioSourceGroupType.Nonspatial,
                    100, 1f, EOcclusionTest.None, null, false);
                src?.EnableStereo(true);
            }
            catch (Exception ex)
            {
                _log?.LogError($"[MortalStrike] Ошибка воспроизведения {group}: {ex.Message}");
                yield break;
            }

            yield return new WaitForSeconds(clip.length);
        }

        // ─── Ротация ─────────────────────────────────────────────────────────────

        private static AudioClip PickClip(SoundGroup group)
        {
            if (!_clips.TryGetValue(group, out var list) || list.Count == 0)
                return null;

            var used = _usedThisSession[group];

            // Доступные = все, кроме сыгравших в этом рейде
            var available = list.Where(c => !used.Contains(c.name)).ToList();

            // Если все уже сыграли — сбрасываем пул
            if (available.Count == 0)
            {
                used.Clear();
                available = new List<AudioClip>(list);
                _log?.LogInfo($"[MortalStrike] Ротация {group}: пул сброшен.");
            }

            var chosen = available[UnityEngine.Random.Range(0, available.Count)];
            used.Add(chosen.name);
            return chosen;
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────────

        private static Vector3 GetLocalPlayerPos()
        {
            try { return GamePlayerOwner.MyPlayer?.Position ?? Vector3.zero; }
            catch { return Vector3.zero; }
        }

        public static bool IsReady(SoundGroup group) =>
            _clips.TryGetValue(group, out var l) && l.Count > 0;
    }
}
