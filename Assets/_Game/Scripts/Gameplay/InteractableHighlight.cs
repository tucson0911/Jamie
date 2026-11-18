using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Jamie.Gameplay
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class InteractableHighlight : MonoBehaviour
    {
        [SerializeField] private WorldInteractor interactor;
        [SerializeField] private PlayerFlashlight flashlight;
        [SerializeField] private Light2D beam;
        [SerializeField] private Light2D visionCone;
        [SerializeField, Min(0.1f)] private float highlightDistance = 6f;
        [SerializeField, Range(0f, 1f)] private float strength = 0.3f;

        private static readonly int StrengthId = Shader.PropertyToID("_HighlightStrength");
        private MaterialPropertyBlock properties;
        private readonly List<Collider2D> nearby = new();
        private readonly HashSet<WorldInteractable> visited = new();
        private readonly HashSet<SpriteRenderer> highlighted = new();
        private readonly List<SpriteRenderer> sprites = new();

        private void Awake() => properties = new MaterialPropertyBlock();

        private void LateUpdate()
        {
            Clear();
            if (interactor == null || !interactor.isActiveAndEnabled ||
                !interactor.Player.isActiveAndEnabled || Time.timeScale <= 0f)
                return;

            ContactFilter2D filter = new();
            filter.SetLayerMask(interactor.TargetLayers);
            filter.useTriggers = true;
            nearby.Clear();
            visited.Clear();
            Physics2D.OverlapCircle(interactor.Player.transform.position,
                highlightDistance, filter, nearby);

            foreach (Collider2D collider in nearby)
            {
                WorldInteractable target = collider.GetComponentInParent<WorldInteractable>();
                if (target == null || !visited.Add(target) ||
                    !target.isActiveAndEnabled || !target.CanInteract(interactor))
                    continue;

                bool torchOn = flashlight != null && flashlight.IsOn;
                if (!torchOn && !target.HighlightWithoutFlashlight)
                    continue;
                Light2D source = torchOn ? beam : visionCone;
                if (source == null || !source.isActiveAndEnabled)
                    continue;

                Vector2 point = target.GetInteractionPoint(source.transform.position);
                Vector2 localPoint = source.transform.InverseTransformPoint(point);
                float distance = Vector2.Distance(point, interactor.Player.transform.position);
                if (distance > highlightDistance || localPoint.magnitude > source.pointLightOuterRadius ||
                    Vector2.Angle(Vector2.up, localPoint) > source.pointLightOuterAngle * 0.5f ||
                    !interactor.HasClearPath(target))
                    continue;

                sprites.Clear();
                target.GetComponentsInChildren(true, sprites);
                foreach (SpriteRenderer sprite in sprites)
                {
                    if (sprite == null || !sprite.enabled || !sprite.gameObject.activeInHierarchy ||
                        sprite.sharedMaterial == null || !sprite.sharedMaterial.HasProperty(StrengthId))
                        continue;
                    sprite.GetPropertyBlock(properties);
                    properties.SetFloat(StrengthId, strength);
                    sprite.SetPropertyBlock(properties);
                    highlighted.Add(sprite);
                }
            }
        }

        private void Clear()
        {
            if (properties == null) return;
            foreach (SpriteRenderer sprite in highlighted)
            {
                if (sprite == null) continue;
                sprite.GetPropertyBlock(properties);
                properties.SetFloat(StrengthId, 0f);
                sprite.SetPropertyBlock(properties);
            }
            highlighted.Clear();
        }

        private void OnDisable() => Clear();
    }
}
