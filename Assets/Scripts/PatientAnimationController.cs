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

    public Animator patientAnimator;
    public NavMeshAgent patientAgent;
    public TransitionManager transitionManager;
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

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Start()
    {
        if (patientAgent == null || patientAnimator == null)
        {
            Debug.LogError("Animator ou NavMeshAgent não encontrados no paciente!", gameObject);
            enabled = false;
            return;
        }

        patientAgent.enabled = false;
    }

    public void StartExitAndReturnSequence()
    {
        if (tripRoutine != null) return;

        tripRoutine = StartCoroutine(FullTripRoutine());
    }

    private IEnumerator FullTripRoutine()
    {
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
    }

    private IEnumerator WalkTo(Vector3 destination)
    {
        patientAnimator.SetBool(IsWalking, true);
        patientAgent.SetDestination(destination);

        while (patientAgent.pathPending || patientAgent.remainingDistance > patientAgent.stoppingDistance)
            yield return null;

        patientAnimator.SetBool(IsWalking, false);
    }

    private bool IsInState(string stateName)
    {
        AnimatorStateInfo stateInfo = patientAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName(stateName))
            return true;

        if (stateName.Equals("Walking", System.StringComparison.OrdinalIgnoreCase))
            return stateInfo.IsName("Walking") || stateInfo.IsName("walking");

        if (stateName.Equals("Sitting", System.StringComparison.OrdinalIgnoreCase))
            return stateInfo.IsName("Sitting") || stateInfo.IsName("sitting_idle");

        return false;
    }
}
