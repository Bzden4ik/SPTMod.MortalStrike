using System.Collections.Generic;
using CommonAssets.Scripts.ArtilleryShelling;
using HarmonyLib;
using UnityEngine;

namespace MortalStrike
{
    public static class ShellingZoneFactory
    {
        private delegate void Method25Delegate(ServerShellingControllerClass instance, ref ArtilleryShellingZone zone);
        private static Method25Delegate _method25;

        static ShellingZoneFactory()
        {
            var mi = AccessTools.Method(typeof(ServerShellingControllerClass), "method_25");
            if (mi != null)
                _method25 = (Method25Delegate)System.Delegate.CreateDelegate(typeof(Method25Delegate), mi);
        }

        /// <summary>
        /// Если на карте нет бригад — инжектирует синтетическую.
        /// Вызывать один раз при инициализации.
        /// </summary>
        public static void EnsureBrigades(ServerShellingControllerClass ctrl, BepInEx.Logging.ManualLogSource log)
        {
            var cfg = ctrl.ArtilleryShellingMapConfiguration_0;

            if (cfg.Brigades != null && cfg.Brigades.Length > 0)
            {
                log.LogInfo($"[MortalStrike] Бригады на карте: {cfg.Brigades.Length} (оригинальные).");
                return;
            }

            // Ставим пушку далеко сбоку и высоко — это только стартовая позиция снаряда визуально
            var gunPos = new Vector3(500f, 300f, 500f);
            var gun = new ArtilleryGun { Position = gunPos };

            // Нужно минимум 2 элемента: [0] используется для стрельбы, [1] для method_20
            var brigade = new ArtilleryBrigade
            {
                ID = 0,
                ArtilleryGuns = new[] { gun, gun }
            };

            cfg.Brigades = new[] { brigade };
            ctrl.ArtilleryShellingMapConfiguration_0 = cfg;

            log.LogInfo("[MortalStrike] Синтетическая бригада инжектирована.");
        }

        /// <summary>
        /// Зона с длительным обстрелом ~60 секунд.
        /// 4 раунда по 6 выстрелов, паузы 2-4с между выстрелами, 6-10с между раундами.
        /// </summary>
        public static ArtilleryShellingZone CreateLong(
            ServerShellingControllerClass ctrl,
            Vector3 center,
            string id = "mortal_strike_long")
        {
            var zone = new ArtilleryShellingZone
            {
                ID                     = id + "_" + Random.Range(0, int.MaxValue),
                Center                 = center,
                GridStep               = new Vector2(8f, 8f),
                Points                 = new Vector2(3f, 3f),
                PointRadius            = 4f,
                Rotate                 = 0f,
                ShellingRounds         = 4,
                ShotCount              = 6,
                PauseBetweenRounds     = new Vector2(6f, 10f),
                PauseBetweenShots      = new Vector2(2f, 4f),
                PointsInShellings      = new Vector2(5f, 9f),
                ExplosionDistanceRange = new Vector2(2f, 5f),
                UseInCalledShelling    = true,
                IsActive               = true,
                ZoneBounds             = new List<Vector2>(),
                AlarmStages            = new GStruct150[]
                {
                    new GStruct150 { Value = new Vector2(0f, 60f) }
                }
            };

            if (_method25 != null)
                _method25(ctrl, ref zone);

            return zone;
        }

        public static ArtilleryShellingZone Create(
            ServerShellingControllerClass ctrl,
            Vector3 center,
            string id = "mortal_strike_zone")
        {
            var zone = new ArtilleryShellingZone
            {
                ID                     = id + "_" + Random.Range(0, int.MaxValue),
                Center                 = center,
                GridStep               = new Vector2(8f, 8f),
                Points                 = new Vector2(3f, 3f),
                PointRadius            = 4f,
                Rotate                 = 0f,
                ShellingRounds         = 1,
                ShotCount              = 3,
                PauseBetweenRounds     = new Vector2(3f, 5f),
                PauseBetweenShots      = new Vector2(1.5f, 3f),
                PointsInShellings      = new Vector2(3f, 6f),
                ExplosionDistanceRange = new Vector2(2f, 5f),
                UseInCalledShelling    = true,
                IsActive               = true,
                ZoneBounds             = new List<Vector2>(),
                AlarmStages            = new GStruct150[]
                {
                    new GStruct150 { Value = new Vector2(0f, 60f) }
                }
            };

            if (_method25 != null)
                _method25(ctrl, ref zone);

            return zone;
        }
    }
}
