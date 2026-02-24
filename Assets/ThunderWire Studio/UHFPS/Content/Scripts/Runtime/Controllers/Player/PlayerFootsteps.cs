using UnityEngine;
using UHFPS.Runtime;

namespace UHFPS.Runtime
{
    public class PlayerFootsteps : PlayerComponent
    {
        public float FootstepInterval = 0.5f;

        private float lastFootstepTime;

        private void Update()
        {
            if (PlayerStateMachine.IsCurrent(PlayerStateMachine.WALK_STATE) ||
                PlayerStateMachine.IsCurrent(PlayerStateMachine.RUN_STATE))
            {
                if (Time.time - lastFootstepTime > FootstepInterval)
                {
                    SoundManager.Instance.PlaySound(transform.position, SoundManager.SoundType.Footsteps);
                    lastFootstepTime = Time.time;
                }
            }
        }
    }
}