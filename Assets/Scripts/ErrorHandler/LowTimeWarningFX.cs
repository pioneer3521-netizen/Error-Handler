using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Handles a pulsing/flashing border and optional audio stingers while a bug is "low-time".
// Attach this to the same GameObject as the BugListItem_TMP (or a child) and assign references.
[RequireComponent(typeof(AudioSource))]
public class LowTimeWarningFX : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Image used as a border. Its color/alpha will be animated.")]
    public Image borderImage;
    [Tooltip("Base color for the border when not flashing.")]
    public Color baseColor = new Color(1f, 1f, 1f, 0.12f);
    [Tooltip("Flash color when warning is active.")]
    public Color flashColor = new Color(1f, 0.3f, 0.3f, 0.95f);
    [Tooltip("Speed of the flash pulse.")]
    public float flashFrequency = 2.8f;
    [Tooltip("Whether to also pulse the entire item scale slightly.")]
    public bool pulseScale = true;
    [Tooltip("Pulse amplitude (fraction).")]
    public float pulseAmplitude = 0.04f;

    [Header("Audio")]
    [Tooltip("One-shot stinger played once when warning begins.")]
    public AudioClip initialStinger;
    [Tooltip("Optional repeated stinger played while still in warning state.")]
    public AudioClip repeatingStinger;
    [Tooltip("Interval between repeating stingers (seconds).")]
    public float repeatingInterval = 6f;
    [Tooltip("Volume for stingers.")]
    [Range(0f,1f)]
    public float stingerVolume = 0.9f;

    // internals
    AudioSource audioSource;
    bool isActive = false;
    Coroutine repeatingCoroutine;
    Vector3 baseScale;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        if (borderImage != null) borderImage.color = baseColor;
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (!isActive) return;

        // Visual flash: lerp between baseColor and flashColor using pingpong.
        if (borderImage != null)
        {
            float t = (Mathf.Sin(Time.time * flashFrequency * Mathf.PI * 2f) + 1f) * 0.5f; // 0..1 smooth
            borderImage.color = Color.Lerp(baseColor, flashColor, t);
        }

        if (pulseScale)
        {
            float pulse = 1f + (Mathf.Sin(Time.time * flashFrequency * Mathf.PI * 2f) * pulseAmplitude);
            transform.localScale = baseScale * pulse;
        }
    }

    // Start the warning effect (idempotent)
    public void StartWarning()
    {
        if (isActive) return;
        isActive = true;

        // start audio
        if (initialStinger != null)
            audioSource.PlayOneShot(initialStinger, stingerVolume);

        if (repeatingStinger != null && repeatingInterval > 0f)
            repeatingCoroutine = StartCoroutine(RepeatingStinger());

        // (border animation driven in Update)
    }

    // Stop the warning effect and restore visuals
    public void StopWarning()
    {
        if (!isActive) return;
        isActive = false;

        if (repeatingCoroutine != null)
        {
            StopCoroutine(repeatingCoroutine);
            repeatingCoroutine = null;
        }

        if (borderImage != null) borderImage.color = baseColor;
        transform.localScale = baseScale;
    }

    IEnumerator RepeatingStinger()
    {
        // small initial delay so the initial stinger is not immediately repeated
        yield return new WaitForSeconds(Mathf.Max(0.15f, repeatingInterval));
        while (true)
        {
            if (repeatingStinger != null)
                audioSource.PlayOneShot(repeatingStinger, stingerVolume);
            yield return new WaitForSeconds(repeatingInterval);
        }
    }
}
