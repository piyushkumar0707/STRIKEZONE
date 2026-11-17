using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Reports the bowling ball entering the pit so roll settling can begin.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BowlingPitTrigger : MonoBehaviour
    {
        [SerializeField] private PinManager _pinManager;

        private void Awake()
        {
            if (_pinManager == null)
            {
                _pinManager = FindAnyObjectByType<PinManager>();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var pin = other.GetComponent<BowlingPin>();
            if (pin != null)
            {
                pin.MarkEnteredPit();
                return;
            }

            if (other.attachedRigidbody != null)
            {
                _pinManager?.NotifyBallEnteredPit(other.attachedRigidbody);
            }
        }
    }
}
