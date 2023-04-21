#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace MarkovCraft
{
    [RequireComponent(typeof (Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] public float moveSpeed   =  10F;
        [SerializeField] public float scrollSpeed = 500F;

        private Camera? viewCamera;
        public Camera? ViewCamera => viewCamera;

        private bool dragging = false;
        private Vector3 lastDragPos = Vector2.zero;

        void Start()
        {
            // Get camera component
            viewCamera = GetComponent<Camera>();

        }

        void Update()
        {
            if (Test.Instance.IsPaused)
            {
                dragging = false;
                return;
            }

            if (dragging) // Perform dragging
            {
                if (Input.GetMouseButton(1))
                {
                    var curDragPos = Input.mousePosition;
                    var dragOffset = curDragPos - lastDragPos;
                    transform.position -= transform.right * dragOffset.x * 0.2F + transform.up * dragOffset.y * 0.2F;

                    lastDragPos = curDragPos;
                }
                else
                    dragging = false;
            }
            else // Check start dragging
            {
                if (Input.GetMouseButton(1) && !EventSystem.current.IsPointerOverGameObject())
                {
                    dragging = true;
                    lastDragPos = Input.mousePosition;
                }

            }

            float hor = Input.GetAxis("Horizontal");
            float ver = Input.GetAxis("Vertical");

            float scroll = Input.GetAxis("Mouse ScrollWheel");

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
                transform.position += transform.forward * scroll * scrollSpeed;
            }

        }
    }
}