//* Created by Morgan Finney
//* A Player Controller for Unity using Rigidbody Physics.
//* Jan 2023

//* TODO: Slide down slopes that are to steep
//* TODO: Stop player from sliding down slopes when they are not moving 
//* TODO: Stop players being able to go up diagonally (maybe)
//* TODO: Stop the player from jittering when they are at the edge of a slope that they are unable to walk on
//* TODO: Make Orbit camera work of alt key
//* TODO: Add stairs
//* TODO: Smooth crouching (maybe)
//* TODO: Move with platforms (maybe)
//* FIXME: Jump is outside of the physics update loop (maybe)



using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using Cinemachine;

namespace pdox.RBPC
{
    enum CameraMode
    {
        FirstPerson,
        ThirdPerson,
        Orbit
    }

    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        #region Variables

        [Header("Editor Settings")]
        [Tooltip("Editor Debugging")][SerializeField] private bool m_Debug = false;

        [Space]
        [Header("Camera Settings")]
        [Tooltip("Camera Zoom Multiplier")][SerializeField] private float m_CameraZoomMultiplier = 30f;
        [Tooltip("Current Camera Mode")][SerializeField] private CameraMode m_CameraMode = CameraMode.FirstPerson;
        [Tooltip("Reference to the camera holder")][SerializeField] private Transform m_CameraHolderTransform;
        [Tooltip("Reference to the cinemachine camera")][SerializeField] private CinemachineVirtualCamera m_1stPersonCamera;
        [Tooltip("Reference to the cinemachine camera")][SerializeField] private CinemachineVirtualCamera m_3rdPersonCamera;
        [Tooltip("Reference to the cinemachine camera")][SerializeField] private CinemachineVirtualCamera m_OrbitCamera;
        private float m_3rdPersonCameraMinDistance = 2f;
        private float m_3rdPersonCameraMaxDistance = 10f;
        private float m_OrbitCameraMinDistance = 2f;
        private float m_OrbitCameraMaxDistance = 10f;
        private CinemachineVirtualCamera m_CurrentCamera;
        private CinemachineFramingTransposer m_3rdPersonTransposer = null;
        private CinemachineFramingTransposer m_OrbitTransposer = null;

        [Space]
        [Header("Movement Settings")]
        [Tooltip("Player Walk Speed in Meters Per Second")][SerializeField] private float m_WalkSpeed = 1.8f;
        [Tooltip("Player Sprint Speed in Meters Per Second")][SerializeField] private float m_SprintSpeed = 3f;
        [Tooltip("Player Crouch Speed Multiplier")][SerializeField] private float m_CrouchSpeedMultiplier = 0.8f;
        [Tooltip("Player Rotation Speed Multiplier")][SerializeField] private float m_RotationSpeedMultiplier = 17.5f;
        [Tooltip("Maximum Slope Angle the player can walk on")][SerializeField] private float m_SlopeAngleMax = 42.5f;
        [Tooltip("Slope angle where players speed should begin to be effected")][SerializeField] private float m_SlopeAngleFalloff = 32.5f;
        private float m_CurrentSpeed = 0f;

        [Space]
        [Header("Jump Settings")]
        [Tooltip("Player Jump Force Multiplier")][SerializeField] private float m_JumpForceMultiplier = 1f;
        [Tooltip("Coyote Time in Seconds")][SerializeField] private float m_CoyoteTime = 0.1f;
        [Tooltip("Jump Honor Buffer Time in Seconds")][SerializeField] private float m_JumpHonorBufferTime = 0.1f;
        [Tooltip("Time player has to be on ground to activate coyote time in Seconds")][SerializeField] private float m_CoyoteGroundTime = 0.1f;
        private float m_JumpHonorBufferTimeRemaining = 0f;
        private float m_CoyoteTimeRemaining = 0f;
        private float m_CoyoteGroundTimeTotal = 0f;
        private bool m_IsGrounded = true;
        private float m_GroundCheckRadius;
        private Vector3 m_GroundCheckPositionOffset;
        private float m_GroundCheckDistance;

        [Space]
        [Header("Crouch Settings")]
        [Tooltip("Crouch Amount")][SerializeField] private float m_CrouchAmount = 0.5f;
        private bool m_IsCrouching = false;
        private float m_PlayerHeight;
        private float m_CrouchHeight;

