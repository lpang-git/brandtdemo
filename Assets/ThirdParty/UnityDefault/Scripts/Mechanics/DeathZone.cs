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
        [SerializeField] private int zoneType; // 1 = Void fall, 2 = Spike trap

        private void OnTriggerEnter2D(Collider2D collider)
        {
            var p = collider.gameObject.GetComponent<PlayerController>();
            if (p == null) return;

            switch (zoneType)
            {
                case 1:
                    p.PlaySoundAndAnims(6);
                    break;
                case 2:
                    p.PlaySoundAndAnims(11);
                    break;
            }
            
            var ev = Schedule<PlayerEnteredDeathZone>();
            ev.deathzone = this;
        }
    }
}