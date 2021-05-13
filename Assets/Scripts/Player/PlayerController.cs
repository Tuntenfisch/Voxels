using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tuntenfisch.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [Range(1.0f, 10.0f)]
        [SerializeField]
        private float m_movementSpeed = 5.0f;
        [Range(1.0f, 10.0f)]
        [SerializeField]
        private float m_jumpHeight = 1.5f;

        [Header("Look")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_lookSensitivity = 0.1f;
        [SerializeField]
        private Camera m_camera;

        private float Gravity => Physics.gravity.y;

        private CharacterController m_controller;

        private float2 m_moveDelta;
        private float2 m_lookDelta;
        private bool m_wantsToJump;
        private float2 m_rotation;
        private float m_velocityY;

        public void OnMove(InputAction.CallbackContext context) => m_moveDelta = context.ReadValue<Vector2>();

        public void OnJump(InputAction.CallbackContext context) => m_wantsToJump = context.ReadValueAsButton();

        // OnLook(...) can be called multiple times per frame and the (mouse) delta is framerate independent.
        // Every time OnLook(...) is called we add the received delta to an accumulator. Once Update(...) is
        // called we apply the accumulated delta once and reset it.
        public void OnLook(InputAction.CallbackContext context) => m_lookDelta += (float2)context.ReadValue<Vector2>();

        private void Start()
        {
            m_controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ApplyLook()
        {
            m_rotation.y += m_lookDelta.x * m_lookSensitivity;
            m_rotation.x -= m_lookDelta.y * m_lookSensitivity;
            m_rotation.x = math.clamp(m_rotation.x, -90.0f, 90.0f);

            m_camera.transform.localRotation = Quaternion.Euler(m_rotation.x, 0.0f, 0.0f);
            transform.localRotation = Quaternion.Euler(0.0f, m_rotation.y, 0.0f);

            // Reset the accumulated delta.
            m_lookDelta = float2.zero;
        }

        private void Jump()
        {
            m_velocityY = math.sqrt(-2.0f * Gravity * m_jumpHeight);
        }

        private void ApplyMovement()
        {
            if (m_wantsToJump && m_controller.isGrounded)
            {
                Jump();
            }

            m_velocityY += Gravity * Time.deltaTime;

            float3 horizontalVelocity = (transform.right * m_moveDelta.x + transform.forward * m_moveDelta.y) * m_movementSpeed;
            float3 verticalVelocity = new float3(0.0f, 1.0f, 0.0f) * m_velocityY;

            m_controller.Move((horizontalVelocity + verticalVelocity) * Time.deltaTime);

            if (m_controller.isGrounded)
            {
                m_velocityY = 0.0f;
            }
        }

        private void Update()
        {
            ApplyLook();
            ApplyMovement();
        }
    }
}