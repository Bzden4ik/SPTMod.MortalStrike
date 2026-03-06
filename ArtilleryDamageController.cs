using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.GlobalEvents.ArtilleryShellingEcents;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using UnityEngine;

namespace MortalStrike
{
    /// <summary>
    /// Работает на headless. Подписывается на каждый взрыв снаряда и применяет
    /// урон вручную с проверкой укрытия через raycast вверх от позиции игрока.
    /// </summary>
    public class ArtilleryDamageController : MonoBehaviour
    {
        // ─── Настройки урона (читаются из конфига) ───────────────────────────────
        private float MaxDamageRadius  => Plugin.Cfg.DamageRadius.Value;
        private float LethalRadius     => Plugin.Cfg.DamageLethalRadius.Value;
        private float MaxDamage        => Plugin.Cfg.DamageMax.Value;
        private float MinDamage        => Plugin.Cfg.DamageMin.Value;
        private const float CoverCheckDistance = 4f;
        private const float ArmorDamage        = 80f;

        // Маска: Terrain + HighPolyCollider (здания, стены, крыши)
        private static readonly int CoverMask =
            LayerMask.GetMask("Terrain", "HighPolyCollider");

        // Маска: игровые коллайдеры игроков
        private static readonly int PlayerOverlapMask =
            LayerMask.GetMask("Player", "Bot", "DeadBody");

        private static readonly Collider[] _overlapBuffer = new Collider[64];

        // Части тела которые получают урон от взрыва
        private static readonly EBodyPart[] _bodyParts =
        {
            EBodyPart.Chest,
            EBodyPart.Head,
            EBodyPart.LeftLeg,
            EBodyPart.RightLeg,
            EBodyPart.LeftArm,
            EBodyPart.RightArm,
        };

        private static readonly float[] _bodyPartWeights =
        {
            0.35f, // Chest
            0.20f, // Head
            0.15f, // LeftLeg
            0.15f, // RightLeg
            0.08f, // LeftArm
            0.08f, // RightArm
        };

        private ManualLogSource _log;
        private Action _unsubExplosion;

        public void Initialize(ManualLogSource log)
        {
            _log = log;

            // Пробуем подписаться на событие взрыва (работает в соло и возможно в Fika).
            // На headless это событие может не стрелять — fallback через ApplyAreaDamage() из Event-классов.
            var handler = GlobalEventHandlerClass.Instance;
            if (handler != null)
            {
                _unsubExplosion = handler.SubscribeOnEvent<ShellingProjectileExplosionEvent>(OnExplosion);
                log.LogInfo("[MortalStrike] ArtilleryDamageController: подписан на взрывы (ShellingProjectileExplosionEvent).");
            }
            else
            {
                log.LogWarning("[MortalStrike] ArtilleryDamageController: GlobalEventHandler недоступен — урон только через прямой вызов ApplyAreaDamage.");
            }
        }

        /// <summary>
        /// Публичный вход для прямого нанесения урона (вызывается из EventOne/EventTwo на headless,
        /// когда ShellingProjectileExplosionEvent не стреляет).
        /// </summary>
        public void ApplyAreaDamage(Vector3 center)
        {
            OnExplosion(new ShellingProjectileExplosionEvent { Center = center });
        }        private void OnExplosion(ShellingProjectileExplosionEvent evt)
        {
            Vector3 center = evt.Center;

            var gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            int count = Physics.OverlapSphereNonAlloc(
                center, MaxDamageRadius, _overlapBuffer,
                LayerMaskClass.GrenadeAffectedMask);

            var processed = new HashSet<string>();

            for (int i = 0; i < count; i++)
            {
                var collider = _overlapBuffer[i];
                if (collider == null) continue;

                var bridge = gameWorld.GetAlivePlayerBridgeByCollider(collider);
                if (bridge == null) continue;

                var player = bridge.iPlayer as Player;
                if (player == null || !player.HealthController.IsAlive) continue;
                if (!processed.Add(player.ProfileId)) continue;

                float distance = Vector3.Distance(player.Position, center);
                if (distance > MaxDamageRadius) continue;

                Vector3 headPos = player.PlayerBones?.Head?.position ?? player.Position + Vector3.up * 1.7f;
                if (IsUnderCover(headPos, center))
                {
                    _log.LogInfo($"[MortalStrike] Урон: {player.Profile?.Nickname} под укрытием — пропускаем.");
                    continue;
                }

                float damage = CalculateDamage(distance);
                SendOrApplyDamage(player, damage);

                _log.LogInfo($"[MortalStrike] Урон: {player.Profile?.Nickname} d={distance:F1}m → {damage:F0} hp");
            }
        }

        private void SendOrApplyDamage(Player player, float damage)
        {
            // Боты управляются на headless — применяем урон локально всегда.
            if (player.IsAI)
            {
                ApplyDamageLocal(player, damage);
                return;
            }

            // Живой игрок на headless (Fika): здоровье авторитетно на клиенте — шлём пакет.
            if (RuntimeContext.IsHeadless && Plugin.NetworkInstance != null)
            {
                Plugin.NetworkInstance.SendDamageToClient(player.ProfileId, damage);
                return;
            }

            // Соло или сам клиент: применяем локально.
            ApplyDamageLocal(player, damage);
        }

        /// <summary>
        /// Возвращает true если над головой игрока есть крыша/потолок в пределах CoverCheckDistance,
        /// ИЛИ взрыв не имеет прямой видимости до игрока (стена между ними).
        /// </summary>
        private bool IsUnderCover(Vector3 headPos, Vector3 explosionPos)
        {
            // 1. Потолок над головой
            if (Physics.Raycast(headPos, Vector3.up, CoverCheckDistance, CoverMask))
                return true;

            // 2. Прямая видимость от взрыва до головы (через HighPolyCollider — стены/полы)
            Vector3 dir = headPos - explosionPos;
            float dist = dir.magnitude;
            if (Physics.Raycast(explosionPos, dir.normalized, dist - 0.3f, CoverMask))
                return true;

            return false;
        }

        private float CalculateDamage(float distance)
        {
            if (distance <= LethalRadius)
                return MaxDamage;

            float t = Mathf.InverseLerp(MaxDamageRadius, LethalRadius, distance);
            return Mathf.Lerp(MinDamage, MaxDamage, t);
        }

        private void ApplyDamageLocal(Player player, float totalDamage)
        {
            try
            {
                var healthCtrl = player.ActiveHealthController;
                if (healthCtrl == null) return;

                var damageInfo = new DamageInfoStruct
                {
                    DamageType        = EDamageType.Artillery,
                    ArmorDamage       = ArmorDamage,
                    PenetrationPower  = 50f,
                    StaminaBurnRate   = 1f,
                    Direction         = Vector3.down,
                    Player            = null,
                    IsForwardHit      = true,
                    HitPoint          = player.Position,
                    HitNormal         = Vector3.up,
                };

                for (int i = 0; i < _bodyParts.Length; i++)
                {
                    float partDamage = totalDamage * _bodyPartWeights[i];
                    if (partDamage < 1f) continue;
                    damageInfo.Damage = partDamage;
                    healthCtrl.ApplyDamage(_bodyParts[i], partDamage, damageInfo);
                }
            }
            catch (Exception ex)
            {
                _log.LogError($"[MortalStrike] Ошибка ApplyDamage: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            try { _unsubExplosion?.Invoke(); } catch { }
        }
    }
}