        [Space]
        [Header("General Settings")]
        private Rigidbody m_Rigidbody;
        private CapsuleCollider m_Collider;

        [Space]
        [Header("Input Settings")]
        private Vector2 m_MoveInput;
        private Vector2 m_LookInput, m_PreviousLookInput;
        private float m_LookRotationX, m_LookRotationY;
        [SerializeField] PickUpController m_PickUpController;
       
        [SerializeField] DoorRayCast m_DoorRayCast;

        #endregion

        #region Input Events
        //* Called for WASD and Arrow Keys or Gamepad Left Stick
        public void OnMove(CallbackContext a_Context)
        {
            m_MoveInput = a_Context.ReadValue<Vector2>();
        }

        //* Called for Mouse or Gamepad Right Stick
        public void OnLook(CallbackContext a_Context)
        {
            m_LookInput = a_Context.ReadValue<Vector2>();
        }

        //* Called for LMB or Gamepad Right Trigger
        public void OnFire(CallbackContext a_Context)
        {
           m_PickUpController.SendMessage("PickUpInput");
          
           m_DoorRayCast.SendMessage("PlayAnimation");
        }

        //* Called for Left Ctrl or Gamepad West Button
        public void OnCrouch(CallbackContext a_Context)
        {
            if (a_Context.started)
            {
                m_IsCrouching = true;
            }
            else if (a_Context.canceled)
            {
                m_IsCrouching = false;
            }
        }

        //* Called for Space or Gamepad South Button
        public void OnJump(CallbackContext a_Context)
        {
            if (a_Context.started)
            {
                //* If the player is grounded, jump
                if (m_IsGrounded)
                {
                    DoJump();
                }
                //* If the player is not grounded, but has coyote time remaining, jump
                else if (m_CoyoteTimeRemaining > 0f)
                {
                    DoJump();
                }
                //* If the player is not grounded and has no coyote time remaining, set the jump honor buffer time
                else
                {
                    m_JumpHonorBufferTimeRemaining = m_JumpHonorBufferTime;
                }
            }
        }

        //* Called for Left Shift or Gamepad Left Trigger
        public void OnSprint(CallbackContext a_Context)
        {
            if (a_Context.started)
            {
                m_CurrentSpeed = m_SprintSpeed;
            }
            else if (a_Context.canceled)
            {
                m_CurrentSpeed = m_WalkSpeed;
            }
        }

        //* Called for V or Gamepad Right Stick Press
        public void OnSwitchCamera(CallbackContext a_Context)
        {
            if (a_Context.performed)
            {
                //SwitchCamera();
            }
        }

        //* Called for Mouse Scroll Wheel or Gamepad D-Pad Up/Down
        public void OnCameraZoom(CallbackContext a_Context)
        {
            if (a_Context.performed)
            {
                float l_ZoomInput = a_Context.ReadValue<float>();
                ZoomCamera(l_ZoomInput);
            }
        }
        #endregion

        #region Unity Methods

        //* Awake is called when the script instance is being loaded
        private void Awake()
        {
            
    
        
             // Locks the cursor
            Cursor.lockState = CursorLockMode.Locked;
            


            //* Set the current speed to the walk speed
            m_CurrentSpeed = m_WalkSpeed;

            //* Get the rigidbody and collider
            m_Rigidbody = GetComponent<Rigidbody>();
            m_Collider = GetComponent<CapsuleCollider>();

            //* Get the radius of the collider
            m_GroundCheckRadius = m_Collider.radius * 0.9f;

            //* Get the height of the player and the crouch height
            m_PlayerHeight = m_Collider.height;
            m_CrouchHeight = m_PlayerHeight - m_CrouchAmount;

            //* Get the offset of the collider
            float l_OffsetY = m_Collider.radius;
            m_GroundCheckPositionOffset = new Vector3(0.0f, l_OffsetY, 0.0f);

            //* Get the distance of the ground check
            m_GroundCheckDistance = m_Collider.radius * 0.25f;

            //* Get the cameras
            m_3rdPersonTransposer = m_3rdPersonCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            m_OrbitTransposer = m_OrbitCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        }

