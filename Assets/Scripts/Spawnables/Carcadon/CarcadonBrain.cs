using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LevelSelect;
using Player;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;
using Random = UnityEngine.Random;

namespace Spawnables.Carcadon
{
    public class CarcadonBrain : MonoBehaviour
    {
        public float stealthOpacity, stackedStealthOpacity;
        public float opacityTime;
        public float accel, maxSpeed;
        
        public float stealthRadius, stealthScaling, stealthScaleMaxRad; // increase in max speed per unit closeness
        public float minRandStealthTime, maxRandStealthTime, minUnstealthDist;
        public float stealthAcc;
        public float stealthLowAccTime;

        public float attackAccel, attackSpeed;
        public float attackRadius;

        private EnemySpawner.EnemySpawner _enemySpawner;
        private IEnumerable<SpriteRenderer> _baseSpriteRenderers;
        private List<SpriteRenderer> _stackedSpriteRenderers = new();
        private ArmController[] _armControllers;
        private CustomRigidbody2D _rb;

        private enum Mode { Stealth, Rush, Attack }

        private Mode _mode = Mode.Stealth;
        private float _currSpeed;
        private Timer _opacityTimer = new();

        private CustomRigidbody2D _player;

        private float _maxHealth;
        private float _forceFieldRadius;
        private void Start()
        {
            _maxHealth = GetComponent<EnemyDamageable>().maxHealth;
            
            _opacityTimer.Value = opacityTime;
            _rb = GetComponent<CustomRigidbody2D>();
            _armControllers = GetComponentsInChildren<ArmController>();
            _enemySpawner = GetComponent<MiniBoss>().enemySpawner;
            _player = _enemySpawner.player.GetComponent<CustomRigidbody2D>();
            
            foreach (var ac in _armControllers) ac.Player = _player.gameObject;
            
            _forceFieldRadius = _enemySpawner.planet.transform.GetChild(0).transform.lossyScale.x / 2;
            
            StartCoroutine(Cutscene());
        }

        private int _opacityDir;
        private readonly Timer _stealthTimer = new();
        private readonly Timer _stealthAccTimer = new();

        private Vector2 _attackTarg;
        private bool _isOpaque = true;
        private float _timeGoingForPos;
        private void Update()
        {
            if (_inCutscene) return;
            
            var dir = _rb.velocity.normalized;
            
            if (_mode == Mode.Stealth)
            {
                var playerDist = (_player.transform.position - transform.position).magnitude;
                
                var distVal = stealthScaleMaxRad - playerDist;
                var trueMax = maxSpeed + (distVal > 0 ? distVal*distVal : 0) * stealthScaling;

                _stealthAccTimer.Update();
                var trueAccel = _stealthAccTimer.IsFinished ? accel : stealthAcc;
                _currSpeed = UtilFuncs.LerpSafe(_currSpeed, Mathf.Min(trueMax, _currSpeed+trueAccel), Time.deltaTime);

                var w = _currSpeed / stealthRadius; // angular velocity
                
                var pointOnCircle = transform.position.normalized * stealthRadius;

                var targPoint = Quaternion.Euler(0, 0, w * Mathf.Rad2Deg * Time.deltaTime) * pointOnCircle;

                dir = (targPoint - transform.position).normalized;
                
                _stealthTimer.Update();
                if (!_forceStealth && ((_stealthTimer.IsFinished && playerDist >= minUnstealthDist) || _enemySpawner.SpawnedEnemies.All(g => g == gameObject))) _mode = Mode.Rush;
            } else if (_mode == Mode.Rush)
            {
                _currSpeed = Mathf.Min(maxSpeed, _currSpeed + accel * Time.deltaTime);

                var target = (Vector2)_player.transform.position;
                
                var dist = target - (Vector2) transform.position;
                if (dist.magnitude < 19) SetVisualStealth(false);
                if (!_stealth && dist.magnitude < attackRadius) _mode = Mode.Attack;
                
                dir = dist.normalized;
            } else if (_mode == Mode.Attack)
            {
                _timeGoingForPos += Time.deltaTime;
                
                var dist = _attackTarg - (Vector2)transform.position;
                
                if (_timeGoingForPos > 2.5f || _attackTarg.sqrMagnitude == 0 || dist.magnitude < 3.5f)
                {
                    var radius = Random.Range(3, attackRadius);
                    var angle = Random.Range(0, 2 * Mathf.PI);

                    _attackTarg = radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) + (Vector2)_player.transform.position;

                    _timeGoingForPos = 0;
                }
                
                dir = dist.normalized;
                
                _currSpeed = UtilFuncs.LerpSafe(_currSpeed, Math.Min(attackSpeed - 2000/Mathf.Pow(dist.magnitude, 3) + 4*Mathf.Log(dist.magnitude), _currSpeed + attackAccel), Time.deltaTime);
            }

