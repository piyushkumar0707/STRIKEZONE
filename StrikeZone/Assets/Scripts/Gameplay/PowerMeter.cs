using System;
using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Runs a 0-to-1 oscillating power meter and publishes its locked value.</summary>
    public sealed class PowerMeter : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _cyclesPerSecond = 1.2f;

        private float _elapsedTime;

        public event Action<float> OnPowerLocked;

        public bool IsRunning { get; private set; }
        public float Value { get; private set; }

        private void Update()
        {
            if (!IsRunning)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            // PingPong covers one full 0→1→0 cycle in two units of its input.
            Value = Mathf.PingPong(_elapsedTime * _cyclesPerSecond * 2f, 1f);
        }

        public void StartMeter()
        {
            if (IsRunning)
            {
                return;
            }

            _elapsedTime = 0f;
            Value = 0f;
            IsRunning = true;
        }

        /// <summary>Stops the meter, returns the locked value, and raises the lock event.</summary>
        public float LockPower()
        {
            if (!IsRunning)
            {
                return Value;
            }

            IsRunning = false;
            OnPowerLocked?.Invoke(Value);
            return Value;
        }

        public void ResetMeter()
        {
            IsRunning = false;
            _elapsedTime = 0f;
            Value = 0f;
        }
    }
}
