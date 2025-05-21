using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ParticleSystem))]
public class FogDistanceActivator : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Distance Settings")]
    public float activationDistance = 20f;
    public float deactivationDistance = 25f;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;
    public Color baseColor = Color.white;

    private ParticleSystem ps;
    private ParticleSystem.ColorOverLifetimeModule colorOverLifetime;
    private Gradient gradient;
    private Coroutine fadeRoutine;

    private float currentAlpha = 0f;
    private bool isActive = false;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        if (player == null && Camera.main != null)
            player = Camera.main.transform;

        colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        UpdateColorOverLifetime(0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        
        if (dist <= activationDistance && !isActive)
        {
            isActive = true;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            ps.Play();
            fadeRoutine = StartCoroutine(FadeAlpha(currentAlpha, 1f));
        }

        
        else if (dist > deactivationDistance && isActive)
        {
            isActive = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeAlpha(currentAlpha, 0f));
        }
    }

    IEnumerator FadeAlpha(float fromAlpha, float toAlpha)
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;
            currentAlpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            UpdateColorOverLifetime(currentAlpha);
            timer += Time.deltaTime;
            yield return null;
        }

        currentAlpha = toAlpha;
        UpdateColorOverLifetime(toAlpha);

        if (toAlpha == 0f)
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void UpdateColorOverLifetime(float alpha)
    {
        gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(baseColor, 0f),
                new GradientColorKey(baseColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(alpha, 0f),
                new GradientAlphaKey(alpha, 1f)
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
    }
}
