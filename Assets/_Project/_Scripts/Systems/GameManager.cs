using System;
using System.Collections;
using System.Collections.Generic;
using Core.Patterns.Singleton;
using UnityEngine;

namespace Systems
{
    public class GameManager : Singleton<GameManager>
    {
        public float speed = 5f;
        private void Update()
        {
            HandleMove();
            HandleJump();
            transform.Translate(Vector3.forward * (speed * Time.deltaTime));
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

