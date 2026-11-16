using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class TimePassageEffect : MonoBehaviour
{
    [Header("Luzes")]
    public Light[] lights;
    public Color nightLightColor = new Color(0.35f, 0.45f, 0.85f);
    [Range(0f, 1f)] public float nightLightIntensity = 0.15f;
    [Range(0f, 1f)] public float nightAmbientIntensity = 0.2f;

    [Header("Tempo")]
    public float duskDuration = 2f;
    public float nightDuration = 1.5f;
    public float dawnDuration = 2f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Som")]
    public AudioSource clockSource;
    public float soundFadeDuration = 0.5f;

    private Color[] originalColors;
    private float[] originalIntensities;
    private SphericalHarmonicsL2 originalAmbientProbe;
    private float originalReflectionIntensity;
    private float originalClockVolume = 1f;

    public bool IsPlaying { get; private set; }
    public float Duration => duskDuration + nightDuration + dawnDuration;

    public IEnumerator Play()
    {
        if (IsPlaying) yield break;

        IsPlaying = true;
        CaptureOriginals();

        if (clockSource != null)
        {
            clockSource.loop = true;
            clockSource.volume = 0f;
            clockSource.Play();
        }

        float total = Duration;
        for (float elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
        {
            ApplyNight(NightAmount(elapsed));
            SetClockVolume(elapsed, total);
            yield return null;
        }

        Restore();
    }

    // 0 = dia, 1 = noite.
    private float NightAmount(float elapsed)
    {
        if (elapsed < duskDuration)
            return transitionCurve.Evaluate(elapsed / duskDuration);

        elapsed -= duskDuration;
        if (elapsed < nightDuration)
            return 1f;

        elapsed -= nightDuration;
        return dawnDuration > 0f ? 1f - transitionCurve.Evaluate(elapsed / dawnDuration) : 0f;
    }

    private void SetClockVolume(float elapsed, float total)
    {
        if (clockSource == null) return;

        float fade = soundFadeDuration > 0f
            ? Mathf.Clamp01(Mathf.Min(elapsed, total - elapsed) / soundFadeDuration)
            : 1f;
        clockSource.volume = originalClockVolume * fade;
    }

    private void CaptureOriginals()
    {
        int count = lights != null ? lights.Length : 0;
        originalColors = new Color[count];
        originalIntensities = new float[count];
        for (int i = 0; i < count; i++)
        {
            if (lights[i] == null) continue;
            originalColors[i] = lights[i].color;
            originalIntensities[i] = lights[i].intensity;
        }

        originalAmbientProbe = RenderSettings.ambientProbe;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;

        if (clockSource != null)
            originalClockVolume = clockSource.volume;
    }

    private void ApplyNight(float amount)
    {
        for (int i = 0; i < originalColors.Length; i++)
        {
            if (lights[i] == null) continue;
            lights[i].color = Color.Lerp(originalColors[i], nightLightColor, amount);
            lights[i].intensity = originalIntensities[i] * Mathf.Lerp(1f, nightLightIntensity, amount);
        }

        float ambient = Mathf.Lerp(1f, nightAmbientIntensity, amount);
        RenderSettings.ambientProbe = originalAmbientProbe * ambient;
        RenderSettings.reflectionIntensity = originalReflectionIntensity * ambient;
    }

    private void Restore()
    {
        ApplyNight(0f);

        if (clockSource != null)
        {
            clockSource.Stop();
            clockSource.volume = originalClockVolume;
        }

        IsPlaying = false;
    }

    void OnDisable()
    {
        if (IsPlaying)
            Restore();
    }
}
