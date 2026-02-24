using UnityEngine;

namespace UHFPS.Runtime
{
    public class NPCWeaponHandler : MonoBehaviour
    {
        [Tooltip("Сокет для оружия в состоянии idle/walk/run (например, для спокойных анимаций)")]
        public Transform WeaponIdleWalkRun;
    
        [Tooltip("Сокет для оружия в состоянии атаки (например, для боевых анимаций)")]
        public Transform WeaponAttack;

        [Tooltip("Объект оружия, который будет перемещаться между сокетами")]
        public GameObject WeaponObject;

        private Transform currentSocket;

        private void Start()
        {
            if (WeaponObject != null && WeaponIdleWalkRun != null)
            {
                // Изначально прикрепить к сокету idle/walk/run
                AttachToSocket(WeaponIdleWalkRun);
            }
        }

        /// <summary>
        /// Переключить оружие на сокет idle/walk/run
        /// </summary>
        public void SwitchToIdleWalkRun()
        {
            if (WeaponIdleWalkRun != null)
            {
                AttachToSocket(WeaponIdleWalkRun);
            }
        }

        /// <summary>
        /// Переключить оружие на сокет атаки
        /// </summary>
        public void SwitchToAttack()
        {
            if (WeaponAttack != null)
            {
                AttachToSocket(WeaponAttack);
            }
        }

        private void AttachToSocket(Transform socket)
        {
            if (WeaponObject == null || socket == null) return;

            // Если мы уже прикреплены к этому сокету, ничего не делаем
            if (WeaponObject.transform.parent == socket) return;

            // Делаем сокет родителем, сохраняя мировую трансформацию
            WeaponObject.transform.SetParent(socket, true);

            // Сбрасываем локальную позицию и ротацию на значения сокета
            WeaponObject.transform.localPosition = Vector3.zero;
            WeaponObject.transform.localRotation = Quaternion.identity;

            currentSocket = socket;
        }

        /// <summary>
        /// Получить текущий сокет
        /// </summary>
        public Transform GetCurrentSocket()
        {
            return currentSocket;
        }
    }
}