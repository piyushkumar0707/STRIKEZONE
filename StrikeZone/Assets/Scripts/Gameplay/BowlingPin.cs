using System;
using UnityEngine;

namespace StrikeZone.Gameplay
{
    /// <summary>Tracks one pin's physics state and marks it down after a roll settles.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class BowlingPin : MonoBehaviour
    {
        private Rigidbody _body;
        private Vector3 _rackPosition;
        private Quaternion _rackRotation;
        private bool _enteredPit;

        public bool IsKnockedDown { get; private set; }
        public Rigidbody Body => _body;
        public event Action<float> OnImpact;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            RememberRackPose();
        }

        /// <summary>Captures the current transform as this pin's original rack pose.</summary>
        public void RememberRackPose()
        {
            _rackPosition = transform.position;
            _rackRotation = transform.rotation;
        }

        /// <summary>Marks this pin down when the settled pose meets a bowling fall condition.</summary>
        public bool EvaluateAndMarkKnockedDown(float deckSurfaceY, float fallAngleDegrees, float deckMargin)
        {
            if (IsKnockedDown)
            {
                return true;
            }

            var tiltDegrees = Vector3.Angle(transform.up, Vector3.up);
            var isTilted = tiltDegrees > fallAngleDegrees;
            var fellBelowDeck = transform.position.y < deckSurfaceY - deckMargin;

            if (isTilted || fellBelowDeck || _enteredPit)
            {
                IsKnockedDown = true;
            }

            return IsKnockedDown;
        }

        /// <summary>Returns whether this pin's rigidbody is below both settle thresholds.</summary>
        public bool IsSettled(float linearSpeedThreshold, float angularSpeedThreshold)
        {
            return _body == null || _body.isKinematic ||
                   (_body.linearVelocity.sqrMagnitude < linearSpeedThreshold * linearSpeedThreshold &&
                    _body.angularVelocity.sqrMagnitude < angularSpeedThreshold * angularSpeedThreshold);
        }

        /// <summary>Restores this pin at its rack point and clears its once-per-roll state.</summary>
        public void ResetToRackPose(Vector3 position, Quaternion rotation)
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            gameObject.SetActive(true);
            _body.isKinematic = true;
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            transform.SetPositionAndRotation(position, rotation);
            _enteredPit = false;
            IsKnockedDown = false;
            Physics.SyncTransforms();
            _body.isKinematic = false;
            _body.Sleep();
        }

        /// <summary>Restores this pin to the rack pose captured when the scene started.</summary>
        public void ResetToRackPose()
        {
            ResetToRackPose(_rackPosition, _rackRotation);
        }

        /// <summary>Records that this pin entered the pit trigger during a roll.</summary>
        public void MarkEnteredPit()
        {
            _enteredPit = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed > 0.8f)
            {
                OnImpact?.Invoke(impactSpeed);
            }
        }
    }
}
