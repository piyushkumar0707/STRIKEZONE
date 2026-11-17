using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Clamps left/right aim and updates the in-world aiming line.</summary>
    public sealed class AimController : MonoBehaviour
    {
        [SerializeField, Range(1f, 45f)] private float _maximumAngleDegrees = 12f;
        [SerializeField, Min(1f)] private float _degreesPerSecond = 16f;
        [SerializeField, Min(0.5f)] private float _aimLineLength = 2.5f;
        [SerializeField] private LineRenderer _aimLine;

        private float _aimAngleDegrees;

        public float AimAngleDegrees => _aimAngleDegrees;
        public float Centering => 1f - Mathf.Clamp01(Mathf.Abs(_aimAngleDegrees) / _maximumAngleDegrees);
        public Vector3 Direction => (Quaternion.AngleAxis(_aimAngleDegrees, Vector3.up) * Vector3.forward).normalized;

        private void Awake()
        {
            if (_aimLine == null)
            {
                _aimLine = GetComponent<LineRenderer>();
            }

            if (_aimLine != null)
            {
                _aimLine.positionCount = 2;
                _aimLine.useWorldSpace = true;
            }
        }

        /// <summary>Applies a normalized horizontal input axis for the supplied frame duration.</summary>
        public void ApplyAimInput(float axis, float deltaTime)
        {
            _aimAngleDegrees = Mathf.Clamp(
                _aimAngleDegrees + Mathf.Clamp(axis, -1f, 1f) * _degreesPerSecond * deltaTime,
                -_maximumAngleDegrees,
                _maximumAngleDegrees);
        }

        private void LateUpdate()
        {
            if (_aimLine == null)
            {
                return;
            }

            var start = transform.position;
            start.y = Mathf.Max(start.y, 0.02f);
            _aimLine.SetPosition(0, start);
            _aimLine.SetPosition(1, start + Direction * _aimLineLength);
        }
    }
}