        //* Start is called before the first frame update
        void Start()
        {   
            //* Set the camera mode to it's default
            m_CameraMode = CameraMode.Orbit;
            SwitchCamera();
        }

        //* Update is called once per frame
        void Update()
        {
        }

        //* FixedUpdate is called every fixed framerate loop
        private void FixedUpdate()
        {
            //* Check if the player is grounded and has a jump honor buffer
            if (m_IsGrounded && m_JumpHonorBufferTimeRemaining > 0f)
            {
                DoJump();
            }

            //* Do not rotate the player if the camera is in orbit mode
            if (m_CameraMode != CameraMode.Orbit)
                RotatePlayer();

            MovePlayer();

            HandelCrouch();

            JumpTimers();

        }

        //* LateUpdate called after all Update functions have been called
        private void LateUpdate()
        {

        }

        //* OnDrawGizmos is called when the script is loaded or a value is changed in the inspector
        private void OnDrawGizmos()
        {
            if (!m_Debug)
                return;

            Gizmos.color = Color.yellow;
            Vector3 l_GroundCheckPosition = transform.position + m_GroundCheckPositionOffset;
            Gizmos.DrawWireSphere(l_GroundCheckPosition, m_GroundCheckRadius);
            Vector3 l_FinalGroundCheckPosition = l_GroundCheckPosition + (Vector3.down * m_GroundCheckDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(l_GroundCheckPosition, l_FinalGroundCheckPosition);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(l_FinalGroundCheckPosition, m_GroundCheckRadius);

        }
        #endregion

        #region Custom Methods
        private void MovePlayer()
        {
            //* Get the current speed
            float l_CurrentSpeed = m_CurrentSpeed;

            //* If the player is crouching, reduce the speed
            if (m_IsCrouching)
                l_CurrentSpeed *= m_CrouchSpeedMultiplier;

            //* Get the current velocity
            Vector3 l_CurrentVelocity = m_Rigidbody.velocity;
            //* Get the target velocity from the input
            Vector3 l_TargetVelocity = new Vector3(m_MoveInput.x, 0.0f, m_MoveInput.y);
            //* Multiply the target velocity by the current speed
            l_TargetVelocity *= m_CurrentSpeed;

            //* A quaternion to store the slope angle rotation
            Quaternion l_SlopeAngleRotation;

            //* Check if the player is grounded and get the slope angle rotation
            GroundCheck(out m_IsGrounded, out l_SlopeAngleRotation);

            //* If the player is not grounded, return as we don't want to move the player
            if (!m_IsGrounded)
                return;

            //* If the player is grounded
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            //* Draw uncorrected velocity
            if (m_Debug)
                Debug.DrawRay(transform.position, transform.TransformDirection(l_TargetVelocity), Color.red, Mathf.Infinity);
#endif
            //* rotate the target velocity to match the slope angle
            l_TargetVelocity = l_SlopeAngleRotation * l_TargetVelocity;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            //* Draw corrected velocity 
            if (m_Debug)
                Debug.DrawRay(transform.position, transform.TransformDirection(l_TargetVelocity), Color.green, Mathf.Infinity);
#endif


            //* check the angle of the slope relative to the player movement
            float l_SlopeAngle = Vector3.Angle(l_TargetVelocity, transform.up);
            //* subtract 90 degrees from the slope angle to get the angle relative to the player
            l_SlopeAngle -= 90.0f;
            //* apply a movement multiplier to the target velocity based on the slope angle

            //* convert the l_SlopeAngle to a number between 0 and 1 relative to m_SlopeAngleFalloff as 0 and m_SlopeAngleMax as 1
            float l_SlopeEffector = Mathf.InverseLerp(m_SlopeAngleMax, m_SlopeAngleFalloff, Mathf.Abs(l_SlopeAngle));

            //* GOTCHA: if the slope angle is negative, we are going up the slope
            if (l_SlopeAngle > 0.0f)
                l_SlopeEffector = (1.0f - l_SlopeEffector) + 1.0f;

            //* multiply the target velocity by the slope effector
            l_TargetVelocity *= l_SlopeEffector;

            //* convert the target velocity to world space
            l_TargetVelocity = transform.TransformDirection(l_TargetVelocity);
            //* calculate the velocity change
            Vector3 l_VelocityChange = l_TargetVelocity - l_CurrentVelocity;
            //* apply the velocity change
            m_Rigidbody.AddForce(l_VelocityChange, ForceMode.VelocityChange);
        }

        private void RotatePlayer()
        {
            m_LookInput = Vector2.Lerp(m_PreviousLookInput, m_LookInput * Time.fixedDeltaTime, 0.35f);
            m_PreviousLookInput = m_LookInput;

            //* Rotate the player around the Y axis
            m_LookRotationY += m_LookInput.x * m_RotationSpeedMultiplier;
            transform.rotation = Quaternion.Euler(0.0f, m_LookRotationY, 0.0f);

            //* Rotate the camera around the X axis
            m_LookRotationX += m_LookInput.y * m_RotationSpeedMultiplier;
            m_LookRotationX = Mathf.Clamp(m_LookRotationX, -70.0f, 90.0f);
            m_CameraHolderTransform.localRotation = Quaternion.Euler(-m_LookRotationX, 0.0f, 0.0f);
        }

        private void DoJump()
        {
            m_CoyoteTimeRemaining = 0.0f;
            m_JumpHonorBufferTimeRemaining = 0.0f;
            m_CoyoteGroundTimeTotal = 0.0f;
            m_IsGrounded = false;

            float l_60cmJump = 300f;
            float l_JumpForce = l_60cmJump * m_JumpForceMultiplier;
            m_Rigidbody.AddForce(Vector3.up * l_JumpForce, ForceMode.Impulse);
        }

        private void HandelCrouch()
        {
            if (m_IsCrouching)
            {
                Debug.Log("Crouching");
                m_Collider.height = m_CrouchHeight;

                //* Move the collider down to match the new height
                Vector3 l_ColliderCenter = m_Collider.center;
                l_ColliderCenter.y = m_CrouchHeight * 0.5f;
                m_Collider.center = l_ColliderCenter;
                m_CameraHolderTransform.localPosition = new Vector3(0, m_Collider.height, 0);
            }

            else
            {
                //* Head check to make sure the player can stand up
                float l_HeadCheckPositionY = transform.position.y + m_Collider.height - m_Collider.radius;
                Vector3 l_HeadCheckPosition = new Vector3(transform.position.x, l_HeadCheckPositionY, transform.position.z);

                float l_HeadCheckDistance = m_PlayerHeight - m_Collider.height;

                //* Check if the player can stand up
                RaycastHit[] l_Hits = Physics.SphereCastAll(l_HeadCheckPosition, m_GroundCheckRadius, Vector3.up, l_HeadCheckDistance);

                //* If the player can stand up
                if (l_Hits.Length <= 1)
                {
                    m_Collider.height = m_PlayerHeight;
                    //* Move the collider down to match the new height
                    Vector3 l_ColliderCenter = m_Collider.center;
                    l_ColliderCenter.y = m_PlayerHeight * 0.5f;
                    m_Collider.center = l_ColliderCenter;
                    m_CameraHolderTransform.localPosition = new Vector3(0, m_Collider.height, -0.1f);
                    return;
                }

                //* if the player cant stand up then un-crouch as much as possible 

                float l_ClosestHitDistance = Mathf.Infinity;


                foreach (RaycastHit l_Hit in l_Hits)
                {
                    if (l_Hit.collider.gameObject == gameObject)
                    {
                        continue;
                    }

                    //* if the below is above the player
                    if (l_Hit.point.y <= (transform.position.y + m_Collider.height + 0.05f))
                    {
                        continue;
                    }

                    if (l_Hit.distance < l_ClosestHitDistance)
                    {
                        //* set the closest hit distance to the new closest hit
                        l_ClosestHitDistance = l_Hit.distance;
                    }
                }

                if (l_ClosestHitDistance == Mathf.Infinity)
                    return;

                //* set the collider height to the closest hit distance
                m_Collider.height = l_ClosestHitDistance + l_HeadCheckPositionY + (m_Collider.radius * 0.5f);

                //* Move the collider down to match the new height
                Vector3 l_ColliderCenter2 = m_Collider.center;
                l_ColliderCenter2.y = m_Collider.height * 0.5f;
                m_Collider.center = l_ColliderCenter2;
                m_CameraHolderTransform.localPosition = new Vector3(0, m_Collider.height, -0.1f);
            }
        }

        private void SwitchCamera()
        {
            switch (m_CameraMode)
            {
                case CameraMode.FirstPerson:
                    SetCameraProperties(m_1stPersonCamera, m_3rdPersonCamera);
                    m_CameraMode = CameraMode.ThirdPerson;
                    Camera.main.cullingMask |= 1 << LayerMask.NameToLayer("PlayerSelf");
                    break;
                case CameraMode.ThirdPerson:
                    SetCameraProperties(m_3rdPersonCamera, m_OrbitCamera);
                    m_CameraMode = CameraMode.Orbit;
                    break;
                case CameraMode.Orbit:
                    SetCameraProperties(m_OrbitCamera, m_1stPersonCamera);
                    m_CameraMode = CameraMode.FirstPerson;
                    Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("PlayerSelf"));
                    break;
            }
        }

