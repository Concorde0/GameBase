using DeepDig.Player;
using DeepDig.System.Input;
using DeepDig.System.Param;
using TarodevController;
using UnityEngine;

namespace DeepDig.Player
{
    public class PlayerInitializer : MonoBehaviour
    {
        private PlayerContext _context;

        private void Awake()
        {
            _context = new PlayerContext();
            var playerParams = new PlayerParams();

            var inputSystem = GetComponent<PlayerInputSystem>();
            var anim = GetComponent<PlayerAnimator>();
            var playerController = GetComponent<PlayerController>();


            _context.Register(inputSystem);
            _context.Register(playerParams);
            _context.Register(anim);
            _context.Register(playerController);
 
            playerController.Init(_context);
        }
    }

}