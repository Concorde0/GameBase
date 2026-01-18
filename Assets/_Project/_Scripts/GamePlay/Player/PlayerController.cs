using System;
using GamePlay;
using UnityEngine;

namespace GamePlay
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private ScriptableStats _stats;
        
        private PlayerInputSystem _input;
        
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        

        private Vector2 _processedMove; 
        private bool _jumpToConsume;
        private bool _jumpHeld;
        
        private Vector2 _velocity;
        private bool _grounded;
        private float _time;
        private float _frameLeftGrounded = float.MinValue;
        private bool _coyoteUsable;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private float _timeJumpWasPressed;

        public Vector2 FrameInput => _input.Current.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        private bool _cachedQueryStartInColliders;

        public void Init(PlayerContext context)
        {
            _input = context.Get<PlayerInputSystem>();
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

            
        }

        private void Update()
        {
            _time += Time.deltaTime;

            if (_input == null) return;

            // 拉取 FrameInput（采样层）
            var fi = _input.Current;

            // 处理 deadzone / snap
            Vector2 processed = fi.Move;
            if (_stats != null && _stats.snapInput)
            {
                processed.x = Mathf.Abs(processed.x) < _stats.horizontalDeadZoneThreshold ? 0 : Mathf.Sign(processed.x);
                processed.y = Mathf.Abs(processed.y) < _stats.verticalDeadZoneThreshold ? 0 : Mathf.Sign(processed.y);
            }
            else if (_stats != null)
            {
                processed.x = Mathf.Abs(processed.x) < _stats.horizontalDeadZoneThreshold ? 0 : processed.x;
                processed.y = Mathf.Abs(processed.y) < _stats.verticalDeadZoneThreshold ? 0 : processed.y;
            }

            _processedMove = processed;

            // 按下那一帧会被 latch，供 FixedUpdate 的跳跃缓冲使用
            if (fi.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }

            _jumpHeld = fi.JumpHeld;
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            
            if (_jumpToConsume)
            {
                _bufferedJumpUsable = true;
            }

            HandleJump();
            HandleHorizontal();
            HandleGravity();
            
            _rb.velocity = _velocity;
            
            _jumpToConsume = false;
        }

        #region Collisions

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            bool groundHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0f, Vector2.down, _stats.grounderDistance, ~_stats.playerLayer);
            bool ceilingHit = Physics2D.CapsuleCast(_col.bounds.center, _col.size, _col.direction, 0f, Vector2.up, _stats.grounderDistance, ~_stats.playerLayer);

            if (ceilingHit) _velocity.y = Mathf.Min(0, _velocity.y);

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_velocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.jumpBuffer && _timeJumpWasPressed > 0;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.coyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_jumpHeld && _rb.velocity.y > 0) _endedJumpEarly = true;

            if (!HasBufferedJump) return;

            if (_grounded || CanUseCoyote)
            {
                ExecuteJump();
            }
            
            _bufferedJumpUsable = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0f;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _velocity.y = _stats.jumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleHorizontal()
        {
            if (Mathf.Abs(_processedMove.x) < 1e-5f)
            {
                var decel = _grounded ? _stats.groundDeceleration : _stats.airDeceleration;
                _velocity.x = Mathf.MoveTowards(_velocity.x, 0f, decel * Time.fixedDeltaTime);
            }
            else
            {
                var target = _processedMove.x * _stats.maxSpeed;
                _velocity.x = Mathf.MoveTowards(_velocity.x, target, _stats.acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _velocity.y <= 0f)
            {
                _velocity.y = _stats.groundingForce;
            }
            else
            {
                var gravity = _stats.fallAcceleration;
                if (_endedJumpEarly && _velocity.y > 0) gravity *= _stats.jumpEndEarlyGravityModifier;
                _velocity.y = Mathf.MoveTowards(_velocity.y, -_stats.maxFallSpeed, gravity * Time.fixedDeltaTime);
            }
        }

        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign ScriptableStats to PlayerController", this);
        }
#endif
    }
}
