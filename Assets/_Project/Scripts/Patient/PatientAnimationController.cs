using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PatientAnimationController : MonoBehaviour
{
    private const float SeatSettleDelay = 0.2f;
    private const float ReturnToSeatDelay = 1.0f;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int StartExit = Animator.StringToHash("IniciarSaida");
    private static readonly int StartSitting = Animator.StringToHash("IniciarSentar");
    private static readonly int IsTalking = Animator.StringToHash("IsTalking");

    public Animator patientAnimator;
    public Animator parentAnimator;
    public NavMeshAgent patientAgent; // Preserved for NavMesh if needed
    public TransitionManager transitionManager;
    public AudioSource anamnesisAudioSource;

    [Header("Parent Movement Animation")]
    public string parentMovingStateName = "moving";
    public string parentIdleStateName = "Idle";
    [Tooltip("Duration of the full trip moving animation (auto-detects from clip length if found)")]
    public float movingDuration = 10f;

    [Header("Reference Points")]
    public Transform pontoIncioCaminhada;
    public Transform pontoDestino;
    public float tempoDeEsperaNoDestino = 3.0f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Coroutine tripRoutine;

    void Awake()
    {
        if (patientAnimator == null)
            patientAnimator = GetComponent<Animator>();
        if (patientAgent == null)
            patientAgent = GetComponent<NavMeshAgent>();
        if (transitionManager == null)
            transitionManager = FindObjectOfType<TransitionManager>();

        if (parentAnimator == null && transform.parent != null)
            parentAnimator = transform.parent.GetComponent<Animator>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Update()
    {
        if (anamnesisAudioSource != null && patientAnimator != null)
        {
            patientAnimator.SetBool(IsTalking, anamnesisAudioSource.isPlaying);
        }
    }

    void Start()
    {
        if (patientAnimator == null)
        {
            Debug.LogError("Animator não encontrado no paciente!", gameObject);
            enabled = false;
            return;
        }

        /* === NAVMESH CHECK (COMMENTED OUT) ===
        if (patientAgent == null)
        {
            Debug.LogError("NavMeshAgent não encontrado no paciente!", gameObject);
            enabled = false;
            return;
        }
        ======================================== */

        if (patientAgent != null)
            patientAgent.enabled = false;
    }

    public void StartExitAndReturnSequence()
    {
        if (tripRoutine != null) return;

        tripRoutine = StartCoroutine(FullTripRoutine());
    }

    private IEnumerator FullTripRoutine()
    {
        /* =========================================================================
         * ORIGINAL NAVMESH MOVEMENT LOGIC (COMMENTED OUT)
         * =========================================================================
        patientAnimator.SetBool(IsWalking, true);
        patientAnimator.SetTrigger(StartExit);
        yield return new WaitUntil(() => IsInState("Walking"));

        patientAgent.enabled = true;
        patientAgent.Warp(pontoIncioCaminhada.position);
        yield return WalkTo(pontoDestino.position);

        yield return new WaitForSeconds(tempoDeEsperaNoDestino);

        yield return WalkTo(pontoIncioCaminhada.position);

        yield return new WaitForSeconds(SeatSettleDelay);
        patientAnimator.SetTrigger(StartSitting);
        patientAgent.enabled = false;
        yield return new WaitUntil(() => IsInState("Sitting"));
        yield return new WaitForSeconds(ReturnToSeatDelay);

        transform.SetPositionAndRotation(originalPosition, originalRotation);
        tripRoutine = null;

        if (transitionManager != null)
            transitionManager.AnimationSequenceFinished();
        ========================================================================= */

        // =========================================================================
        // PARENT ANIMATION MOVEMENT
        // The "moving" animation covers the full journey:
        // Going to destination, waiting at the exam table, and returning to the chair.
        // =========================================================================

        // 1. Patient stands up and begins walking leg cycle
        patientAnimator.SetBool(IsWalking, true);
        patientAnimator.SetTrigger(StartExit);
        yield return new WaitUntil(() => IsInState("Walking") || IsInState("walking"));

        // 2. Play the parent's "moving" animation for the entire round trip
        if (parentAnimator != null && !string.IsNullOrEmpty(parentMovingStateName))
        {
            parentAnimator.Play(parentMovingStateName, 0, 0f);
        }

        // Wait for the full round trip animation to finish
        yield return WaitForParentMovement(parentMovingStateName, movingDuration);

        // 3. Arrived back at chair: stop walking legs, settle, and sit down
        patientAnimator.SetBool(IsWalking, false);

        if (parentAnimator != null && !string.IsNullOrEmpty(parentIdleStateName))
        {
            parentAnimator.Play(parentIdleStateName, 0, 0f);
        }

        yield return new WaitForSeconds(SeatSettleDelay);
        patientAnimator.SetTrigger(StartSitting);

        yield return new WaitUntil(() => IsInState("Sitting"));
        yield return new WaitForSeconds(ReturnToSeatDelay);

        transform.SetPositionAndRotation(originalPosition, originalRotation);
        tripRoutine = null;

        if (transitionManager != null)
            transitionManager.AnimationSequenceFinished();
    }

    /// <summary>
    /// Can be called via Animation Events in moving.anim to stop the leg walking cycle
    /// when the patient arrives at the exam table and is waiting / going up/down.
    /// </summary>
    public void StopWalking()
    {
        if (patientAnimator != null)
            patientAnimator.SetBool(IsWalking, false);
    }

    /// <summary>
    /// Can be called via Animation Events in moving.anim to resume the leg walking cycle
    /// when the patient starts walking back to the chair.
    /// </summary>
    public void ResumeWalking()
    {
        if (patientAnimator != null)
            patientAnimator.SetBool(IsWalking, true);
    }

    private IEnumerator WaitForParentMovement(string stateName, float fallbackDuration)
    {
        float duration = fallbackDuration;

        // Try to auto-detect the duration from the animation clip on the parent animator
        if (parentAnimator != null && parentAnimator.runtimeAnimatorController != null)
        {
            foreach (AnimationClip clip in parentAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Equals(stateName, System.StringComparison.OrdinalIgnoreCase))
                {
                    duration = clip.length;
                    break;
                }
            }
        }

        yield return new WaitForSeconds(duration);
    }

    /* =========================================================================
     * ORIGINAL NAVMESH WALKTO HELPER (COMMENTED OUT)
     * =========================================================================
    private IEnumerator WalkTo(Vector3 destination)
    {
        patientAnimator.SetBool(IsWalking, true);
        patientAgent.SetDestination(destination);

        while (patientAgent.pathPending || patientAgent.remainingDistance > patientAgent.stoppingDistance)
            yield return null;

        patientAnimator.SetBool(IsWalking, false);
    }
    ========================================================================= */

    private bool IsInState(string stateName)
    {
        AnimatorStateInfo stateInfo = patientAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(stateName))
            return true;

        if (stateName.Equals("Walking", System.StringComparison.OrdinalIgnoreCase))
            return stateInfo.IsName("Walking") || stateInfo.IsName("walking");

        if (stateName.Equals("Sitting", System.StringComparison.OrdinalIgnoreCase))
            return stateInfo.IsName("Sitting") || stateInfo.IsName("sitting_idle") || stateInfo.IsName("sitting_idle_talking");

        return false;
    }
}
