using UnityEngine;
using UnityEngine.InputSystem;

namespace DeepDig.System.Input
{
    public struct FrameInput
    {
        public bool JumpDown;   // 仅按下那一帧为 true（采样层）
        public bool JumpHeld;   // 按住期间为 true
        public Vector2 Move;    // 原始轴输入 (-1..1)
    }

    [DefaultExecutionOrder(-50)]
    public class PlayerInputSystem : MonoBehaviour
    {
        private PlayerInput _input;

        private Vector2 _moveDir;
        private bool _jumpDown;
        private bool _jumpHeld;

        public FrameInput Current { get; private set; }

        private void Awake()
        {
            _input = new PlayerInput();

            _input.GamePlay.Move.performed += ctx =>
            {
                _moveDir = ctx.ReadValue<Vector2>();
                
            };
            _input.GamePlay.Move.canceled += _ => _moveDir = Vector2.zero;

            _input.GamePlay.Jump.performed += _ =>
            {
                _jumpDown = true;
                _jumpHeld = true;
            };
            _input.GamePlay.Jump.canceled += _ => _jumpHeld = false;
        }

        private void OnEnable() => _input.Enable();
        private void OnDisable() => _input.Disable();

        private void Update()
        {
            Current = new FrameInput
            {
                Move = _moveDir,
                JumpDown = _jumpDown,
                JumpHeld = _jumpHeld
            };
            
            _jumpDown = false;
        }
    }
}