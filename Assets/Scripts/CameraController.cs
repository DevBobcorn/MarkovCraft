#nullable enable
using UnityEngine;

namespace MarkovBlocks
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] public float moveSpeed = 10F;

        void Update()
        {
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
                transform.position += transform.forward * (scroll * 100F) * Time.deltaTime * moveSpeed;
                Debug.Log($"Scroll {scroll}");
            }

        }
    }
}