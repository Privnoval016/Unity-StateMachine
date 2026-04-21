using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine.Activities
{
    /**
     * <summary>
     * Activity that waits for a specific duration before completing activation or deactivation.
     * Useful for adding delays between state transitions or for staged animations.
     * </summary>
     */
    public class WaitActivity : Activity
    {
        /**
         * <summary>The duration to wait on activation in seconds.</summary>
         */
        private readonly float _activateDelaySeconds;

        /**
         * <summary>The duration to wait on deactivation in seconds.</summary>
         */
        private readonly float _deactivateDelaySeconds;

        /**
         * <summary>
         * Creates a new wait activity with configurable delays.
         * </summary>
         * <param name="activateDelaySeconds">Time to wait when entering the state. Must be >= 0.</param>
         * <param name="deactivateDelaySeconds">Time to wait when exiting the state. Must be >= 0.</param>
         * <exception cref="System.ArgumentException">Thrown if delay values are negative.</exception>
         */
        public WaitActivity(float activateDelaySeconds = 0f, float deactivateDelaySeconds = 0f)
        {
            if (activateDelaySeconds < 0f)
                throw new ArgumentException("Activate delay cannot be negative.", nameof(activateDelaySeconds));
            if (deactivateDelaySeconds < 0f)
                throw new ArgumentException("Deactivate delay cannot be negative.", nameof(deactivateDelaySeconds));

            _activateDelaySeconds = activateDelaySeconds;
            _deactivateDelaySeconds = deactivateDelaySeconds;
        }

        /**
         * <summary>
         * Waits for the configured activation delay, then marks the activity as Active.
         * </summary>
         * <param name="cancellationToken">Token to cancel the wait operation.</param>
         */
        public override async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            if (Mode != ActivityMode.Inactive) return;

            Mode = ActivityMode.Activating;

            // Wait for the activation delay
            if (_activateDelaySeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_activateDelaySeconds), cancellationToken: cancellationToken);
            }

            Mode = ActivityMode.Active;
        }

        /**
         * <summary>
         * Waits for the configured deactivation delay, then marks the activity as Inactive.
         * </summary>
         * <param name="cancellationToken">Token to cancel the wait operation.</param>
         */
        public override async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (Mode == ActivityMode.Inactive) return;

            Mode = ActivityMode.Deactivating;

            // Wait for the deactivation delay
            if (_deactivateDelaySeconds > 0f)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_deactivateDelaySeconds), cancellationToken: cancellationToken);
            }

            Mode = ActivityMode.Inactive;
        }
    }
}





