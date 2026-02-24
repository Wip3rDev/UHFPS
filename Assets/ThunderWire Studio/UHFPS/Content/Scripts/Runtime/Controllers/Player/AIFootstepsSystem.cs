using UnityEngine;
using UHFPS.Scriptable;
using UHFPS.Tools;
using static UHFPS.Scriptable.SurfaceDefinitionSet;
using UnityEngine.AI;

namespace UHFPS.Runtime
{
    [RequireComponent(typeof(AudioSource))]
    public class AIFootstepsSystem : MonoBehaviour
    {
        public SurfaceDefinitionSet SurfaceDefinitionSet;
        public SurfaceDetection SurfaceDetection;
        public LayerMask FootstepsMask;

        public float StepPlayerVelocity = 0.1f;
        public float WalkStepTime = 1f;
        public float RunStepTime = 1f;
        public float LandStepTime = 1f;

        [Range(0, 1)] public float WalkingVolume = 1f;
        [Range(0, 1)] public float RunningVolume = 1f;
        [Range(0, 1)] public float LandVolume = 1f;

        public SurfaceDefinition CurrentSurface;

        private AudioSource audioSource;
        private Collider surfaceUnder;
        private NavMeshAgent agent;
        private Animator animator;

        private int lastStep;
        private int lastLandStep;

        private float stepTime;
        private float airTime;
        private bool wasInAir;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

            // Make audio 3D
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 20f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Update()
        {
            // Raycast down to find surface under NPC
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 1f, FootstepsMask))
            {
                surfaceUnder = hit.collider;
                CurrentSurface = SurfaceDefinitionSet.GetSurface(surfaceUnder.gameObject, transform.position, SurfaceDetection);
                if (CurrentSurface != null)
                    EvaluateFootsteps(CurrentSurface);
            }
            else
            {
                surfaceUnder = null;
            }

            // Check for landing (if NPC can jump, but for simplicity, assume grounded)
            // For now, skip air time logic as NPCs usually don't jump
        }

        private void EvaluateFootsteps(SurfaceDefinition surface)
        {
            float agentVelocity = agent.velocity.magnitude;
            bool isMoving = agentVelocity > StepPlayerVelocity;

            if (isMoving && stepTime <= 0)
            {
                PlayFootstep(surface, false);
                stepTime = agentVelocity > 2f ? RunStepTime : WalkStepTime; // Simple run/walk detection
            }

            if (stepTime > 0f)
                stepTime -= Time.deltaTime;
        }

        private void PlayFootstep(SurfaceDefinition surface, bool isLand)
        {
            if (!isLand && surface.SurfaceFootsteps.Count > 0)
            {
                lastStep = GameTools.RandomUnique(0, surface.SurfaceFootsteps.Count, lastStep);
                AudioClip footstep = surface.SurfaceFootsteps[lastStep];

                float volume = surface.FootstepsVolume;
                float volumeScale = (agent.velocity.magnitude > 2f ? RunningVolume : WalkingVolume) * volume;

                audioSource.PlayOneShot(footstep, volumeScale);
            }
            else if (surface.SurfaceLandSteps.Count > 0)
            {
                lastLandStep = GameTools.RandomUnique(0, surface.SurfaceLandSteps.Count, lastLandStep);
                AudioClip landStep = surface.SurfaceLandSteps[lastLandStep];

                float volume = surface.LandStepsVolume;
                float volumeScale = LandVolume * volume;

                audioSource.PlayOneShot(landStep, volumeScale);
            }
        }

        public void PlayFootstep(bool runningStep)
        {
            if (surfaceUnder == null)
                return;

            CurrentSurface = SurfaceDefinitionSet.GetSurface(surfaceUnder.gameObject, transform.position, SurfaceDetection);
            if (CurrentSurface != null)
            {
                PlayFootstep(CurrentSurface, false);
            }
        }

        public void PlayLandSteps()
        {
            if (surfaceUnder == null)
                return;

            CurrentSurface = SurfaceDefinitionSet.GetSurface(surfaceUnder.gameObject, transform.position, SurfaceDetection);
            if (CurrentSurface != null) PlayFootstep(CurrentSurface, true);
        }
    }
}