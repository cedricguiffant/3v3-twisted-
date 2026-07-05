using UnityEngine;
using Twisted3v3.Core;
using Twisted3v3.Champions;
using Twisted3v3.Minions;

namespace Twisted3v3.Jungle
{
    /// <summary>
    /// Autel des Âmes — objectif majeur central. Une équipe capture en restant dans
    /// le rayon ; la capture est gelée si les deux équipes sont présentes (contestée).
    /// À la prise, un buff d'équipe (issu du <see cref="JungleCampData"/>) est accordé,
    /// et les vagues de sbires de l'équipe capturante sont doublées tant qu'elle
    /// contrôle l'autel.
    /// </summary>
    public sealed class SoulAltar : MonoBehaviour
    {
        [SerializeField] private JungleCampData _data;
        [SerializeField] private float _captureRadius = 5f;
        [SerializeField] private float _captureTime = 4f;
        [SerializeField] private int _unitLayer = 10;
        [SerializeField] private Renderer _marker; // change de couleur selon le contrôle

        private static readonly Collider[] _buffer = new Collider[32];

        public Team ControllingTeam { get; private set; } = Team.None;
        private float _progress;          // 0..1 vers l'équipe capturante
        private Team _capturingTeam = Team.None;

        private void Update()
        {
            if (_data == null) return;

            Team present = ScanPresentTeam(out bool contested);

            if (contested) return; // gelé

            if (present == Team.None)
            {
                // Personne : la progression d'une capture en cours décline.
                if (_capturingTeam != Team.None)
                {
                    _progress -= Time.deltaTime / _captureTime;
                    if (_progress <= 0f) { _progress = 0f; _capturingTeam = Team.None; }
                }
                return;
            }

            if (present == ControllingTeam) return; // déjà contrôlé par eux

            // Une équipe progresse.
            if (present != _capturingTeam) { _capturingTeam = present; _progress = 0f; }
            _progress += Time.deltaTime / _captureTime;

            if (_progress >= 1f) Capture(present);
        }

        private Team ScanPresentTeam(out bool contested)
        {
            contested = false;
            bool blue = false, red = false;
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, _captureRadius, _buffer, 1 << _unitLayer);

            for (int i = 0; i < count; i++)
            {
                if (!_buffer[i].TryGetComponent<Champion>(out var champ) || champ.IsDead) continue;
                if (champ.Team == Team.Blue) blue = true;
                else if (champ.Team == Team.Red) red = true;
            }

            contested = blue && red;
            if (blue && !red) return Team.Blue;
            if (red && !blue) return Team.Red;
            return Team.None;
        }

        private void Capture(Team team)
        {
            ControllingTeam = team;
            _capturingTeam = Team.None;
            _progress = 0f;

            TeamBuffRunner.Instance.ApplyToTeam(team, _data.ClearBuffs, _data.BuffDuration);

            // Vagues de sbires doublées pour l'équipe qui contrôle l'autel ;
            // l'ancienne équipe (s'il y en a une) repasse à la normale.
            foreach (var spawner in Object.FindObjectsByType<MinionWaveSpawner>(FindObjectsSortMode.None))
                spawner.SetWaveMultiplier(spawner.Team == team ? 2 : 1);

            if (_marker != null)
                _marker.material.color = team == Team.Blue
                    ? new Color(0.2f, 0.4f, 1f) : new Color(1f, 0.3f, 0.3f);

            Debug.Log($"[Autel] Capturé par l'équipe {team} → buff d'équipe accordé.");
        }
    }
}
