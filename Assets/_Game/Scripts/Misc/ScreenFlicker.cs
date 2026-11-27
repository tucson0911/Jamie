using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class SpotlightFlicker2D : MonoBehaviour
{
    [Header("Intensity Settings")]
    [Tooltip("Minimum light intensity.")]
    [SerializeField] private float minIntensity = 0.5f;

    [Tooltip("Maximum light intensity.")]
    [SerializeField] private float maxIntensity = 1.5f;

    [Header("Flicker Settings")]
    [Tooltip("How fast the light flickers.")]
    [SerializeField] private float flickerSpeed = 8f;

    [Tooltip("Smoothly blends intensity changes using Perlin noise instead of sharp random jumps.")]
    [SerializeField] private bool smoothFlicker = true;

    [Tooltip("Randomizes the time between flicker steps (most noticeable when Smooth Flicker is off).")]
    [SerializeField] private bool randomInterval = true;

    [Range(0f, 1f)]
    [Tooltip("How much the interval can vary around the base interval (0 = steady, 1 = up to +/-100% variation).")]
    [SerializeField] private float intervalRandomness = 0.5f;

    private Light2D spotlight;
    private float noiseOffset;
    private float sampleTime;
    private float targetIntensity;
    private float currentInterval;
    private float stepTimer;

    private void Awake()
    {
        spotlight = GetComponent<Light2D>();
        noiseOffset = Random.Range(0f, 100f);
        targetIntensity = spotlight.intensity;
        currentInterval = CalculateNextInterval();
    }

    private void Update()
    {
        if (smoothFlicker)
        {
            float stepDelta = randomInterval
                ? Time.deltaTime * flickerSpeed * Random.Range(1f - intervalRandomness, 1f + intervalRandomness)
                : Time.deltaTime * flickerSpeed;

            sampleTime += stepDelta;
            float noise = Mathf.PerlinNoise(sampleTime, noiseOffset);
            spotlight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }
        else
        {
            stepTimer += Time.deltaTime;

            if (stepTimer >= currentInterval)
            {
                stepTimer = 0f;
                targetIntensity = Random.Range(minIntensity, maxIntensity);
                currentInterval = CalculateNextInterval();
            }

            spotlight.intensity = targetIntensity;
        }
    }

    private float CalculateNextInterval()
    {
        float baseInterval = flickerSpeed > 0f ? 1f / flickerSpeed : 1f;

        if (!randomInterval)
        {
            return baseInterval;
        }

        float multiplier = Random.Range(1f - intervalRandomness, 1f + intervalRandomness);
        return Mathf.Max(0.01f, baseInterval * multiplier);
    }

    private void OnValidate()
    {
        minIntensity = Mathf.Max(0f, minIntensity);
        maxIntensity = Mathf.Max(minIntensity, maxIntensity);
        flickerSpeed = Mathf.Max(0f, flickerSpeed);
        intervalRandomness = Mathf.Clamp01(intervalRandomness);
    }
}