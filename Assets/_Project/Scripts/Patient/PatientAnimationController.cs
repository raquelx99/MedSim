using System.Collections;
using UnityEngine;

public class PatientAnimationController : MonoBehaviour
{
    private const float ArrivalThreshold = 0.01f;
    private const float FacingThreshold = 0.5f;
    private const float StandUpExitTime = 0.9f;
    private const float SitDownExitTime = 0.9f;

    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int StartExit = Animator.StringToHash("IniciarSaida");
    private static readonly int StartSitting = Animator.StringToHash("IniciarSentar");
    private static readonly int IsTalking = Animator.StringToHash("IsTalking");

    private const string StandUpState = "stand_up";
    private const string WalkingState = "walking";
    private const string SitDownState = "sit_down";

    [Header("Referências")]
    public Animator patientAnimator;
    public TransitionManager transitionManager;
    public AudioSource anamnesisAudioSource;
    public Transform movementRoot;

    [Header("Trajeto")]
    public Transform[] waypoints;
    public float waitAtDestination = 3f;

    [Header("Movimento")]
    public float walkSpeed = 1f;
    public float turnSpeed = 180f;
    public float maxWalkingTurnAngle = 60f;
    public AnimationCurve standUpLowering = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float leaveSeatDuration = 0.65f;
    public float sitForwardCompensation = 0.55f;
    public float sitSettleDuration = 0.25f;
    public float stateTimeout = 5f;

    private Vector3 modelForwardLocal = Vector3.forward;
    private Coroutine tripRoutine;

    void Awake()
    {
        if (patientAnimator == null)
            patientAnimator = GetComponent<Animator>();
        if (transitionManager == null)
            transitionManager = FindObjectOfType<TransitionManager>();
        if (movementRoot == null)
            movementRoot = transform.parent != null ? transform.parent : transform;

        Animator rootAnimator = movementRoot.GetComponent<Animator>();
        if (rootAnimator != null && rootAnimator != patientAnimator && rootAnimator.enabled)
        {
            Debug.LogWarning($"O Animator de '{movementRoot.name}' foi desativado: agora o trajeto do paciente é feito por waypoints.", rootAnimator);
            rootAnimator.enabled = false;
        }

        if (patientAnimator != null)
        {
            Vector3 local = Quaternion.Inverse(movementRoot.rotation) * patientAnimator.transform.forward;
            local.y = 0f;
            if (local.sqrMagnitude > 0.0001f)
                modelForwardLocal = local.normalized;
        }
    }

    void Start()
    {
        if (patientAnimator == null)
        {
            Debug.LogError("Animator não encontrado no paciente!", gameObject);
            enabled = false;
        }
    }

    void Update()
    {
        if (anamnesisAudioSource != null)
            patientAnimator.SetBool(IsTalking, anamnesisAudioSource.isPlaying);
    }

    public void StartExitAndReturnSequence()
    {
        if (tripRoutine != null) return;

        if (!HasValidPath())
        {
            Debug.LogError("PatientAnimationController: configure pelo menos um waypoint (sem itens vazios) para o paciente andar.", this);
            FinishSequence();
            return;
        }

        tripRoutine = StartCoroutine(FullTripRoutine());
    }

    private IEnumerator FullTripRoutine()
    {
        Vector3 seatPosition = movementRoot.position;
        Quaternion seatRotation = movementRoot.rotation;
        int last = waypoints.Length - 1;

        patientAnimator.SetBool(IsWalking, true);
        patientAnimator.SetTrigger(StartExit);
        yield return WaitForState(StandUpState);

        Vector3 standingPosition = seatPosition;
        standingPosition.y = waypoints[0].position.y;
        float standDuration = CurrentOrNextStateLength() * StandUpExitTime;
        yield return MoveRootTo(standingPosition, seatRotation, standDuration, standUpLowering);
        yield return WaitForState(WalkingState);

        yield return MoveRootTo(waypoints[0].position, seatRotation, leaveSeatDuration);

        for (int i = 1; i <= last; i++)
            yield return WalkTo(waypoints[i].position);

        patientAnimator.SetBool(IsWalking, false);
        yield return TurnTo(FacingRotation(waypoints[last].forward));
        yield return new WaitForSeconds(waitAtDestination);

        patientAnimator.SetBool(IsWalking, true);
        for (int i = last - 1; i >= 0; i--)
            yield return WalkTo(waypoints[i].position);

        patientAnimator.SetBool(IsWalking, false);
        yield return TurnTo(seatRotation);
        patientAnimator.SetTrigger(StartSitting);
        yield return WaitForState(SitDownState);

        float sitDuration = CurrentOrNextStateLength() * SitDownExitTime;
        Vector3 sitPosition = seatPosition + seatRotation * modelForwardLocal * sitForwardCompensation;
        yield return MoveRootTo(sitPosition, seatRotation, sitDuration);
        yield return MoveRootTo(seatPosition, seatRotation, sitSettleDuration);

        FinishSequence();
    }