            if (_mode == Mode.Stealth) // don't want to make it snake, so shouldn't have an arbitrary min turn radius
            {
                _rb.velocity = UtilFuncs.LerpSafe(_rb.velocity, _currSpeed * dir, 5 * Time.deltaTime);
            }
            else
            {
                _rb.velocity = Vector3.RotateTowards(_rb.velocity, _currSpeed * dir, 1.5f * Time.deltaTime, 15 * Time.deltaTime);                
            }
            
            transform.rotation = Quaternion.Lerp(transform.rotation, UtilFuncs.RotFromNorm(_rb.velocity), 5 * Time.deltaTime);

            if (!_stealth && _opacityDir == 0)
            {
                if (((Vector2)transform.position).magnitude < _forceFieldRadius)
                {
                    if (_isOpaque) _opacityDir = -2;
                }
                else
                {
                    if (!_isOpaque) _opacityDir = 1;
                }
            }
            if (_opacityDir != 0)
            {
                _opacityTimer.Update(_opacityDir);
                
                foreach (var sr in _baseSpriteRenderers) sr.color = new Color(1, 1, 1, 1-(1-_opacityTimer.Progress) * (1-stealthOpacity));
                foreach (var sr in _stackedSpriteRenderers) sr.color = new Color(1, 1, 1, 1-(1-_opacityTimer.Progress) * (1-stackedStealthOpacity));

                if (_opacityTimer.IsFinished || _opacityTimer.Progress >= 1)
                {
                    _isOpaque = _opacityDir > 0;
                    _opacityDir = 0;
                }
            }
        }

        public void TakeDamage(float oldHealth, float newHealth)
        {
            if (Mathf.Min((int) (oldHealth / _maxHealth * LevelSelectData.ELITE_WAVES), LevelSelectData.ELITE_WAVES-1) != (int) (newHealth / _maxHealth * LevelSelectData.ELITE_WAVES))
            {
                _mode = Mode.Stealth;
                _rb.velocity = Vector2.zero; _currSpeed = 0;
                _stealthTimer.Value = Random.Range(minRandStealthTime, maxRandStealthTime);
                _stealthAccTimer.Value = stealthLowAccTime;
                SetVisualStealth(true);

                _enemySpawner.SpawnWave();
            }
        }
        
        private bool _stealth;
        private void SetVisualStealth(bool stealth)
        {
            if (_stealth == stealth) return;
            _stealth = stealth;
            _opacityDir = stealth ? -1 : 1;
            
            foreach (var ac in _armControllers)
            {
                if (stealth) ac.FoldClosed();
                else ac.FoldOpen();
            }
        }