        private void ZoomCamera(float a_Zoom)
        {
            switch (m_CameraMode)
            {
                case CameraMode.FirstPerson:
                    break;
                case CameraMode.ThirdPerson:
                    m_3rdPersonTransposer.m_CameraDistance += a_Zoom / m_CameraZoomMultiplier;
                    m_3rdPersonTransposer.m_CameraDistance = Mathf.Clamp(m_3rdPersonTransposer.m_CameraDistance, m_3rdPersonCameraMinDistance, m_3rdPersonCameraMaxDistance);
                    break;
                case CameraMode.Orbit:
                    m_OrbitTransposer.m_CameraDistance += a_Zoom / m_CameraZoomMultiplier;
                    m_OrbitTransposer.m_CameraDistance = Mathf.Clamp(m_OrbitTransposer.m_CameraDistance, m_OrbitCameraMinDistance, m_OrbitCameraMaxDistance);
                    break;
            }
        }
        private void SetCameraProperties(CinemachineVirtualCamera a_CameraOld, CinemachineVirtualCamera a_CameraNew)
        {
            a_CameraOld.Priority = int.MaxValue;
            a_CameraNew.Priority = int.MinValue;
            m_CurrentCamera = a_CameraNew;
        }

        private void GroundCheck(out bool a_Grounded, out Quaternion a_GroundSlope)
        {
            a_GroundSlope = Quaternion.identity;
            a_Grounded = false;

            //* Ground Check using sphere cast
            //* GOTCHA: If a collider is detected at the start of the sphere cast, then the hit info will have no normal direction, thats why we start the cast inside the player and move down
            RaycastHit[] l_Hits = Physics.SphereCastAll(transform.position + m_GroundCheckPositionOffset, m_GroundCheckRadius, Vector3.down, m_GroundCheckDistance);

            //* If there is no collider, then the player is not grounded
            if (l_Hits.Length < 1)
            {
                return;
            }

            //* If there is a collider, then check if it is the player
            foreach (RaycastHit a_Hit in l_Hits)
            {
                if (a_Hit.transform.gameObject != this.transform.gameObject)
                {
                    a_Grounded = true;

                    //* Get the normal of the ground
                    Vector3 l_GroundNormal = m_Rigidbody.transform.InverseTransformDirection(a_Hit.normal);
                    float l_GroundAngle = Vector3.Angle(l_GroundNormal, Vector3.up);
                    if (l_GroundAngle != 0)
                    {
                        Quaternion l_SlopeAngleRotation = Quaternion.FromToRotation(Vector3.up, l_GroundNormal);
                        a_GroundSlope = l_SlopeAngleRotation;
                    }
                    return;
                }
            }
            return;
        }

        private void JumpTimers()
        {
            if (m_IsGrounded)
            {
                m_CoyoteGroundTimeTotal += Time.fixedDeltaTime;
                if (m_CoyoteGroundTimeTotal > m_CoyoteGroundTime)
                {
                    m_CoyoteTimeRemaining = m_CoyoteTime;
                }
            }
            else if (m_CoyoteTimeRemaining > 0.0f)
            {
                m_CoyoteTimeRemaining -= Time.fixedDeltaTime;
            }

            if (m_JumpHonorBufferTimeRemaining > 0.0f && !m_IsGrounded)
            {
                m_JumpHonorBufferTimeRemaining -= Time.fixedDeltaTime;
            }
        }

        #endregion
    }
}