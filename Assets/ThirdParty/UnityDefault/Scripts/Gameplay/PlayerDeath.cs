using Platformer.Core;
using Platformer.Model;

namespace Platformer.Gameplay
{
    /// <summary>
    /// Fired when the player has died.
    /// </summary>
    /// <typeparam name="PlayerDeath"></typeparam>
    public class PlayerDeath : Simulation.Event<PlayerDeath>
    {
        private PlatformerModel _model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            var player = _model.player;
            if (player.health.IsAlive)
            {
                player.health.Die();
                _model.virtualCamera.Follow = null;
                _model.virtualCamera.LookAt = null;
                player.collider2d.enabled = true;
                player.controlEnabled = false;
                player.canUseGun = false;
                Simulation.Schedule<PlayerSpawn>(2);
            }
        }
    }
}