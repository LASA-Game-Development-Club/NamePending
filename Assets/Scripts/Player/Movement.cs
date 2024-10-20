using System.Collections.Generic;
using Scriptable_Objects.Upgrades;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Player
{
    public class Movement : MonoBehaviour
    {
        public bool inputBlocked;
        
        public float driftCorrection;
        public float speedLimit;
        public float acceleration;
        public float dodgeVelocity;
        public float dodgeDistance;
        public float dodgeCooldown;
        public float dodgeTimeDilation;
        public AnimationCurve dodgeTimeDilationCurve;
        public float afterImageSpacing;

        private Collider2D _collider;
        private SpriteRenderer _sprite;
        private TrailRenderer[] _trails;
        private CustomRigidbody2D _rigid;
        private Camera _camera;
    
        private Vector2 _preDodgeVel;
        private float _acceleration;

        private Vector2 _forwards;
        private struct OrbitState
        {
            float _orbitDistance;
            bool _isAntiClockwise;
            Vector2 _targetVel;
            Vector2 _orbitPoint;
        };
        private OrbitState _orbitState;

        private float _dodgeTimeLength;
        private readonly Timer _dodgeTimer = new();
        private readonly Timer _dodgeCooldownTimer = new();
        private readonly Timer _afterImageTimer = new();
        private Vector2 _dodgeDirection;
        private readonly List<GameObject> _afterImages = new();

        private Upgradeable _upgradeable;
        
        private void Start()
        {
            _camera = Camera.main;
            _rigid = GetComponent<CustomRigidbody2D>();
            _collider = GetComponent<Collider2D>();
            _sprite = GetComponent<SpriteRenderer>();
            _trails = GetComponentsInChildren<TrailRenderer>();
            _upgradeable = GetComponent<Upgradeable>();
        }

        private bool GetKey(KeyCode code)
        {
            return !inputBlocked && Input.GetKey(code);
        }
    
        private void FixedUpdate()
        {
            var velocity = _rigid.velocity;

            var tar = _camera.ScreenToWorldPoint(Input.mousePosition) - transform.position;
            _forwards = ((Vector2) tar).normalized;
            var curAngles = transform.rotation.eulerAngles;
            transform.rotation=Quaternion.Euler(curAngles.x, curAngles.y, -90+Mathf.Rad2Deg*Mathf.Atan2(tar.y, tar.x));

            if (_dodgeCooldownTimer.IsFinished && GetKey(KeyCode.Space))
            {
                var evt = new DodgeEvent
                {
                    dodgeCooldown = dodgeCooldown,
                    dodgeDistance = dodgeDistance,
                    dodgeVelocity = dodgeVelocity
                };
                if (_upgradeable) _upgradeable.HandleEvent(evt, null);

                _preDodgeVel = velocity;
                _dodgeTimeLength = evt.dodgeDistance / evt.dodgeVelocity;
                _dodgeTimer.Value = _dodgeTimeLength;
                _dodgeCooldownTimer.Value = evt.dodgeDistance / evt.dodgeVelocity + evt.dodgeCooldown;
                _dodgeDirection = new Vector2(_forwards.x, _forwards.y);
            }

            var wasDodging = !_dodgeTimer.IsFinished;
            _dodgeTimer.FixedUpdate();
            _dodgeCooldownTimer.FixedUpdate();
            _afterImageTimer.FixedUpdate();
            var dodging = !_dodgeTimer.IsFinished;

            
            // CustomRigidbody2D.Scaling = dodging ? dodgeTimeDilationCurve.Evaluate(1 - _dodgeTimer.Value/_dodgeTimeLength) : 1;
            CustomRigidbody2D.Scaling = dodging ? dodgeTimeDilation : 1;
            _collider.enabled = !dodging;
            foreach (var trail in _trails) trail.emitting = !dodging;
            _sprite.color = dodging ? new Color(1, 1, 1, 0.5f) : Color.white;
            
            if (dodging)
            {
                if (_afterImageTimer.IsFinished)
                {
                    _afterImageTimer.Value = afterImageSpacing / dodgeVelocity;
                    
                    var afterImage = new GameObject
                    {
                        transform =
                        {
                            position = transform.position,
                            rotation = transform.rotation
                        }
                    };
                    
                    var sprite = afterImage.AddComponent<SpriteRenderer>();
                    sprite.sprite = _sprite.sprite;
                    sprite.color = new Color(1, 1, 1, 0.225f);

                    _afterImages.Add(afterImage);
                }
                _rigid.velocity = _dodgeDirection * dodgeVelocity;
                return;
            }
            if (wasDodging)
            {
                _afterImages.ForEach(Destroy);
                _afterImages.Clear();
                
                velocity = _forwards * _preDodgeVel.magnitude;
            }
            
            if (GetKey(KeyCode.W))
            {
                var evt = new MoveEvent { speedLimit = speedLimit, acceleration = acceleration };
                if (_upgradeable) _upgradeable.HandleEvent(evt, null);
                
                var dv = evt.speedLimit * evt.speedLimit / 100;
                var eff = 1 / (1 + Mathf.Exp(velocity.sqrMagnitude / 100 - dv));
                if (velocity.sqrMagnitude > (evt.speedLimit + 5) * (evt.speedLimit + 5)){
                    _acceleration = 0;
                } else {
                    _acceleration = 10 * evt.acceleration * eff;
                }
            
                var vm = velocity.magnitude;
                velocity += driftCorrection * Time.fixedDeltaTime * Push(velocity, _forwards);
                velocity *= (.01f + vm) / (.01f+velocity.magnitude);

                /*if (GetKey("a"))
                {
                    _velocity = _or
                }*/

            } else if (GetKey(KeyCode.S)) {
                velocity *= Mathf.Pow(.2f, Time.fixedDeltaTime);
            } else {
                _acceleration = 0;
            }
            
            //forwards = new Vector2(-Mathf.Sin(Mathf.Deg2Rad * rigid.freezeRotation), Mathf.Cos(Mathf.Deg2Rad * rigid.freezeRotation));

            velocity += _forwards * (_acceleration * Time.fixedDeltaTime);
            velocity *= Mathf.Pow(.99f, Time.fixedDeltaTime);

            _rigid.velocity = velocity;
        }
    
        private static Vector2 Push(Vector2 target, Vector2 line)
        {
            var length = target.magnitude;
            var prod = (line.normalized.x * target.x + line.normalized.y * target.y);
            var kv = (prod + length) / 2; //Gives nicer turns, but less snapped in backwards feel
            //float kv = prod; //more control going backwards, but kinda confusing
            var proj = kv * line.normalized;

            var mid = (target.normalized + line.normalized).normalized * length/2;

            return proj - target + mid;
        }
    }
}
