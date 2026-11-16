using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ImpactSound : MonoBehaviour
{
    public AudioClip[] impactClips;

    [Header("Força da batida")]
    public float minImpactSpeed = 0.4f;
    public float maxImpactSpeed = 4f;
    [Range(0f, 1f)] public float minVolume = 0.15f;
    [Range(0f, 1f)] public float maxVolume = 1f;

    [Header("Variação")]
    public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
    public float cooldown = 0.1f;

    [Header("Filtros")]
    public LayerMask surfaces = ~0;
    public float ignoreAfterStart = 0.5f;

    private AudioSource source;
    private float nextAllowedTime;

    void Awake()
    {
        // AudioSource próprio, para não interferir em outro som que o objeto já tenha.
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 0.5f;
        source.maxDistance = 8f;

        // Evita som quando os objetos se acomodam no início da cena.
        nextAllowedTime = Time.time + ignoreAfterStart;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (impactClips == null || impactClips.Length == 0) return;
        if (Time.time < nextAllowedTime) return;
        if ((surfaces.value & (1 << collision.gameObject.layer)) == 0) return;

        float speed = collision.relativeVelocity.magnitude;
        if (speed < minImpactSpeed) return;

        float strength = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, speed);
        AudioClip clip = impactClips[Random.Range(0, impactClips.Length)];
        if (clip == null) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, Mathf.Lerp(minVolume, maxVolume, strength));
        nextAllowedTime = Time.time + cooldown;
    }
}
