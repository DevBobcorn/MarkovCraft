#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;

namespace MarkovCraft
{
    [RequireComponent(typeof (Camera))]
    public class CameraController : MonoBehaviour
    {
        private static readonly Plane REFERENCE_PLANE = new(Vector3.up, Vector3.zero);
        private static readonly Vector2 VIEWPORT_ROTATE_CENTER = new(0.5F, 0.3F);
        [SerializeField] private ScreenManager? screenManager;
        [SerializeField] [Range( 1F, 1000F)] private float moveSpeed   =  10F;
        [SerializeField] [Range( 1F, 1000F)] private float turnSpeed   =  10F;
        [SerializeField] [Range( 1F, 1000F)] private float scrollSpeed = 500F;
        [SerializeField] [Range(10F, 1000F)] private float yPosMin   =  15F;
        [SerializeField] [Range(10F, 1000F)] private float yPosMax   = 325F;
        private float yPosition = 0F;

        public Camera? ViewCamera { get; private set; }

        private bool dragging = false, dragRotating = false;
        private Vector3 lastDragPos = Vector2.zero;
        private Vector3? targetPos = null;

        public void SetCenterPosition(Vector3 newCenter)
        {
            var ray = !ViewCamera ?
                    new Ray(transform.position, transform.forward) :
                    ViewCamera.ViewportPointToRay(VIEWPORT_ROTATE_CENTER);
            REFERENCE_PLANE.Raycast(ray, out float dist);

            newCenter.y = 0F;
            // Update current position
            targetPos = newCenter - ray.direction * dist;
        }

        private void Start()
        {
            // Get camera component
            ViewCamera = GetComponent<Camera>();
            // Update initial y position
            yPosition = transform.position.y;
        }

        private void Update()
        {
            if (targetPos != null)
            {
                // Moving towards a target point, ignore user input and get to the target point
                transform.position = Vector3.MoveTowards(transform.position, targetPos.Value, moveSpeed * 3F * Time.deltaTime);

                if (transform.position == targetPos.Value) // Target point reached
                {
                    // Clear target point
                    targetPos = null;
                }
            }

            if (screenManager && !screenManager.AllowsMovementInput)
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

                    var dragMultiplier = yPosition / yPosMax * 0.3F;
                    var newPos = transform.position - dragMultiplier * (transform.right * dragOffset.x + transform.up * dragOffset.y);

                    newPos.y = Mathf.Clamp(newPos.y, yPosMin, yPosMax);

                    transform.position = newPos;

                    lastDragPos = curDragPos;
                }
                else
                {
                    dragging = false;
                }
            }
            else // Check start dragging
            {
                if (!dragRotating && Input.GetMouseButton(1) && !pointerOverUI)
                {
                    dragging = true;
                    lastDragPos = Input.mousePosition;
                }
            }

            float hor = Input.GetAxis("Horizontal");
            float ver = Input.GetAxis("Vertical");
            float fly = 0F;
            float rot = 0F;

            if (dragRotating) // Perform dragging
            {
                if (Input.GetMouseButton(2))
                {
                    var curDragPos = Input.mousePosition;
                    var dragOffset = curDragPos - lastDragPos;

                    rot = dragOffset.x * Time.deltaTime * 30F;

                    lastDragPos = curDragPos;
                }
                else
                {
                    dragRotating = false;
                }
            }
            else // Check start dragging
            {
                if (!dragging && Input.GetMouseButton(2) && !pointerOverUI)
                {
                    dragRotating = true;
                    lastDragPos = Input.mousePosition;
                }
                else
                {
                    if (Input.GetKey(KeyCode.E)) // Turn camera counter-clockwise
                    {
                        rot += 1F;
                    }

                    if (Input.GetKey(KeyCode.Q)) // Turn camera clockwise
                    {
                        rot -= 1F;
                    }
                }
            }

            if (Input.GetKey(KeyCode.Space)) // Fly up
            {
                fly += 1F;
            }

            if (Input.GetKey(KeyCode.LeftShift)) // Fly down
            {
                fly -= 1F;
            }

            float scroll = pointerOverUI ? 0F : Input.GetAxis("Mouse ScrollWheel");

            if (rot != 0F) // Turn camera
            {
                var ray = !ViewCamera ?
                        new Ray(transform.position, transform.forward) :
                        ViewCamera.ViewportPointToRay(VIEWPORT_ROTATE_CENTER);
                REFERENCE_PLANE.Raycast(ray, out float dist);

                var hitPoint = transform.position + ray.direction * dist;
                Debug.DrawLine(hitPoint, hitPoint + Vector3.up * 50F);

                var eulers = transform.eulerAngles;
                transform.localEulerAngles = new Vector3(eulers.x, eulers.y + rot * turnSpeed * Time.deltaTime, eulers.z);

                // Get ray direction after the rotation
                var newRayDirection = !ViewCamera ? transform.forward:
                        ViewCamera.ViewportPointToRay(VIEWPORT_ROTATE_CENTER).direction;

                // Update current position
                transform.position = hitPoint - newRayDirection * dist;
            }

            if (hor != 0F) // Movement in horizontal direction - Left / Right
            {
                transform.position += transform.right * (hor * Time.deltaTime * moveSpeed);
            }

            if (ver != 0F) // Movement in horizontal direction - Forward / Back
            {
                var horizontalForward = transform.up;
                horizontalForward.y = 0;
                horizontalForward = horizontalForward.normalized;

                transform.position += horizontalForward * (ver * Time.deltaTime * moveSpeed);
            }

            if (fly != 0F) // Movement in vertical direction - Up / Down
            {
                var newPos = transform.position + Vector3.up * (fly * Time.deltaTime * moveSpeed);
                newPos.y = Mathf.Clamp(newPos.y, yPosMin, yPosMax);

                transform.position = newPos;
            }

            if (scroll != 0F) // Adjust camera distance to ground - Near / Far
            {
                // Initial amount of movement
                var moveAmount = scrollSpeed * scroll * transform.forward;
                var newYPos = yPosition + moveAmount.y;

                // Max limit check
                if (moveAmount.y > 0F && newYPos > yPosMax)
                {
                    moveAmount *= (yPosMax - yPosition) / (newYPos - yPosition);
                }

                // Min limit check
                if (moveAmount.y < 0F && newYPos < yPosMin)
                {
                    moveAmount *= (yPosition - yPosMin) / (yPosition - newYPos);
                }
                
                transform.position += moveAmount;
                yPosition = transform.position.y;
            }
        }
    }
}