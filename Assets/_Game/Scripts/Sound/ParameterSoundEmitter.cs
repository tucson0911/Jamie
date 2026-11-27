using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[AddComponentMenu("FMOD/Parameter Sound Emitter")]
public class ParameterSoundEmitter : MonoBehaviour
{
    [Header("FMOD Target")]
    [Tooltip("Leave empty to use a StudioEventEmitter on this same GameObject.")]
    [SerializeField] private StudioEventEmitter emitter;

    [Tooltip("Exact name of the local parameter on the FMOD event.")]
    [SerializeField] private string parameterName = "Proximity";

    [Header("Proximity Target & Ranges")]
    [Tooltip("Player or listener transform. Falls back to Camera.main if left empty.")]
    [SerializeField] private Transform target;

    [Min(0f)]
    [SerializeField] private float outerRange = 15f;

    [Min(0f)]
    [SerializeField] private float innerRange = 3f;

    [Header("Gizmos")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color outerGizmoColor = new Color(1f, 0.75f, 0.1f, 0.85f);
    [SerializeField] private Color innerGizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.85f);

    private PARAMETER_ID _parameterId;
    private bool _hasParameterId;
    private float _currentValue;

    public float CurrentParameterValue => _currentValue;

    private void OnValidate()
    {
        if (emitter == null)
        {
            emitter = GetComponent<StudioEventEmitter>();
        }

        // Keep innerRange from exceeding outerRange
        if (innerRange > outerRange)
        {
            innerRange = outerRange;
        }
    }

    private void Start()
    {
        if (emitter == null)
        {
            emitter = GetComponent<StudioEventEmitter>();
        }

        if (target == null && Camera.main != null)
        {
            target = Camera.main.transform;
        }

        CacheParameterId();
        UpdateProximityParameter(forceApply: true);
    }

    private void Update()
    {
        UpdateProximityParameter(forceApply: false);
    }

    private void CacheParameterId()
    {
        if (emitter == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        EventInstance instance = emitter.EventInstance;
        if (!instance.isValid())
        {
            return;
        }

        RESULT descResult = instance.getDescription(out EventDescription description);
        if (descResult != RESULT.OK || !description.isValid())
        {
            return;
        }

        RESULT paramResult = description.getParameterDescriptionByName(parameterName, out PARAMETER_DESCRIPTION paramDesc);
        if (paramResult == RESULT.OK)
        {
            _parameterId = paramDesc.id;
            _hasParameterId = true;
        }
        else
        {
            UnityEngine.Debug.LogWarning($"FMOD parameter '{parameterName}' not found on event for '{name}'.", this);
        }
    }

    private void UpdateProximityParameter(bool forceApply)
    {
        Transform activeTarget = target != null ? target : (Camera.main != null ? Camera.main.transform : null);
        if (activeTarget == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, activeTarget.position);
        float nextValue = CalculateProximityValue(distance, outerRange, innerRange);

        if (!forceApply && Mathf.Approximately(nextValue, _currentValue))
        {
            return;
        }

        _currentValue = nextValue;

        if (emitter == null)
        {
            return;
        }

        EventInstance instance = emitter.EventInstance;
        if (!instance.isValid())
        {
            return;
        }

        if (!_hasParameterId)
        {
            CacheParameterId();
        }

        if (_hasParameterId)
        {
            instance.setParameterByID(_parameterId, _currentValue);
        }
    }

    public static float CalculateProximityValue(float distance, float outer, float inner)
    {
        if (outer <= 0f)
        {
            return distance <= 0f ? 1f : 0f;
        }

        if (distance >= outer)
        {
            return 0f;
        }

        if (distance <= inner)
        {
            return 1f;
        }

        // InverseLerp maps outer -> 0 and inner -> 1
        return Mathf.InverseLerp(outer, inner, distance);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
        {
            return;
        }

        float clampedInner = Mathf.Min(innerRange, outerRange);

        Gizmos.color = outerGizmoColor;
        Gizmos.DrawWireSphere(transform.position, outerRange);

        Gizmos.color = innerGizmoColor;
        Gizmos.DrawWireSphere(transform.position, clampedInner);
    }
}