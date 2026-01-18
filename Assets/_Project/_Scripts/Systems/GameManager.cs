using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns.Singleton;
using UnityEngine;

namespace Systems
{
    public class GameManager : Singleton<GameManager>
    {
        private void Update()
        {
            HandleJump();
            HandleMove();
        }

        private void HandleJump()
        {
            throw new NotImplementedException();
        }

        private void HandleMove()
        {
            throw new NotImplementedException();
        }
    }
}

