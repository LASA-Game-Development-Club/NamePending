using UnityEngine;
using System.Collections.Generic;
using ProgressBars;

namespace Spawnables
{
    public class Damageable : MonoBehaviour, IDamageable
    {
        public GameObject healthBarPrefab;
        public DamageResistances dmgRes;

        public bool IsDead => Health <= 0;
        
        protected virtual float Health { get; set; }
        protected virtual float MaxHealth { get; }
        
        protected ProgressBar _healthBar;
        
        public void Start()
        {
            dmgRes.Ready();
            _healthBar = Instantiate(healthBarPrefab).GetComponent<ProgressBar>();
            _healthBar.transform.SetParent(transform, true);
        }

        public virtual void Damage(float damage, IDamageable.DmgType dmgType, float reduceMod = 1f)
        {
            damage -= reduceMod * dmgRes.dmgReduce[(int)dmgType];
            damage *= dmgRes.dmgMod[(int)dmgType];
            Health -= damage>0 ? damage : 0;
        
            if (IsDead)
            {
                OnDeath();
                Destroy(gameObject);
            }

            _healthBar.UpdatePercentage(Health, MaxHealth);
        }
        
        protected virtual void OnDeath() { }
    }
}