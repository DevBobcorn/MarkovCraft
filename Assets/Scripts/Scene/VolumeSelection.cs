#nullable enable
using UnityEngine;

namespace MarkovCraft
{
    public class VolumeSelection : MonoBehaviour
    {
        [SerializeField] public Vector3 VolumePosition;
        [SerializeField] public Vector3 VolumeSize;
        [SerializeField] public Material? MatA, MatB, MatLocked;
        [SerializeField] public float TransformSpeed = 20F;

        [HideInInspector] public bool Locked = false;

        private bool transforming = false;

        public void HideVolume()
        {
            if (Locked) Unlock();

            // Shrink in-place
            UpdateVolume(transform.position, Vector3.zero);

        }

        public void Lock()
        {
            // Move to target transformation immediately
            transform.position = VolumePosition;
            transform.localScale = VolumeSize;

            transforming = false;
            Locked = true;

            if (MatLocked)
                GetComponent<MeshRenderer>().sharedMaterial = MatLocked;
        }

        public void Unlock()
        {
            Locked = false;

            if (MatLocked)
                GetComponent<MeshRenderer>().sharedMaterial = MatB;
        }

        public void UpdateVolume(Vector3 pos, Vector3 size)
        {
            if (VolumePosition == pos && VolumeSize == size)
                return; // Nothing to update

            VolumePosition = pos;
            VolumeSize = size;

            // Adjust center for bottom face to render properly
            VolumePosition.y += 0.5F + 0.01F;
            VolumeSize.y -= 1F;

            // Teleport to target pos if currently hidden
            if (transform.localScale == Vector3.zero)
            {
                transform.localPosition = VolumePosition;
                // Start scaling up from Vector3.one
                transform.localScale = Vector3.one;
            }

            StartTransformation();
        }

        private void StartTransformation()
        {
            transforming = true;

            if (MatA)
                GetComponent<MeshRenderer>().sharedMaterial = MatA;
        }

        private void EndTransformation()
        {
            transforming = false;

            if (MatB)
                GetComponent<MeshRenderer>().sharedMaterial = MatB;
        }

        private void Update()
        {
            if (!transforming) return;

            transform.position = Vector3.MoveTowards(transform.position, VolumePosition, TransformSpeed * Time.deltaTime);
            transform.localScale = Vector3.MoveTowards(transform.localScale, VolumeSize, TransformSpeed * Time.deltaTime);

            if (transform.position == VolumePosition && transform.localScale == VolumeSize)
                EndTransformation();
        }
    }
}