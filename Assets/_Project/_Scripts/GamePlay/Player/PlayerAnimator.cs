using UnityEngine;

namespace GameBase.GamePlay
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _anim;
        [SerializeField] private SpriteRenderer _sprite;
        [SerializeField] private float _flipDuration = 0.15f; // seconds for the flip transition

        private PlayerController _controller;
        private Rigidbody2D _rb;
        // runtime flip state
        private float _currentYAngle;
        private float _targetYAngle;
        private bool _facingRight = true;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _rb = _controller.GetComponent<Rigidbody2D>();

            // initialize rotation-based flip state from the sprite transform
            if (_sprite != null)
            {
                _currentYAngle = _sprite.transform.localEulerAngles.y;
                // normalize to 0 or 180 for consistent behavior
                _currentYAngle = Mathf.Abs(Mathf.DeltaAngle(0, _currentYAngle)) < 90f ? 0f : 180f;
                _targetYAngle = _currentYAngle;
                _facingRight = Mathf.Approximately(_currentYAngle, 0f);
                _sprite.transform.localEulerAngles = new Vector3(0f, _currentYAngle, 0f);
            }
        }


        private void OnEnable()
        {
            if (_controller != null)
            {
                _controller.Jumped += OnJumped;
                _controller.GroundedChanged += OnGroundedChanged;
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                _controller.Jumped -= OnJumped;
                _controller.GroundedChanged -= OnGroundedChanged;
            }
        }

        private void Update()
        {
            if (_rb == null) return;
            var vx = _rb.velocity.x;
            
            
            // determine desired facing from velocity (keep the existing threshold)
            if (Mathf.Abs(vx) > 0.01f)
            {
                var wantRight = vx > 0f;
                if (wantRight != _facingRight)
                {
                    _facingRight = wantRight;
                    _targetYAngle = _facingRight ? 0f : 180f;
                }
            }

            // smoothly move current angle toward target angle
            if (_sprite != null && !Mathf.Approximately(_currentYAngle, _targetYAngle))
            {
                // rotation speed so it completes in ~_flipDuration seconds (use MoveTowardsAngle for shortest path)
                var rotationSpeed = 180f / Mathf.Max(0.0001f, _flipDuration);
                _currentYAngle = Mathf.MoveTowardsAngle(_currentYAngle, _targetYAngle, rotationSpeed * Time.deltaTime);
                _sprite.transform.localEulerAngles = new Vector3(0f, -_currentYAngle, 0f);
            }
            _anim.SetFloat("IdleSpeed", Mathf.InverseLerp(0, 5, Mathf.Abs(vx)));
        }

        private void OnJumped() => _anim.SetTrigger("Jump");
        private void OnGroundedChanged(bool grounded, float impact) => _anim.SetBool("Grounded", grounded);
    }
}