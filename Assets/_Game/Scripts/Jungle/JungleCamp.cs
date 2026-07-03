using System.Collections.Generic;
using UnityEngine;
using Twisted3v3.Champions;
using Twisted3v3.Combat;

namespace Twisted3v3.Jungle
{
    /// <summary>
    /// Un emplacement de camp : fait apparaître ses monstres (construits par code
    /// depuis le <see cref="JungleCampData"/>), suit les survivants, gère le respawn
    /// et accorde le buff de clear au tueur (ou à son équipe) quand le camp est vidé.
    /// </summary>
    public sealed class JungleCamp : MonoBehaviour
    {
        [SerializeField] private JungleCampData _data;
        [Tooltip("Layer des monstres pour le ciblage (Units).")]
        [SerializeField] private int _monsterLayer = 10;

        private readonly List<JungleMonster> _alive = new();
        private float _respawnTimer;
        private bool _active;

        public JungleCampData Data => _data;

        private void Start()
        {
            if (_data == null) { Debug.LogWarning($"[JungleCamp] {name} sans JungleCampData."); return; }
            _respawnTimer = _data.InitialSpawnDelay;
        }

        private void Update()
        {
            if (_data == null || _active) return;
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f) SpawnCamp();
        }

        private void SpawnCamp()
        {
            _active = true;
            _alive.Clear();

            for (int i = 0; i < _data.MonsterCount; i++)
            {
                // Disposition en cercle autour du point de camp.
                float angle = _data.MonsterCount > 1 ? (i / (float)_data.MonsterCount) * Mathf.PI * 2f : 0f;
                Vector3 offset = new(Mathf.Cos(angle) * 1.6f, 0f, Mathf.Sin(angle) * 1.6f);

                var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                go.name = _data.DisplayName + "_Monster" + i;
                go.layer = _monsterLayer;
                go.transform.SetParent(transform, false);
                go.transform.position = transform.position + offset + Vector3.up;
                go.transform.localScale = Vector3.one * _data.MonsterScale;
                if (go.TryGetComponent<Renderer>(out var rend))
                    rend.material.color = _data.MonsterColor;
                go.AddComponent<Twisted3v3.VFX.MaterialCleanup>(); // libère la copie de matériau

                var monster = go.AddComponent<JungleMonster>();
                monster.Initialize(_data);
                monster.OnDied += HandleMonsterDied;
                _alive.Add(monster);
            }
        }

        private void HandleMonsterDied(JungleMonster monster, Champion killer)
        {
            _alive.Remove(monster);
            if (_alive.Count > 0) return;

            // Camp nettoyé → buff de clear + planification du respawn.
            GrantClearBuff(killer);
            _active = false;
            _respawnTimer = _data.RespawnTime;
        }

        private void GrantClearBuff(Champion killer)
        {
            if (killer == null || _data.ClearBuffs.Count == 0) return;

            if (_data.TeamWideBuff)
                TeamBuffRunner.Instance.ApplyToTeam(killer.Team, _data.ClearBuffs, _data.BuffDuration);
            else
                TeamBuffRunner.Instance.ApplyToChampion(killer, _data.ClearBuffs, _data.BuffDuration);

            Debug.Log($"[Jungle] {_data.DisplayName} nettoyé par {killer.name} → buff accordé.");
        }
    }
}
