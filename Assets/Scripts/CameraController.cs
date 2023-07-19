#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace MarkovCraft
{
    [RequireComponent(typeof (Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private ScreenManager? screenManager;

        [SerializeField] public float moveSpeed   =  10F;
        [SerializeField] public float scrollSpeed = 500F;
        [SerializeField] public float zPosLimit   = 320F;

        private Camera? viewCamera;
        public Camera? ViewCamera => viewCamera;

        private bool dragging = false;
        private Vector3 lastDragPos = Vector2.zero;

        private float zPosition = 0F;

        void Start()
        {
            // Get camera component
            viewCamera = GetComponent<Camera>();

            zPosition = zPosLimit / 2F;
        }

        void Update()
        {
            if (screenManager != null && !screenManager.AllowsMovementInput)
            {
                dragging = false;
                return;
            }

            var pointerOverUI = EventSystem.current.IsPointerOverGameObject();

            if (dragging) // Perform dragging
            {
                if (Input.GetMouseButton(1))
                {
                    var curDragPos = Input.mousePosition;
                    var dragOffset = curDragPos - lastDragPos;

                    var dragMultiplier = (zPosLimit * 1.1F - zPosition) * 0.1F / zPosLimit;
                    transform.position -= dragMultiplier * (transform.right * dragOffset.x + transform.up * dragOffset.y);

                    lastDragPos = curDragPos;
                }
                else
                    dragging = false;
            }
            else // Check start dragging
            {

                if (Input.GetMouseButton(1) && !pointerOverUI)
                {
                    dragging = true;
                    lastDragPos = Input.mousePosition;
                }

            }

            float hor = Input.GetAxis("Horizontal");
            float ver = Input.GetAxis("Vertical");

            float scroll = pointerOverUI ? 0F : Input.GetAxis("Mouse ScrollWheel");

            if (hor != 0F)
            {
                transform.position += transform.right * hor * Time.deltaTime * moveSpeed;
            }

            if (ver != 0F)
            {
                transform.position += transform.up * ver * Time.deltaTime * moveSpeed;
            }

            if (scroll != 0F)
            {
                var newZPosition = Mathf.Clamp(zPosition + scroll * scrollSpeed, zPosLimit / 4F, zPosLimit);
                transform.position += transform.forward * (newZPosition - zPosition);

                zPosition = newZPosition;
            }

        }
    }
}