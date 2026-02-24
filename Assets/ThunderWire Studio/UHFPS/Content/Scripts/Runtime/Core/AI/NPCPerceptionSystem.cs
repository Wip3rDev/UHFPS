using UnityEngine;
using ThunderWire.Attributes;

namespace UHFPS.Runtime
{
    /// <summary>
    /// System for NPC perception - detects player by sound (running) and damage.
    /// </summary>
    [Docs("https://docs.twgamesdev.com/uhfps/guides/state-machines/npc-perception")]
    public class NPCPerceptionSystem : MonoBehaviour
    {
        [Header("Hearing Settings")]
        [Tooltip("Radius in which NPC can hear the player running")]
        public float HearRadius = 10f;

        [Tooltip("Minimum player speed to be considered as running (for hearing detection)")]
        public float MinRunningSpeed = 4f;

        [Tooltip("Check player velocity instead of state for hearing detection")]
        public bool UseVelocityInsteadOfState = false;

        [Header("Shot Detection Settings")]
        [Tooltip("Radius in which NPC can hear gunshots")]
        public float ShotHearRadius = 50f;

        [Tooltip("NPC will chase player when hearing a gunshot")]
        public bool ChaseOnHearShot = true;

        [Header("Damage Alert Settings")]
        [Tooltip("NPC will always chase when damaged, regardless of distance")]
        public bool ChaseOnDamage = true;

        [Tooltip("How long NPC will remember being damaged and chase the player")]
        public float DamageAlertDuration = 5f;

        [Header("Visualization")]
        [Tooltip("Show hearing radius in editor gizmos")]
        public bool ShowHearRadius = true;

        [Tooltip("Color of hearing radius gizmo")]
        public Color HearRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);

        [Tooltip("Color of shot hearing radius gizmo")]
        public Color ShotHearRadiusColor = new Color(1f, 0f, 0f, 0.2f);

        [Tooltip("Show damage alert timer in editor gizmos")]
        public bool ShowDamageAlert = true;

        /// <summary>
        /// Event triggered when NPC detects player (by sound or damage).
        /// </summary>
        public System.Action onPlayerDetected;

        // Private fields
        private NPCStateMachine npcStateMachine;
        private NPCHealth npcHealth;
        private PlayerStateMachine playerStateMachine;
        private CharacterController playerController;

        private bool isPlayerRunning;
        private float damageAlertTimer;
        private bool isAlertedByDamage;
        private bool isAlertedByShot;

        // Properties
        public bool IsAlerted => isAlertedByDamage || isAlertedByShot || HearPlayer();
        public float DamageAlertRemainingTime => damageAlertTimer;

        private void Awake()
        {
            npcStateMachine = GetComponent<NPCStateMachine>();
            npcHealth = GetComponent<NPCHealth>();
        }

        private void Start()
        {
            // Get player references
            if (PlayerPresenceManager.Instance != null)
            {
                var player = PlayerPresenceManager.Instance.Player;
                if (player != null)
                {
                    playerStateMachine = player.GetComponent<PlayerStateMachine>();
                    playerController = player.GetComponent<CharacterController>();
                }

                // Subscribe to player shot event
                PlayerPresenceManager.OnPlayerShot += OnPlayerShot;
            }

            // Subscribe to damage event
            if (npcHealth != null)
            {
                npcHealth.OnTakeDamage.AddListener(OnNpcTakeDamage);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from player shot event
            PlayerPresenceManager.OnPlayerShot -= OnPlayerShot;

            // Unsubscribe from damage event
            if (npcHealth != null)
            {
                npcHealth.OnTakeDamage.RemoveListener(OnNpcTakeDamage);
            }
        }

        private void Update()
        {
            if (npcStateMachine == null || npcStateMachine.IsPlayerDead)
                return;

            // Update player running state
            UpdatePlayerRunningState();

            // Update damage alert timer
            if (isAlertedByDamage)
            {
                damageAlertTimer -= Time.deltaTime;
                if (damageAlertTimer <= 0f)
                {
                    isAlertedByDamage = false;
                    damageAlertTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Update whether player is running based on state or velocity.
        /// </summary>
        private void UpdatePlayerRunningState()
        {
            if (playerStateMachine == null)
            {
                isPlayerRunning = false;
                return;
            }

            if (UseVelocityInsteadOfState && playerController != null)
            {
                // Check player velocity magnitude
                float speed = playerController.velocity.magnitude;
                isPlayerRunning = speed >= MinRunningSpeed;
            }
            else
            {
                // Check player state
                isPlayerRunning = playerStateMachine.IsCurrent(PlayerStateMachine.RUN_STATE);
            }
        }

        /// <summary>
        /// Check if NPC can hear the player running.
        /// </summary>
        public bool HearPlayer()
        {
            if (playerStateMachine == null || npcStateMachine == null)
                return false;

            // Check distance
            float distance = Vector3.Distance(transform.position, npcStateMachine.Player.transform.position);
            if (distance > HearRadius)
                return false;

            // Check if player is running
            return isPlayerRunning;
        }

        /// <summary>
        /// Called when NPC takes damage.
        /// </summary>
        private void OnNpcTakeDamage(int damage)
        {
            if (!ChaseOnDamage || npcStateMachine.IsPlayerDead)
                return;

            // Start damage alert
            isAlertedByDamage = true;
            damageAlertTimer = DamageAlertDuration;

            // Trigger detection event
            onPlayerDetected?.Invoke();
        }

        /// <summary>
        /// Called when player fires a shot.
        /// </summary>
        private void OnPlayerShot(Vector3 shootPosition)
        {
            if (!ChaseOnHearShot || npcStateMachine == null || npcStateMachine.IsPlayerDead)
                return;

            // Check if NPC can hear the shot
            float distance = Vector3.Distance(transform.position, shootPosition);
            if (distance <= ShotHearRadius)
            {
                isAlertedByShot = true;
                onPlayerDetected?.Invoke();
            }
        }

        /// <summary>
        /// Check if NPC should chase the player.
        /// </summary>
        public bool ShouldChasePlayer()
        {
            return isAlertedByDamage || isAlertedByShot || HearPlayer();
        }

        private void OnDrawGizmosSelected()
        {
            DrawGizmos();
        }

        private void OnDrawGizmos()
        {
            DrawGizmos();
        }

        private void DrawGizmos()
        {
            // Draw hearing radius
            if (ShowHearRadius)
            {
                Gizmos.color = HearRadiusColor;
                Gizmos.DrawSphere(transform.position, HearRadius);
                
                // Draw wire sphere for better visibility
                Gizmos.color = HearRadiusColor * 2f;
                Gizmos.DrawWireSphere(transform.position, HearRadius);
            }

            // Draw shot hearing radius
            Gizmos.color = ShotHearRadiusColor;
            Gizmos.DrawSphere(transform.position, ShotHearRadius);
            Gizmos.color = ShotHearRadiusColor * 2f;
            Gizmos.DrawWireSphere(transform.position, ShotHearRadius);

            // Draw damage alert indicator
            if (ShowDamageAlert && Application.isPlaying && (isAlertedByDamage || isAlertedByShot))
            {
                Gizmos.color = Color.red;
                Vector3 alertPos = transform.position + Vector3.up * 2f;
                Gizmos.DrawWireCube(alertPos, Vector3.one * 0.5f);
            }
        }
    }
}
