using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Applies bowling throws to the ball's Rigidbody and returns it to the foul line.</summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public sealed class BowlingBall : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _minimumLaunchSpeed = 5f;
        [SerializeField, Min(0.1f)] private float _maximumLaunchSpeed = 12f;

        private Rigidbody _body;

        public Rigidbody Body => _body;
        public float Speed => _body != null ? _body.linearVelocity.magnitude : 0f;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        /// <summary>Launches the ball with power mapped from the minimum to maximum speed.</summary>
        public void Launch(Vector3 direction, float power)
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                Debug.LogError("BowlingBall cannot launch with a zero direction.", this);
                return;
            }

            var speed = Mathf.Lerp(_minimumLaunchSpeed, _maximumLaunchSpeed, Mathf.Clamp01(power));
            _body.isKinematic = false;
            _body.linearVelocity = direction.normalized * speed;
            _body.WakeUp();
        }

        /// <summary>Stops the ball and moves it back to the supplied spawn transform.</summary>
        public void ResetTo(Transform spawnPoint)
        {
            if (spawnPoint == null)
            {
                Debug.LogError("BowlingBall.ResetTo requires a spawn point.", this);
                return;
            }

            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            _body.isKinematic = true;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            Physics.SyncTransforms();
            _body.isKinematic = false;
            _body.Sleep();
        }
    }
}
