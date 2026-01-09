using System.Collections;
using System.Collections.Generic;
using Platformer.Gameplay;
using UnityEngine;
using static Platformer.Core.Simulation;

namespace Platformer.Mechanics
{
    /// <summary>
    /// DeathZone components mark a collider which will schedule a
    /// PlayerEnteredDeathZone event when the player enters the trigger.
    /// </summary>
    public class DeathZone : MonoBehaviour
    {
        private static readonly int DeathBySpike = Animator.StringToHash("deathBySpike");
        
        [SerializeField] private int zoneType; // 1 = Void fall, 2 = Spike trap

        private void OnTriggerEnter2D(Collider2D other)
        {
            var p = other.gameObject.GetComponent<PlayerController>();
            var e = other.gameObject.GetComponent<EnemyController>();
            if (p)
            {
                switch (zoneType)
                {
                    case 1:
                        // Void
                        p.PlaySoundAndAnims(6);
                        break;
                    case 2:
                        // Trap
                        p.PlaySoundAndAnims(11);
                        break;
                }
            
                p.dyingState = true;
                p.fallingDramatically = false;
                var ev = Schedule<PlayerEnteredDeathZone>();
                ev.deathzone = this;
            } else if (e)
            {
                switch (zoneType)
                {
                    case 1:
                        // Void
                        Schedule<EnemyDeath>().enemy = e;
                        break;
                    case 2:
                        // Trap
                        e.animator.SetTrigger(DeathBySpike);
                        Schedule<EnemyDeath>().enemy = e;
                        break;
                }
            }
        }
    }
}