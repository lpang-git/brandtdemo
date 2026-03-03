using Platformer.Core;
using Platformer.Model;

namespace Platformer.Gameplay
{
    /// <summary>
    /// This event is fired when user input should be enabled.
    /// </summary>
    public class EnablePlayerInput : Simulation.Event<EnablePlayerInput>
    {
        private PlatformerModel _model = Simulation.GetModel<PlatformerModel>();

        public override void Execute()
        {
            var player = _model.player;
            player.dyingState = false;
            player.controlEnabled = true;
            player.canUseGun = true;
        }
    }
}