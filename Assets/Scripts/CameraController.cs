#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    [RequireComponent(typeof (Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] public float moveSpeed   =  10F;
        [SerializeField] public float scrollSpeed = 500F;

        private Camera? viewCamera;
        public Camera? ViewCamera => viewCamera;

        void Start()
        {
            // Get camera component
            viewCamera = GetComponent<Camera>();

        }

        void Update()
        {
            if (Test.Instance.IsPaused)
                return;

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