﻿using UnityEngine;

namespace KRG
{
    [ExecuteAlways]
    public class FollowPlayerCharacterInEditor : MonoBehaviour
    {
        private void Awake()
        {
            if (Application.IsPlaying(this)) this.Dispose();
        }

        private void Update()
        {
            GameObjectBody pc = G.U.EditModePlayerCharacter;

            if (pc != null)
            {
                transform.position = pc.transform.position;
            }
        }
    }
}