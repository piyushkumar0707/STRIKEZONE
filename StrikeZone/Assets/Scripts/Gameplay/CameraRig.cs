using StrikeZone.Core;
using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Smoothly frames the ball, lane, and pin deck as the game changes state.</summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Transform _ball;
        [SerializeField] private BowlingGameManager _gameManager;
        [SerializeField, Min(0.01f)] private float _smoothTime = 0.35f;
        [SerializeField, Min(0.1f)] private float _lookAhead = 4f;
        [SerializeField, Min(0f)] private float _stopBeforePins = 2f;

        private Vector3 _startPosition;
        private Quaternion _startRotation;
        private Vector3 _positionVelocity;

        private void Awake()
        {
            if (_ball == null)
            {
                var ball = FindAnyObjectByType<BowlingBall>();
                if (ball != null) _ball = ball.transform;
            }

            if (_gameManager == null) _gameManager = FindAnyObjectByType<BowlingGameManager>();
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }

        private void LateUpdate()
        {
            if (_ball == null || _gameManager == null) return;

            var laneEndBeforePins = 9.85f - _stopBeforePins;
            Vector3 targetPosition;
            Vector3 lookAt;
            switch (_gameManager.State)
            {
                case BowlingGameState.BallRolling:
                case BowlingGameState.PinsSettling:
                    if (_gameManager.State == BowlingGameState.PinsSettling)
                    {
                        targetPosition = new Vector3(0f, 3.1f, 6.2f);
                        lookAt = new Vector3(0f, 0.45f, 9.8f);
                    }
                    else
                    {
                        var followZ = Mathf.Min(_ball.position.z - 3.4f, laneEndBeforePins - 2f);
                        targetPosition = new Vector3(_ball.position.x * 0.45f, _ball.position.y + 2.1f, followZ);
                        lookAt = new Vector3(_ball.position.x * 0.25f, _ball.position.y, Mathf.Min(_ball.position.z + _lookAhead, laneEndBeforePins));
                    }
                    break;
                default:
                    targetPosition = _startPosition;
                    lookAt = _ball.position + Vector3.forward * 5.5f;
                    break;
            }

            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _positionVelocity, _smoothTime);
            var targetRotation = Quaternion.LookRotation(lookAt - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-5f * Time.deltaTime));
        }

        public void ResetCamera()
        {
            _positionVelocity = Vector3.zero;
            transform.SetPositionAndRotation(_startPosition, _startRotation);
        }
    }
}
