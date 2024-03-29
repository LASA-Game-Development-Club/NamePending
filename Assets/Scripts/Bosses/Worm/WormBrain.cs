using System;
using UnityEngine;

namespace Bosses.Worm
{
    public class WormBrain : MonoBehaviour
    {
        public GameObject head, middle, tail;
        public int middleLength;
        public SnakePathfinder pathfinder;

        public Vector3 targetPosition;
        public GameObject player;

        public float maxTurnAngleDeg;

        private GameObject[] _segments;
        private Rigidbody2D _headRigid;
        private float _speed;
        private Vector2 _targetMovePos;
        private float _segmentDist;
        private float _ouroborosRadius;
        private Vector2 _currdir;
        private float _swipe;
        public float swipetime;
        public float swipeangle;
        public float tailmomentum;

        public float pursueSpeed;
        public float pursueSnakiness;
        public float pursueTurnAngle;

        public float ouroborosSpeed;
        public float ouroborosSnakiness;
        public float ouroborosTurnAngle;

        public float wanderSpeed;
        public float wanderSnakiness;
        public float wanderTurnAngle;

        private float _tarSpeed;
        private float _tarSnakines;
        private float _tarTurnAngle;


        public enum MoveMode
        {
            Direct,
            Circle,
            Wander,
        } 
        public MoveMode _moveMode;

        private void Start()
        {
            _segments = new GameObject[middleLength + 2];

            _segments[0] = head;
            _segments[1] = middle;
            _segments[^1] = tail;

            var tailPos = tail.transform.localPosition;
            tailPos.x -= middle.transform.localScale.x * (middleLength - 1);
            tail.transform.localPosition = tailPos;

            float totallength = head.transform.localScale.x + tail.transform.localScale.x;

            for (var i = 2; i < middleLength + 1; i++)
            {
                var segment = (_segments[i] = Instantiate(middle)).transform;
                segment.SetParent(transform);

                var pos = _segments[i - 1].transform.localPosition;

                totallength += _segmentDist = segment.localScale.x;
                pos.x -= _segmentDist;
                segment.localPosition = pos;
            }

            _ouroborosRadius = totallength / (2 * Mathf.PI);
            
            _headRigid = head.GetComponent<Rigidbody2D>();
            _moveMode = MoveMode.Wander;
        }

        private void Update()
        {
            UpdateMovement();

            var _snakiness = pathfinder.snakeyness;

            _speed = Mathf.Clamp(_speed + 5 * Time.deltaTime * MathF.Sign(_tarSpeed - _speed), Mathf.Min(_speed, _tarSpeed), Mathf.Max(_speed, _tarSpeed));
            pathfinder.snakeyness = Mathf.Clamp(_snakiness + .1f * Time.deltaTime * MathF.Sign(_tarSnakines - _snakiness), Mathf.Min(_snakiness, _tarSnakines), Mathf.Max(_snakiness, _tarSnakines));
            maxTurnAngleDeg = Mathf.Clamp(maxTurnAngleDeg + 10 * Time.deltaTime * MathF.Sign(_tarTurnAngle - maxTurnAngleDeg), Mathf.Min(maxTurnAngleDeg, _tarTurnAngle), Mathf.Max(maxTurnAngleDeg, _tarTurnAngle));

            if (_swipe < -20) TailSwipe();
        }

        private void RippleSegments()
        {
            for (var i = 1; i < middleLength + 1; i++)
            {
                Vector3 nextSegmentPos = _segments[i - 1].transform.position;
                Vector3 currSegmentPos = _segments[i].transform.position;
                Vector3 prevSegmentPos = _segments[i + 1].transform.position;

                Vector3 curr2next = (nextSegmentPos - currSegmentPos).normalized;
                currSegmentPos = nextSegmentPos + -_segmentDist * curr2next;
                if(((Vector2)currSegmentPos).sqrMagnitude <= _ouroborosRadius * _ouroborosRadius)
                {
                    currSegmentPos += .1f * (Vector3)((Vector2)currSegmentPos).normalized;
                }
                _segments[i].transform.position = currSegmentPos;

                Vector3 prev2curr = (currSegmentPos - prevSegmentPos).normalized;
                Vector3 meanDir = .5f * (prev2curr + curr2next);

                var angle = Mathf.Atan2(meanDir.y, meanDir.x);
                _segments[i].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * angle);
            }

