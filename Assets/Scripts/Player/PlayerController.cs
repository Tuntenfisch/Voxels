using Tuntenfisch.Voxels.CSG;
using Tuntenfisch.Voxels.Materials;
using Tuntenfisch.World;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Tuntenfisch.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        private float Gravity => Physics.gravity.y;

        private const float c_minDownwardVelocity = -2.0f;

        [Header("Movement")]
        [Min(1.0f)]
        [SerializeField]
        private float m_movementSpeed = 5.0f;
        [Min(1.0f)]
        [SerializeField]
        private float m_jumpHeight = 1.5f;

        [Header("Look")]
        [Range(0.0f, 1.0f)]
        [SerializeField]
        private float m_lookSensitivity = 0.05f;
        [SerializeField]
        private Camera m_camera;

        private CharacterController m_controller;
        private int m_playerLayerMask;

        private float2 m_moveDelta;
        private bool m_wantsToJump;
        private float2 m_lookDelta;
        private float2 m_rotation;
        private float3 m_velocity;
        private bool m_primaryDown;
        private bool m_secondaryDown;

        private void Start()
        {
            m_controller = GetComponent<CharacterController>();
            m_playerLayerMask = LayerMask.GetMask("Player");
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            ApplyMovement();
            ApplyLook();
            HandleWorldInteraction();
        }

        public void OnMove(InputValue value) => m_moveDelta = value.Get<Vector2>();

        public void OnJump() => m_wantsToJump = m_controller.isGrounded;

        public void OnLook(InputValue value) => m_lookDelta = value.Get<Vector2>();

        public void OnPrimary(InputValue value) => m_primaryDown = value.isPressed;

        public void OnSecondary(InputValue value) => m_secondaryDown = value.isPressed;

        private void ApplyMovement()
        {
            if (m_controller.isGrounded)
            {
                m_velocity.y = m_wantsToJump ? math.sqrt(-2.0f * Gravity * m_jumpHeight) : c_minDownwardVelocity;
                m_wantsToJump = false;
            }
            else
            {
                m_velocity.y += Gravity * Time.deltaTime;
            }

            m_velocity.xz = (((float3)transform.right).xz * m_moveDelta.x + ((float3)transform.forward).xz * m_moveDelta.y) * m_movementSpeed;
            m_controller.Move(m_velocity * Time.deltaTime);
        }

        private void ApplyLook()
        {
            m_rotation.y += m_lookDelta.x * m_lookSensitivity;
            m_rotation.x -= m_lookDelta.y * m_lookSensitivity;
            m_rotation.x = math.clamp(m_rotation.x, -90.0f, 90.0f);

            m_camera.transform.localRotation = Quaternion.Euler(m_rotation.x, 0.0f, 0.0f);
            transform.localRotation = Quaternion.Euler(0.0f, m_rotation.y, 0.0f);

            m_lookDelta = 0.0f;
        }

        private void HandleWorldInteraction()
        {
            Ray ray = new Ray(m_camera.transform.position, m_camera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~m_playerLayerMask))
            {
                GPUCSGPrimitive primitive = new GPUCSGPrimitive(CSGPrimitiveType.Sphere);
                float3 scale = 4.0f;

                WorldManager.Instance.DrawCSGPrimitiveHologram(primitive.PrimitiveType, hit.point, scale);

                if (m_primaryDown)
                {
                    WorldManager.Instance.ApplyCSGOperation(new GPUCSGOperator(CSGOperatorIndex.Union), primitive, MaterialIndex.Dirt, hit.point, scale);
                }

                if (m_secondaryDown)
                {
                    WorldManager.Instance.ApplyCSGOperation(new GPUCSGOperator(CSGOperatorIndex.Difference), primitive, default, hit.point, scale);
                }
            }
        }
    }
}