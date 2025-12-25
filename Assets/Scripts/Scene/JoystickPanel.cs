using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MarkovCraft
{
    /// <summary>
    /// UI control that turns mouse/touch drags into a 2D movement vector (dir + magnitude).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class JoystickPanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform handle;
        [SerializeField] private RectTransform background;
        [SerializeField] private Camera uiCameraOverride;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        [SerializeField] private Button rotateLeftButton;
        [SerializeField] private Button rotateRightButton;

        [Header("Behavior")]
        [SerializeField] [Min(1F)] private float maxRadius = 120F;
        [SerializeField] [Range(0F, 1F)] private float deadZone = 0.1F;

        [Header("Events")]
        [SerializeField] private UnityEvent<Vector2> onValueChanged;

        public Vector2 Value { get; private set; }
        public bool IsHeld { get; private set; }
        public float Magnitude => Value.magnitude;
        
        public bool ZoomInButtonIsHeld { get; private set; }
        public bool ZoomOutButtonIsHeld { get; private set; }
        public bool RotateLeftButtonIsHeld { get; private set; }
        public bool RotateRightButtonIsHeld { get; private set; }

        private RectTransform rectTransform;
        private Canvas parentCanvas;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();

            ResetStick();
        }

        public void UpdateZoomInButtonHeldStatus(bool held)
        {
            ZoomInButtonIsHeld = held;
        }
        
        public void UpdateZoomOutButtonHeldStatus(bool held)
        {
            ZoomOutButtonIsHeld = held;
        }

        public void UpdateRotateLeftButtonHeldStatus(bool held)
        {
            RotateLeftButtonIsHeld = held;
        }

        public void UpdateRotateRightButtonHeldStatus(bool held)
        {
            RotateRightButtonIsHeld = held;
        }

        private Camera ResolveCamera()
        {
            if (uiCameraOverride) return uiCameraOverride;
            if (!parentCanvas) return null;
            return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            if (TryGetLocalPoint(eventData, out var localPoint))
            {
                UpdateValue(localPoint);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsHeld) return;
            if (TryGetLocalPoint(eventData, out var localPoint))
            {
                UpdateValue(localPoint);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            ResetStick();
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                           eventData.position,
                                                                           ResolveCamera(),
                                                                           out localPoint);
        }

        private void UpdateValue(Vector2 currentLocalPoint)
        {
            var rect = rectTransform.rect;
            // Keep pointer inside panel bounds
            var clampedPoint = new Vector2(
                Mathf.Clamp(currentLocalPoint.x, rect.xMin, rect.xMax),
                Mathf.Clamp(currentLocalPoint.y, rect.yMin, rect.yMax)
            );

            // Offset relative to center
            var offset = clampedPoint - rectTransform.rect.center;

            // Optional radial clamp to maxRadius to cap magnitude
            if (offset.sqrMagnitude > maxRadius * maxRadius)
            {
                offset = offset.normalized * maxRadius;
            }

            var newValue = offset; // Raw, not normalized

            // Dead zone expressed as fraction of maxRadius
            if (maxRadius > 0F && (newValue.magnitude / maxRadius) < deadZone)
            {
                newValue = Vector2.zero;
            }

            Value = newValue;
            onValueChanged?.Invoke(Value);

            if (handle)
            {
                handle.anchoredPosition = offset;
            }
        }

        private void ResetStick()
        {
            Value = Vector2.zero;
            onValueChanged?.Invoke(Value);

            if (handle)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }
    }
}