            { //Scope naming stuffs
                Vector3 nextSegmentPos = _segments[^2].transform.position;
                Vector3 currSegmentPos = _segments[^1].transform.position;

                Vector3 curr2next = (nextSegmentPos - currSegmentPos).normalized;
                _segments[^1].transform.position = currSegmentPos = nextSegmentPos + -_segmentDist * curr2next;

                var angle = Mathf.Atan2(curr2next.y, curr2next.x);
                _segments[^1].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * angle);
            }
        }
        private void RippleSegmentsWithSwipe(float swipeAngle, int startpos)
        {
            for (var i = 1; i < middleLength + 1; i++)
            {
                Vector3 nextSegmentPos = _segments[i - 1].transform.position;
                Vector3 currSegmentPos = _segments[i].transform.position;
                Vector3 prevSegmentPos = _segments[i + 1].transform.position;

                Vector3 curr2next = (nextSegmentPos - currSegmentPos).normalized;
                if (i >= startpos)
                {
                    Vector2 next2nnext = (_segments[i - 2].transform.position - nextSegmentPos).normalized;
                    Vector2 tarcurr2next = Util.UtilFuncs.Rot(next2nnext, swipeAngle);
                    curr2next = (tailmomentum * .03f / Time.deltaTime * curr2next + (Vector3)tarcurr2next).normalized;
                }
                currSegmentPos = nextSegmentPos + -_segmentDist * curr2next;
                if (((Vector2)currSegmentPos).sqrMagnitude <= _ouroborosRadius * _ouroborosRadius)
                {
                    currSegmentPos += .1f * (Vector3)((Vector2)currSegmentPos).normalized;
                }
                _segments[i].transform.position = currSegmentPos;

                Vector3 prev2curr = (currSegmentPos - prevSegmentPos).normalized;
                Vector3 meanDir = .5f * (prev2curr + curr2next);

                var angle = Mathf.Atan2(meanDir.y, meanDir.x);
                _segments[i].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * angle);
            }

            { //Scope naming stuffs
                Vector3 nextSegmentPos = _segments[^2].transform.position;
                Vector3 currSegmentPos = _segments[^1].transform.position;

                Vector3 curr2next = (nextSegmentPos - currSegmentPos).normalized;
                Vector2 next2nnext = (_segments[^3].transform.position - nextSegmentPos).normalized;
                Vector2 tarcurr2next = Util.UtilFuncs.Rot(next2nnext, swipeAngle);
                curr2next = (tailmomentum * .03f / Time.deltaTime * curr2next + (Vector3)tarcurr2next).normalized;

                _segments[^1].transform.position = currSegmentPos = nextSegmentPos + -_segmentDist * curr2next;

                var angle = Mathf.Atan2(curr2next.y, curr2next.x);
                _segments[^1].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * angle);
            }
        }
        private void TailSwipe()
        {
            if(_moveMode == MoveMode.Wander)
            {
                _swipe = 4 * swipetime;
            } else
            {
                _swipe = 0;
            }
            
        }

        private void UpdateMovement()
        {
            switch (_moveMode)
            {
                case MoveMode.Wander:
                    _tarSpeed = wanderSpeed;
                    _tarTurnAngle = wanderTurnAngle;
                    _tarSnakines = wanderSnakiness;
                    if ((head.transform.position - targetPosition).sqrMagnitude < 120)
                    {
                        while ((head.transform.position - targetPosition).sqrMagnitude < 4000)
                            targetPosition = UnityEngine.Random.Range(20, 70) * pathfinder.AngleToVector(UnityEngine.Random.Range(0, 6.28f));
                    }
                    break;
                case MoveMode.Direct:
                    _tarSpeed = pursueSpeed;
                    _tarTurnAngle = pursueTurnAngle;
                    _tarSnakines = pursueSnakiness;
                    targetPosition = player.transform.position;
                    break;
                case MoveMode.Circle:
                    _tarSpeed = ouroborosSpeed;
                    _tarTurnAngle = ouroborosTurnAngle;
                    _tarSnakines = ouroborosSnakiness;
                    targetPosition = Util.UtilFuncs.TangentPointOnCircleFromPoint(Vector2.zero, 20, head.transform.position);
                    break;
            }
            _targetMovePos = targetPosition;
            var dir = pathfinder.PathDirNorm(_segments[0].transform.position, _targetMovePos);
            Vector2 prevAngle = pathfinder.AngleToVector(Mathf.Deg2Rad * _segments[1].transform.rotation.eulerAngles.z);
            dir = pathfinder.ClampAngle(dir, prevAngle, Mathf.Deg2Rad * maxTurnAngleDeg);

            _currdir = dir = (.1f / Time.deltaTime * _currdir + dir).normalized;

            _headRigid.velocity = _speed * dir;

            var angle = Mathf.Atan2(dir.y, dir.x);
            _segments[0].transform.rotation = Quaternion.Euler(0, 0, Mathf.Rad2Deg * angle);

            if (_swipe > 2 * swipetime)
            {
                RippleSegmentsWithSwipe(swipeangle, 16);
            }
            else if (_swipe > swipetime)
            {
                RippleSegmentsWithSwipe(-swipeangle, 16);
            }
            else if (_swipe > 0)
            {
                RippleSegmentsWithSwipe(swipeangle, 16);
            }
            else
            {
                RippleSegments();
            }
            _swipe -= Time.deltaTime;
        }
    }
}