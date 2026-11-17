using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StrikeZone.Gameplay
{
    /// <summary>Reads bowling actions and coordinates aim, power locking, and ball release.</summary>
    public sealed class BowlingController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActionsAsset;
        [SerializeField] private AimController _aimController;
        [SerializeField] private PowerMeter _powerMeter;
        [SerializeField] private BowlingBall _ball;
        [SerializeField] private PinManager _pinManager;
        [SerializeField, Min(0f)] private float _releaseDelay = 0.2f;

        private InputActionMap _actionMap;
        private InputAction _aimAction;
        private InputAction _confirmAction;
        private InputAction _pauseAction;
        private bool _inputEnabled = true;
        private bool _releasePending;

        public event Action OnBallReleased;
        public event Action OnPauseRequested;
        public event Action OnPowerMeterStarted;
        public event Action<float> OnPowerLocked;
        public bool IsInputEnabled => _inputEnabled;

        private void Awake()
        {
            if (_inputActionsAsset == null)
            {
                Debug.LogError("BowlingController needs the StrikeZoneControls Input Actions asset.", this);
                return;
            }

            _actionMap = _inputActionsAsset.FindActionMap("Bowling", true);
            _aimAction = _actionMap.FindAction("Aim", true);
            _confirmAction = _actionMap.FindAction("Confirm", true);
            _pauseAction = _actionMap.FindAction("Pause", true);

            if (_aimController == null || _powerMeter == null || _ball == null)
            {
                Debug.LogError("BowlingController is missing an aim controller, power meter, or ball reference.", this);
            }
        }

        private void OnEnable()
        {
            _actionMap?.Enable();
        }

        private void OnDisable()
        {
            _actionMap?.Disable();
        }

        private void Update()
        {
            if (_pauseAction != null && _pauseAction.WasPressedThisFrame())
            {
                OnPauseRequested?.Invoke();
            }

            if (!_inputEnabled || _releasePending || _aimAction == null || _confirmAction == null)
            {
                return;
            }

            if (_aimController != null)
            {
                _aimController.ApplyAimInput(_aimAction.ReadValue<float>(), Time.deltaTime);
            }

            if (!_confirmAction.WasPressedThisFrame())
            {
                return;
            }

            if (_powerMeter == null)
            {
                return;
            }

            if (!_powerMeter.IsRunning)
            {
                _powerMeter.StartMeter();
                OnPowerMeterStarted?.Invoke();
                return;
            }

            var lockedPower = _powerMeter.LockPower();
            OnPowerLocked?.Invoke(lockedPower);
            _inputEnabled = false;
            _releasePending = true;
            StartCoroutine(ReleaseAfterDelay(lockedPower));
        }

        /// <summary>Enables or disables aiming and throw controls; pause remains available.</summary>
        public void SetInputEnabled(bool isEnabled)
        {
            _inputEnabled = isEnabled;
        }

        /// <summary>Resets power and returns the ball for the next roll.</summary>
        public void ResetForNextRoll(Transform ballSpawnPoint)
        {
            if (_ball == null)
            {
                return;
            }

            _ball.ResetTo(ballSpawnPoint);
            _powerMeter?.ResetMeter();
            _releasePending = false;
            _inputEnabled = true;
        }

        private IEnumerator ReleaseAfterDelay(float power)
        {
            yield return new WaitForSecondsRealtime(_releaseDelay);

            if (_pinManager != null)
            {
                _pinManager.BeginRoll();
            }

            if (_ball != null && _aimController != null)
            {
                _ball.Launch(_aimController.Direction, power);
                OnBallReleased?.Invoke();
            }

            _releasePending = false;
        }
    }
}
