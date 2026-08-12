using UnityEngine;

namespace Movement
{

    public class SimpleCharacterController : MonoBehaviour
    {
        public float moveSpeed = 5f;                // Movement speed of the character
        public float lookSpeed = 2f;                // Camera look speed
        public Camera playerCamera;                 // Reference to the player's camera
        public float gravity = -9.81f;              // Gravity applied to the character

        private CharacterController characterController;  // Reference to the CharacterController component
        private Vector3 velocity;                        // Current velocity of the character
        private float xRotation = 0f;                    // Current rotation around the x-axis (for looking up and down)
        private bool cursorVisible = false;              // Flag to track if the cursor is visible

        void Start()
        {
            characterController = GetComponent<CharacterController>();  // Get the CharacterController component on start
            Cursor.lockState = CursorLockMode.Locked;                   // Lock cursor to center of screen
            Cursor.visible = false;                                     // Make cursor invisible
        }

        void Update()
        {
            HandleMovement();           // Handle character movement
            HandleLook();               // Handle camera look
            ApplyGravity();             // Apply gravity to the character
            ToggleCursorVisibility();   // Toggle cursor visibility
        }

        void HandleMovement()
        {
            // Get horizontal and vertical input for movement
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");

            // Calculate movement direction based on current rotation
            Vector3 move = transform.right * moveX + transform.forward * moveZ;

            // Move the character based on direction, speed, and deltaTime
            characterController.Move(move * moveSpeed * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                // Get mouse input for camera rotation
                float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
                float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

                // Adjust rotation around x-axis (up and down look)
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);  // Clamp rotation to prevent flipping

                // Apply rotation to the player camera
                playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

                // Rotate the entire player object around the y-axis (left and right look)
                transform.Rotate(Vector3.up * mouseX);
            }
        }

        void ApplyGravity()
        {
            // Apply gravity to the character's velocity
            velocity.y += gravity * Time.deltaTime;

            // Move the character based on velocity
            characterController.Move(velocity * Time.deltaTime);
        }

        void ToggleCursorVisibility()
        {
            // Toggle cursor visibility on right mouse button click
            if (Input.GetMouseButtonDown(1))
            {
                cursorVisible = !cursorVisible;

                if (cursorVisible)
                {
                    Cursor.lockState = CursorLockMode.None;  // Unlock cursor
                    Cursor.visible = true;                    // Make cursor visible
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;  // Lock cursor to center
                    Cursor.visible = false;                    // Hide cursor
                }
            }
        }
    }
}
