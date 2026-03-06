using System.Collections.Generic;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace MortalStrike
{
    /// <summary>
    /// Выбирает точки обстрела с учётом:
    /// — позиций живых игроков (высокий приоритет)
    /// — ключевых локаций карты (средний приоритет)
    /// — случайных точек как добивка (низкий приоритет)
    /// — минимальной дистанции между выбранными точками
    /// </summary>
    public static class TargetSelector
    {
        // Минимальное расстояние между двумя выбранными точками
        private const float MinDistanceBetweenTargets = 80f;

        // Радиус разброса вокруг игрока/ориентира
        private const float PlayerJitter   = 30f;
        private const float LandmarkJitter = 40f;

        // ─── Ключевые точки по картам ────────────────────────────────────────────
        // LocationId берётся из GameWorld.LocationId (нижний регистр)
        private static readonly Dictionary<string, Vector3[]> MapLandmarks =
            new Dictionary<string, Vector3[]>
            {
                // Берег
                ["shoreline"] = new[]
                {
                    new Vector3(220f,  0f,  170f),  // Санаторий восток
                    new Vector3(170f,  0f,  170f),  // Санаторий запад
                    new Vector3(-50f,  0f,  390f),  // Причал/Village
                    new Vector3(350f,  0f, -120f),  // АЗС
                    new Vector3( 80f,  0f, -350f),  // Посёлок на юге
                    new Vector3(-320f, 0f,  -50f),  // Рыбацкий посёлок
                },

                // Лес
                ["woods"] = new[]
                {
                    new Vector3( 150f, 0f,  200f),  // Лесопилка
                    new Vector3(-100f, 0f,  350f),  // USEC лагерь
                    new Vector3( 300f, 0f, -100f),  // Деревня Шахта
                    new Vector3(-200f, 0f, -200f),  // Заброшенный посёлок
                    new Vector3(  50f, 0f, -100f),  // Центр карты
                },

                // Таможня
                ["bigmap"] = new[]
                {
                    new Vector3( 180f, 0f,   80f),  // Общага
                    new Vector3(-100f, 0f,  200f),  // Новая АЗС
                    new Vector3( 300f, 0f,  -50f),  // Завод
                    new Vector3( -50f, 0f, -200f),  // Зерновой склад
                    new Vector3( 200f, 0f,  300f),  // Развязка
                },

                // Развязка
                ["interchange"] = new[]
                {
                    new Vector3(   0f, 0f,    0f),  // ТЦ центр
                    new Vector3( 150f, 0f,  100f),  // OLI
                    new Vector3(-150f, 0f,  100f),  // IDEA
                    new Vector3(   0f, 0f,  250f),  // Парковка
                    new Vector3( 200f, 0f, -150f),  // Техническая зона
                },

                // Резерв
                ["rezervbase"] = new[]
                {
                    new Vector3(   0f, 0f,    0f),  // Центр базы
                    new Vector3( 150f, 0f,  150f),  // Ангары
                    new Vector3(-150f, 0f,  100f),  // Бункер вход
                    new Vector3(  50f, 0f, -200f),  // Радиовышка
                    new Vector3(-100f, 0f, -100f),  // Хранилища
                },

                // Маяк
                ["lighthouse"] = new[]
                {
                    new Vector3( 300f, 0f,  100f),  // Вилла
                    new Vector3( 100f, 0f,  300f),  // Посёлок водников
                    new Vector3(-200f, 0f,  200f),  // Маяк
                    new Vector3(   0f, 0f, -100f),  // Техзона
                    new Vector3(-300f, 0f, -200f),  // Рогожин коттедж
                },

                // Улицы Таркова
                ["tarkovstreets"] = new[]
                {
                    new Vector3(   0f, 0f,    0f),  // Центр улиц
                    new Vector3( 200f, 0f,  150f),  // Торговый квартал
                    new Vector3(-150f, 0f,  200f),  // Жилой квартал
                    new Vector3( 100f, 0f, -200f),  // Площадь
                    new Vector3(-200f, 0f, -100f),  // Подвалы
                },

                // Завод
                ["factory4_day"] = new[]
                {
                    new Vector3(  30f, 0f,   30f),
                    new Vector3( -30f, 0f,  -30f),
                    new Vector3(  60f, 0f,  -20f),
                },
                ["factory4_night"] = new[]
                {
                    new Vector3(  30f, 0f,   30f),
                    new Vector3( -30f, 0f,  -30f),
                    new Vector3(  60f, 0f,  -20f),
                },

                // Лаборатория (подземная — обстрел всё равно по поверхности)
                ["laboratory"] = new[]
                {
                    new Vector3(   0f, 0f,    0f),
                    new Vector3( 100f, 0f,  100f),
                    new Vector3(-100f, 0f, -100f),
                },

                // Подземный паркинг
                ["sandbox"] = new[]
                {
                    new Vector3( 150f, 0f,  100f),
                    new Vector3(-100f, 0f,  200f),
                    new Vector3(  50f, 0f, -150f),
                },
                ["sandbox_high"] = new[]
                {
                    new Vector3( 150f, 0f,  100f),
                    new Vector3(-100f, 0f,  200f),
                    new Vector3(  50f, 0f, -150f),
                },
            };

        // ─── Основной метод ──────────────────────────────────────────────────────

        public static List<Vector3> SelectTargets(int count, BepInEx.Logging.ManualLogSource log)
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            string mapId = gameWorld?.LocationId?.ToLowerInvariant() ?? "";

            log.LogInfo($"[MortalStrike] Карта: '{mapId}', выбираем {count} точек.");

            // 1. Кандидаты от игроков (высокий приоритет)
            var playerCandidates  = GetPlayerCandidates(gameWorld);

            // 2. Кандидаты от ориентиров карты (средний приоритет)
            var landmarkCandidates = GetLandmarkCandidates(mapId);

            // 3. Отбираем с минимальной дистанцией между точками
            var selected = new List<Vector3>();

            // Сначала игроки, потом ориентиры, потом fallback-рандом
            TryAddCandidates(selected, playerCandidates,   count, "игрок");
            TryAddCandidates(selected, landmarkCandidates, count, "ориентир");

            // Добиваем случайными точками если не хватило
            int attempts = 0;
            while (selected.Count < count && attempts < 200)
            {
                attempts++;
                Vector2 c = Random.insideUnitCircle * 200f;
                var pos = new Vector3(c.x, 0f, c.y);
                if (IsFarEnough(pos, selected))
                    selected.Add(pos);
            }

            log.LogInfo($"[MortalStrike] Выбрано точек: {selected.Count} (игроков в пуле: {playerCandidates.Count}, ориентиров: {landmarkCandidates.Count})");
            return selected;
        }

        // ─── Вспомогательные ─────────────────────────────────────────────────────

        private static List<Vector3> GetPlayerCandidates(GameWorld gameWorld)
        {
            var result = new List<Vector3>();
            if (gameWorld == null) return result;

            foreach (var player in gameWorld.AllAlivePlayersList)
            {
                if (player == null || !player.HealthController.IsAlive) continue;

                Vector2 jitter = Random.insideUnitCircle * PlayerJitter;
                var pos = new Vector3(
                    player.Position.x + jitter.x,
                    0f,
                    player.Position.z + jitter.y);

                result.Add(pos);
            }

            // Перемешиваем чтобы не всегда первым шёл главный игрок
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }

        private static List<Vector3> GetLandmarkCandidates(string mapId)
        {
            var result = new List<Vector3>();

            if (!MapLandmarks.TryGetValue(mapId, out var landmarks))
                return result;

            foreach (var lm in landmarks)
            {
                Vector2 jitter = Random.insideUnitCircle * LandmarkJitter;
                result.Add(new Vector3(lm.x + jitter.x, 0f, lm.z + jitter.y));
            }

            // Перемешиваем ориентиры
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }

        private static void TryAddCandidates(
            List<Vector3> selected,
            List<Vector3> candidates,
            int maxCount,
            string label)
        {
            foreach (var pos in candidates)
            {
                if (selected.Count >= maxCount) break;
                if (IsFarEnough(pos, selected))
                    selected.Add(pos);
            }
        }

        private static bool IsFarEnough(Vector3 pos, List<Vector3> existing)
        {
            float minSqr = MinDistanceBetweenTargets * MinDistanceBetweenTargets;
            foreach (var e in existing)
            {
                float dx = pos.x - e.x;
                float dz = pos.z - e.z;
                if (dx * dx + dz * dz < minSqr)
                    return false;
            }
            return true;
        }
    }
}
