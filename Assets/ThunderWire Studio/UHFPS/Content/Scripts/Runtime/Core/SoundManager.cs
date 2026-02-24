using System;
using UnityEngine;
using UnityEngine.Events;

namespace UHFPS.Runtime
{
    public class SoundManager : Singleton<SoundManager>
    {
        public enum SoundType
        {
            Gunshot,
            Footsteps,
            Other
        }

        [Serializable]
        public class SoundEvent : UnityEvent<Vector3, SoundType> { }

        public SoundEvent OnSoundPlayed = new SoundEvent();

        public void PlaySound(Vector3 position, SoundType type)
        {
            OnSoundPlayed.Invoke(position, type);
        }
    }
}