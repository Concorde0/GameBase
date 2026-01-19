using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns.Singleton;
using UnityEngine;

namespace Systems
{
    public class GameManager : Singleton<GameManager>
    {
        public bool isSprinting = false;
        private void Update()
        {
            HandleMove();
            HandleJump();
            if (isSprinting)
            {
                transform.Translate(Vector3.forward * (10f * Time.deltaTime));
            }
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

