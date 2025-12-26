using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player is spawned after dying.
    /// </summary>
    public class PlayerSpawn : Simulation.Event<PlayerSpawn>
    {
        private static readonly int Dead = Animator.StringToHash("dead");

        private PlatformerModel _model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            var player = _model.player;
            player.collider2d.enabled = true;
            player.controlEnabled = false;
            player.PlaySoundAndAnims(14);
            player.health.Increment();
            player.Teleport(_model.spawnPoint.transform.position);
            player.animator.SetBool(Dead, false);
            _model.virtualCamera.Follow = player.transform;
            _model.virtualCamera.LookAt = player.transform;
            Simulation.Schedule<EnablePlayerInput>(2f);
        }
    }
}