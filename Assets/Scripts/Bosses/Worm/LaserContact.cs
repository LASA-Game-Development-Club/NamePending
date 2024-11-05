using System;
using Spawnables;
using Spawnables.Player;
using UnityEngine;

namespace Bosses.Worm
{
    public class LaserContact : MonoBehaviour
    {
        public float damage;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerDamageable>();
            if (!player) return;
            
            player.Damage(damage, IDamageable.DmgType.Physical);
        }
    }
}