        public string editorNote = "Cutscene Variables";
        public float fadeInTime, waitTime;
        public float camDistAbovePlayer;
        public float distAbovePlayer, distOffScreen;
        public float length, flyAcrossScreenTime;
        public float timeBetweenPasses;
        public float distAboveScreen;
        public float timeBeforeExpand;
        public float timeBeforeReveal;
        public float finalDistAbovePlayer;
        public float camExpandTime, camExpandAmt, camMoveAmt;
        public float pauseTime;
        public float headstartTime;
        private bool _inCutscene = true;
        private bool _forceStealth = true;
        private IEnumerator Cutscene()
        {
            yield return new WaitForEndOfFrame();

            _stackedSpriteRenderers.AddRange(transform.GetChild(0).GetComponentsInChildren<SpriteRenderer>());
            _stackedSpriteRenderers.AddRange(transform.GetChild(2).GetComponentsInChildren<SpriteRenderer>());
            _stackedSpriteRenderers.AddRange(transform.GetChild(3).GetComponentsInChildren<SpriteRenderer>());
            _stackedSpriteRenderers.AddRange(transform.GetChild(4).GetComponentsInChildren<SpriteRenderer>());
            _stackedSpriteRenderers.AddRange(transform.GetChild(5).GetComponentsInChildren<SpriteRenderer>());
            _baseSpriteRenderers = GetComponentsInChildren<SpriteRenderer>().Where(sr=>!_stackedSpriteRenderers.Contains(sr));
            
            
            _player.GetComponent<Shoot>().enabled = false;
            _player.GetComponent<Movement>().inputBlocked = true;
            
            // set up camera
            var cam = Camera.main;
            var camFp = cam!.GetComponent<FollowPlayer>();
            camFp.enabled = false;
            cam.transform.position = (Vector3) (Vector2) _player.transform.position + new Vector3(camDistAbovePlayer, 0, cam.transform.position.z);
            cam.transform.rotation = Quaternion.Euler(0, 0, -90);
            cam.orthographicSize = camFp.baseSize;
            for (var i = 0; i < _enemySpawner.fadeIn.transform.parent.childCount - 1; i++)
            {
                _enemySpawner.fadeIn.transform.parent.GetChild(i).gameObject.SetActive(false);
            }

            // set up carcadon
            foreach (var sr in _baseSpriteRenderers) sr.color = new Color(1, 1, 1, stealthOpacity);
            foreach (var sr in _stackedSpriteRenderers) sr.color = new Color(1, 1, 1, stackedStealthOpacity);
            foreach (var ac in _armControllers)
            {
                ac.SetFolded(true);
                ac.hasAttack = false;
            }
            transform.rotation = Quaternion.Euler(0, 0, 180);
            transform.position = new Vector3(_player.transform.position.x+distAbovePlayer, _player.transform.position.y + cam.orthographicSize * cam.aspect + distOffScreen, transform.position.z);
            
            // fade in
            var fadeImg = _enemySpawner.fadeIn.GetComponent<Image>();
            for (float t = 0; t < fadeInTime; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                fadeImg.color = new Color(fadeImg.color.r, fadeImg.color.g, fadeImg.color.b, Mathf.SmoothStep(1, 0, t / fadeInTime));
            }
            _enemySpawner.fadeIn.SetActive(false);
            
            // hold
            yield return new WaitForSeconds(waitTime);
            
            // move right
            var origPos = transform.position.y;
            var targPos = origPos - (2 * cam.orthographicSize * cam.aspect + length + 2*distOffScreen);
            for (float t = 0; t < flyAcrossScreenTime; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                transform.position = new Vector3(transform.position.x,
                    Mathf.SmoothStep(origPos, targPos, t / flyAcrossScreenTime), transform.position.z);
            }

            // slide down
            for (float t = 0; t < timeBetweenPasses; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                cam.transform.position = new Vector3(_player.transform.position.x + Mathf.SmoothStep(camDistAbovePlayer, -camDistAbovePlayer, t/timeBetweenPasses),
                    cam.transform.position.y, cam.transform.position.z);
            }
            
            // move left
            transform.RotateAround(transform.position + length/2 * Vector3.up, Vector3.forward, 180); // so tail doesn't have to fix itself
            transform.position = new Vector3(_player.transform.position.x-distAbovePlayer, _player.transform.position.y - cam.orthographicSize * cam.aspect - distOffScreen, transform.position.z);
            origPos = transform.position.y;
            targPos = origPos + (2 * cam.orthographicSize * cam.aspect + length + 2*distOffScreen);
            for (float t = 0; t < flyAcrossScreenTime; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                transform.position = new Vector3(transform.position.x,
                    Mathf.SmoothStep(origPos, targPos, t / flyAcrossScreenTime), transform.position.z);
            }
            
            // hold
            yield return new WaitForSeconds(timeBeforeExpand);

            // slide up
            transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z)
                                 + (cam.orthographicSize + distAboveScreen + 2*camDistAbovePlayer) * Vector3.right;
            transform.rotation = Quaternion.Euler(0, 0, 90);
            origPos = transform.position.x;
            targPos = _player.transform.position.x + finalDistAbovePlayer;
            var camOrigPos = cam.transform.position.x;
            var camTargPos = camOrigPos + 2*camDistAbovePlayer + camMoveAmt;
            var origSize = cam.orthographicSize;
            var revealed = false;
            for (float t = 0; t < camExpandTime; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                if (t >= timeBeforeReveal)
                {
                    if (!revealed)
                    {
                        revealed = true;
                        foreach (var ac in _armControllers) ac.FoldOpen();
                    }
                    foreach (var sr in _baseSpriteRenderers) sr.color = new Color(1, 1, 1, 1-(1-(t-timeBeforeReveal)/opacityTime) * (1-stealthOpacity));
                    foreach (var sr in _stackedSpriteRenderers) sr.color = new Color(1, 1, 1, 1-(1-(t-timeBeforeReveal)/opacityTime) * (1-stackedStealthOpacity));
                }
                transform.position = new Vector3(Mathf.SmoothStep(origPos, targPos, t/camExpandTime),
                    transform.position.y, transform.position.z);
                cam.transform.position = new Vector3(Mathf.SmoothStep(camOrigPos, camTargPos, t / camExpandTime),
                        cam.transform.position.y, cam.transform.position.z);
                cam.orthographicSize = Mathf.SmoothStep(origSize, origSize+camExpandAmt, t / camExpandTime);
            }

            // hold
            yield return new WaitForSeconds(pauseTime);
            
            _mode = Mode.Stealth;
            _stealthTimer.Value = Random.Range(minRandStealthTime, maxRandStealthTime);
            SetVisualStealth(true);
            _inCutscene = false;

            // hold
            yield return new WaitForSeconds(headstartTime);
            
            for (var i = 0; i < _enemySpawner.fadeIn.transform.parent.childCount - 1; i++)
            {
                _enemySpawner.fadeIn.transform.parent.GetChild(i).gameObject.SetActive(true);
            }
            _forceStealth = false;
            for (var ac = 0; ac < 2; ac++) _armControllers[ac].hasAttack = true;
            _enemySpawner.SpawnWave();
            _player.GetComponent<Shoot>().enabled = true;
            _player.GetComponent<Movement>().inputBlocked = false;
            camFp.enabled = true;
        }
    }
}