    private void FinishSequence()
    {
        tripRoutine = null;
        if (transitionManager != null)
            transitionManager.AnimationSequenceFinished();
    }

    private bool HasValidPath()
    {
        if (waypoints == null || waypoints.Length == 0)
            return false;

        foreach (Transform waypoint in waypoints)
        {
            if (waypoint == null)
                return false;
        }

        return true;
    }

    private IEnumerator WalkTo(Vector3 target)
    {
        while ((target - movementRoot.position).sqrMagnitude > ArrivalThreshold * ArrivalThreshold)
        {
            Vector3 direction = target - movementRoot.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion facing = FacingRotation(direction);
                movementRoot.rotation = Quaternion.RotateTowards(movementRoot.rotation, facing, turnSpeed * Time.deltaTime);

                if (Quaternion.Angle(movementRoot.rotation, facing) > maxWalkingTurnAngle)
                {
                    yield return null;
                    continue;
                }
            }

            movementRoot.position = Vector3.MoveTowards(movementRoot.position, target, walkSpeed * Time.deltaTime);
            yield return null;
        }

        movementRoot.position = target;
    }

    private IEnumerator TurnTo(Quaternion target)
    {
        while (Quaternion.Angle(movementRoot.rotation, target) > FacingThreshold)
        {
            movementRoot.rotation = Quaternion.RotateTowards(movementRoot.rotation, target, turnSpeed * Time.deltaTime);
            yield return null;
        }

        movementRoot.rotation = target;
    }

    private IEnumerator MoveRootTo(Vector3 position, Quaternion rotation, float duration, AnimationCurve easing = null)
    {
        Vector3 startPosition = movementRoot.position;
        Quaternion startRotation = movementRoot.rotation;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            if (easing != null && easing.length > 0)
                t = easing.Evaluate(t);
            movementRoot.SetPositionAndRotation(
                Vector3.Lerp(startPosition, position, t),
                Quaternion.Slerp(startRotation, rotation, t));
            yield return null;
        }

        movementRoot.SetPositionAndRotation(position, rotation);
    }

    private Quaternion FacingRotation(Vector3 worldDirection)
    {
        worldDirection.y = 0f;
        if (worldDirection.sqrMagnitude < 0.0001f)
            return movementRoot.rotation;

        return Quaternion.LookRotation(worldDirection) * Quaternion.Inverse(Quaternion.LookRotation(modelForwardLocal));
    }

    private IEnumerator WaitForState(string stateName)
    {
        float elapsed = 0f;
        while (!IsInOrEnteringState(stateName))
        {
            if (elapsed >= stateTimeout)
            {
                Debug.LogWarning($"PatientAnimationController: o Animator não chegou ao estado '{stateName}' em {stateTimeout}s. Seguindo mesmo assim.", this);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool IsInOrEnteringState(string stateName)
    {
        if (patientAnimator.GetCurrentAnimatorStateInfo(0).IsName(stateName))
            return true;

        return patientAnimator.IsInTransition(0) && patientAnimator.GetNextAnimatorStateInfo(0).IsName(stateName);
    }

    private float CurrentOrNextStateLength()
    {
        return patientAnimator.IsInTransition(0)
            ? patientAnimator.GetNextAnimatorStateInfo(0).length
            : patientAnimator.GetCurrentAnimatorStateInfo(0).length;
    }

    void OnDrawGizmosSelected()
    {
        if (!HasValidPath()) return;

        Transform root = movementRoot != null ? movementRoot : (transform.parent != null ? transform.parent : transform);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(root.position, waypoints[0].position);

        for (int i = 0; i < waypoints.Length; i++)
        {
            Gizmos.DrawWireSphere(waypoints[i].position, 0.1f);
            if (i > 0)
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }

        Transform destination = waypoints[waypoints.Length - 1];
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(destination.position, destination.forward * 0.5f);
    }
}
