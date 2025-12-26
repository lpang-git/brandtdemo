using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using UnityEngine;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player has died.
    /// </summary>
    /// <typeparam name="PlayerDeath"></typeparam>
    public class PlayerDeath : Simulation.Event<PlayerDeath>
    {
        private static readonly int Hurt = Animator.StringToHash("hurt");
        private static readonly int Dead = Animator.StringToHash("dead");

        private PlatformerModel _model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            var player = _model.player;
            if (player.health.IsAlive)
            {
                player.health.Die();
                _model.virtualCamera.Follow = null;
                _model.virtualCamera.LookAt = null;
                // player.collider.enabled = false;
                player.controlEnabled = false;
                player.animator.SetTrigger(Hurt);
                player.animator.SetBool(Dead, true);
                Simulation.Schedule<PlayerSpawn>(2);
            }
        }
    }
}