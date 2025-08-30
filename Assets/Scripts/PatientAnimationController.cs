using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class PatientAnimationController : MonoBehaviour
{
    [Header("Componentes")]
    public Animator patientAnimator;
    public NavMeshAgent patientAgent;

    [Header("Pontos de Movimento")]
    public Transform pontoIncioCaminhada;
    public Transform pontoDestino;
    
    [Header("Configurações")]
    [Tooltip("Tempo em segundos que o personagem ficará parado no destino.")]
    public float tempoDeEsperaNoDestino = 3.0f;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Coroutine walkCoroutine;

    void Awake()
    {
        if (patientAnimator == null)
            patientAnimator = GetComponent<Animator>();
        if (patientAgent == null)
            patientAgent = GetComponent<NavMeshAgent>();

        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void Start()
    {
        if (patientAgent == null || patientAnimator == null)
        {
            Debug.LogError("Animator ou NavMeshAgent não encontrados no paciente!", this.gameObject);
            this.enabled = false;
            return;
        }
        patientAgent.enabled = false;
    }

    public void StartExitAndReturnSequence()
    {
        if (walkCoroutine != null) return;
        walkCoroutine = StartCoroutine(FullTripRoutine());
    }

    private IEnumerator FullTripRoutine()
    {
        patientAnimator.SetBool("IsWalking", true);
        patientAnimator.SetTrigger("IniciarSaida");

        yield return new WaitUntil(() => patientAnimator.GetCurrentAnimatorStateInfo(0).IsName("Walking"));
        
        patientAgent.enabled = true;
        patientAgent.Warp(pontoIncioCaminhada.position);
        patientAgent.SetDestination(pontoDestino.position);

        while (patientAgent.pathPending || patientAgent.remainingDistance > patientAgent.stoppingDistance)
        {
            yield return null;
        }

        patientAnimator.SetBool("IsWalking", false);
        yield return new WaitForSeconds(tempoDeEsperaNoDestino);

        patientAnimator.SetBool("IsWalking", true);
        patientAgent.SetDestination(pontoIncioCaminhada.position);

        while (patientAgent.pathPending || patientAgent.remainingDistance > patientAgent.stoppingDistance)
        {
            yield return null;
        }

        patientAnimator.SetBool("IsWalking", false);
        yield return new WaitForSeconds(0.2f);

        patientAnimator.SetTrigger("IniciarSentar");
        patientAgent.enabled = false;

        yield return new WaitUntil(() => patientAnimator.GetCurrentAnimatorStateInfo(0).IsName("Sitting"));
        yield return new WaitForSeconds(1.0f);

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        walkCoroutine = null;
        
        FindObjectOfType<TransitionManager>().AnimationSequenceFinished();
    }
}