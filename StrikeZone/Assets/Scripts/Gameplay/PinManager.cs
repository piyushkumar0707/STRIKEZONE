using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Racks pins, waits for a roll to settle, counts fallen pins once, and sweeps them.</summary>
    public sealed class PinManager : MonoBehaviour
    {
        [Header("Knocked-down detection")]
        [SerializeField] private float _deckSurfaceY = 0f;
        [SerializeField, Range(1f, 89f)] private float _fallAngleDegrees = 30f;
        [SerializeField, Min(0f)] private float _belowDeckMargin = 0.05f;

        [Header("Settle detection")]
        [SerializeField, Min(0f)] private float _settledLinearSpeed = 0.05f;
        [SerializeField, Min(0f)] private float _settledAngularSpeed = 0.05f;
        [SerializeField, Min(0.1f)] private float _settleDuration = 1f;
        [SerializeField, Min(0.1f)] private float _ballStoppedDuration = 1f;
        [SerializeField, Min(1f)] private float _ballRollTimeout = 10f;
        [SerializeField, Min(1f)] private float _pinSettleTimeout = 8f;

        private BowlingPin[] _pins = Array.Empty<BowlingPin>();
        private Transform[] _spawnPoints = Array.Empty<Transform>();
        private Rigidbody _ballBody;
        private int _standingBeforeRoll;
        private bool _rollActive;
        private bool _ballEnteredPit;

        public event Action<int> OnRollResolved;

        public int StandingPinCount => _pins.Count(pin => pin != null && pin.gameObject.activeInHierarchy && !pin.IsKnockedDown);

        private void Awake()
        {
            _pins = GetComponentsInChildren<BowlingPin>(true)
                .OrderBy(pin => pin.name, StringComparer.Ordinal)
                .ToArray();

            var spawnRoot = transform.Find("BowlingSetup/PinSpawnPoints");
            if (spawnRoot != null)
            {
                _spawnPoints = spawnRoot.GetComponentsInChildren<Transform>(true)
                    .Where(point => point != spawnRoot && point.name.StartsWith("PinSpawn_", StringComparison.Ordinal))
                    .OrderBy(point => point.name, StringComparer.Ordinal)
                    .ToArray();
            }

            var ballTransform = transform.Find("Ball");
            if (ballTransform != null)
            {
                _ballBody = ballTransform.GetComponent<Rigidbody>();
            }

            if (_pins.Length == 0)
            {
                Debug.LogError("PinManager found no BowlingPin components under its GameObject.", this);
            }

            if (_spawnPoints.Length != 0 && _spawnPoints.Length != _pins.Length)
            {
                Debug.LogWarning("PinManager found a different number of pin spawn points and pins; cached pin poses will be used.", this);
                _spawnPoints = Array.Empty<Transform>();
            }
        }

        /// <summary>Begins a roll and records the current standing count as its baseline.</summary>
        public void BeginRoll()
        {
            if (_rollActive)
            {
                throw new InvalidOperationException("A pin roll is already in progress.");
            }

            _standingBeforeRoll = StandingPinCount;
            _ballEnteredPit = false;
            _rollActive = true;
        }

        /// <summary>
        /// Waits for the ball to reach the pit, stop, or time out, then waits for pins
        /// to settle. Resolves and emits the roll count exactly once.
        /// </summary>
        public IEnumerator WaitForRollToSettle(Action<int> onResolved = null)
        {
            if (!_rollActive)
            {
                throw new InvalidOperationException("Call BeginRoll before waiting for a roll to settle.");
            }

            if (_ballBody == null)
            {
                Debug.LogError("PinManager could not find a Rigidbody on the Ball object.", this);
                yield break;
            }

            var rollElapsed = 0f;
            var ballStillDuration = 0f;
            while (!_ballEnteredPit && ballStillDuration < _ballStoppedDuration && rollElapsed < _ballRollTimeout)
            {
                var deltaTime = Time.deltaTime;
                rollElapsed += deltaTime;
                if (_ballBody.linearVelocity.magnitude < _settledLinearSpeed)
                {
                    ballStillDuration += deltaTime;
                }
                else
                {
                    ballStillDuration = 0f;
                }

                yield return null;
            }

            var pinsSettledFor = 0f;
            var pinWaitElapsed = 0f;
            while (pinsSettledFor < _settleDuration && pinWaitElapsed < _pinSettleTimeout)
            {
                var deltaTime = Time.deltaTime;
                pinWaitElapsed += deltaTime;
                if (AreActivePinsSettled())
                {
                    pinsSettledFor += deltaTime;
                }
                else
                {
                    pinsSettledFor = 0f;
                }

                yield return null;
            }

            var knockedDown = ResolveRoll();
            onResolved?.Invoke(knockedDown);
        }

        /// <summary>Marks a ball as out of play when it enters the pit trigger.</summary>
        public void NotifyBallEnteredPit(Rigidbody enteringBody)
        {
            if (enteringBody != null && enteringBody == _ballBody)
            {
                _ballEnteredPit = true;
            }
        }

        /// <summary>Sweeps only pins marked down by the most recently resolved roll.</summary>
        public void SweepKnockedPins()
        {
            if (_rollActive)
            {
                throw new InvalidOperationException("Resolve the current roll before sweeping pins.");
            }

            foreach (var pin in _pins)
            {
                if (pin != null && pin.IsKnockedDown)
                {
                    pin.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>Re-racks every pin at its configured spawn point or original pose.</summary>
        public void RackAll()
        {
            if (_rollActive)
            {
                throw new InvalidOperationException("Resolve the current roll before racking pins.");
            }

            for (var i = 0; i < _pins.Length; i++)
            {
                var pin = _pins[i];
                if (pin == null)
                {
                    continue;
                }

                if (_spawnPoints.Length == _pins.Length)
                {
                    pin.ResetToRackPose(_spawnPoints[i].position, _spawnPoints[i].rotation);
                }
                else
                {
                    pin.ResetToRackPose();
                }
            }
        }

        private int ResolveRoll()
        {
            if (!_rollActive)
            {
                throw new InvalidOperationException("There is no active roll to resolve.");
            }

            foreach (var pin in _pins)
            {
                if (pin != null && pin.gameObject.activeInHierarchy)
                {
                    pin.EvaluateAndMarkKnockedDown(_deckSurfaceY, _fallAngleDegrees, _belowDeckMargin);
                }
            }

            var knockedDown = Mathf.Max(0, _standingBeforeRoll - StandingPinCount);
            _rollActive = false;
            OnRollResolved?.Invoke(knockedDown);
            return knockedDown;
        }

        private bool AreActivePinsSettled()
        {
            foreach (var pin in _pins)
            {
                if (pin != null && pin.gameObject.activeInHierarchy && !pin.IsKnockedDown &&
                    !pin.IsSettled(_settledLinearSpeed, _settledAngularSpeed))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
