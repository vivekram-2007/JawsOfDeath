using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        // true while right mouse is held (read by PlayerShooting for the crosshair)
        public static bool IsAiming;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face the running direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Camera Follow")]
        [Tooltip("Seconds of running in a side/back direction before the camera swings behind the character")]
        public float CameraFollowDelay = 2.0f;

        [Tooltip("How fast the camera swings behind the character, in degrees per second")]
        public float CameraFollowSpeed = 120.0f;

        [Header("Look Behind")]
        [Tooltip("How fast the camera swings to look behind (hold R), in degrees per second")]
        public float LookBehindSpeed = 700.0f;

        [Header("Aim (hold right mouse)")]
        [Tooltip("How far the camera target slides forward while aiming (camera distance is about 2, so 0.9 leaves about 1.1)")]
        public float AimZoomForward = 0.9f;

        [Tooltip("Extra sideways slide of the camera target while aiming (positive = more over the right shoulder)")]
        public float AimZoomRight = 0.0f;

        [Tooltip("How fast the zoom blends in and out (1 = one second)")]
        public float AimZoomSpeed = 6.0f;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // aim
        private float _aimBlend;
        private Vector3 _targetBaseLocalPos;

        // look behind
        private float _lookBehindOffset;

        // camera follow
        private float _runTimer;
        private bool _dirLocked;
        private Vector3 _lockedDir;
        private Vector2 _lastInput;
        private float _cameraFollowYaw;

        // player
        private float _speed;
        private float _animationBlend;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
				return false;
#endif
            }
        }


        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _targetBaseLocalPos = CinemachineCameraTarget.transform.localPosition;

            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            // hold right mouse to aim
            IsAiming = false;
#if ENABLE_INPUT_SYSTEM
            IsAiming = Mouse.current != null && Mouse.current.rightButton.isPressed;
#endif

            JumpAndGravity();
            GroundedCheck();
            Move();
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            bool mouseMoving = _input.look.sqrMagnitude >= _threshold;

            // if there is an input and camera position is not fixed
            if (mouseMoving && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }
            else if (_dirLocked && !LockCameraPosition)
            {
                // after the delay, swing the camera behind the character (mouse movement pauses this)
                _cinemachineTargetYaw = Mathf.MoveTowardsAngle(_cinemachineTargetYaw, _cameraFollowYaw,
                    CameraFollowSpeed * Time.deltaTime);
            }

            // hold R to look behind
            bool lookBehind = false;
#if ENABLE_INPUT_SYSTEM
            lookBehind = Keyboard.current != null && Keyboard.current.rKey.isPressed;
#endif
            _lookBehindOffset = Mathf.MoveTowards(_lookBehindOffset, lookBehind ? 180f : 0f,
                LookBehindSpeed * Time.deltaTime);

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw + _lookBehindOffset, 0.0f);

            // aim zoom: slide the target forward so the camera sits closer to the character
            _aimBlend = Mathf.MoveTowards(_aimBlend, IsAiming ? 1f : 0f, AimZoomSpeed * Time.deltaTime);
            float t = Mathf.SmoothStep(0f, 1f, _aimBlend);

            Transform targetTf = CinemachineCameraTarget.transform;
            Vector3 basePos = targetTf.parent != null
                ? targetTf.parent.TransformPoint(_targetBaseLocalPos)
                : _targetBaseLocalPos;
            Quaternion yawRot = Quaternion.Euler(0f, _cinemachineTargetYaw + _lookBehindOffset, 0f);
            Vector3 zoomOffset = yawRot * new Vector3(AimZoomRight, 0f, AimZoomForward);
            targetTf.position = basePos + zoomOffset * t;
        }

        private void Move()
        {
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // camera yaw: movement is relative to this (look-behind offset removed so controls don't flip)
            float cameraYaw = _mainCamera.transform.eulerAngles.y - _lookBehindOffset;
            Quaternion camRot = Quaternion.Euler(0.0f, cameraYaw, 0.0f);

            Vector3 targetDirection = Vector3.zero;

            if (_input.move == Vector2.zero)
            {
                _runTimer = 0f;
                _dirLocked = false;
            }
            else
            {
                // key changed: start over
                if ((_input.move - _lastInput).sqrMagnitude > 0.01f)
                {
                    _runTimer = 0f;
                    _dirLocked = false;
                }

                Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
                Vector3 camRelative = camRot * inputDirection;

                if (IsAiming)
                {
                    // aiming: move relative to the camera, no auto camera swing
                    _runTimer = 0f;
                    _dirLocked = false;
                    targetDirection = camRelative;
                }
                else
                {
                    if (_dirLocked)
                    {
                        // keep running the same world direction while the camera swings round
                        targetDirection = _lockedDir;
                    }
                    else
                    {
                        targetDirection = camRelative;

                        // running away from the camera direction (A, S, D)? start the timer
                        float angle = Vector3.Angle(camRot * Vector3.forward, camRelative);
                        if (angle > 45f)
                        {
                            _runTimer += Time.deltaTime;
                            if (_runTimer >= CameraFollowDelay)
                            {
                                _dirLocked = true;
                                _lockedDir = camRelative;
                                _cameraFollowYaw = Mathf.Atan2(camRelative.x, camRelative.z) * Mathf.Rad2Deg;
                            }
                        }
                        else
                        {
                            _runTimer = 0f;
                        }
                    }

                    // turn the character to face the direction it is running
                    float targetYaw = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetYaw, ref _rotationVelocity,
                        RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }

            // aiming: always face where the camera points, even when standing still
            if (IsAiming)
            {
                float aimRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraYaw, ref _rotationVelocity,
                    RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, aimRotation, 0.0f);
            }

            _lastInput = _input.move;

            // move the player
            _controller.Move(targetDirection * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }
}