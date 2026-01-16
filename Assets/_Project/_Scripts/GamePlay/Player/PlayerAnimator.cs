using DeepDig.Player;
using UnityEngine;

namespace TarodevController
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator _anim;
        [SerializeField] private SpriteRenderer _sprite;

        private PlayerController _controller;
        private Rigidbody2D _rb;

        public void Init(PlayerContext context)
        {
            _controller = context.Get<PlayerController>();
            _rb = _controller.GetComponent<Rigidbody2D>();
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
            if (Mathf.Abs(vx) > 0.01f) _sprite.flipX = vx < 0;
            _anim.SetFloat("IdleSpeed", Mathf.InverseLerp(0, 5, Mathf.Abs(vx)));
        }

        private void OnJumped() => _anim.SetTrigger("Jump");
        private void OnGroundedChanged(bool grounded, float impact) => _anim.SetBool("Grounded", grounded);
    }
}