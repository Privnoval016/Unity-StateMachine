using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine.Activities
{
    /**
     * <summary>
     * Activity that invokes a callback method on activation and deactivation.
     * Useful for triggering any custom logic during state transitions.
     * </summary>
     */
    public class CallbackActivity : Activity
    {
        /**
         * <summary>Delegate type for callbacks. Returns a UniTask for async support.</summary>
         */
        public delegate UniTask CallbackDelegate(CancellationToken cancellationToken);

        /**
         * <summary>The callback invoked when the activity activates.</summary>
         */
        private readonly CallbackDelegate _onActivate;

        /**
         * <summary>The callback invoked when the activity deactivates.</summary>
         */
        private readonly CallbackDelegate _onDeactivate;

        /**
         * <summary>
         * Creates a new callback activity with activate and deactivate callbacks.
         * </summary>
         * <param name="onActivate">Callback to invoke on activation. Can be null if no activation callback is needed.</param>
         * <param name="onDeactivate">Callback to invoke on deactivation. Can be null if no deactivation callback is needed.</param>
         */
        public CallbackActivity(CallbackDelegate onActivate = null, CallbackDelegate onDeactivate = null)
        {
            _onActivate = onActivate;
            _onDeactivate = onDeactivate;
        }

        /**
         * <summary>
         * Activates by invoking the activation callback, then transitions to Active mode.
         * </summary>
         * <param name="cancellationToken">Token to cancel the activation.</param>
         */
        public override async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            if (Mode != ActivityMode.Inactive) return;

            Mode = ActivityMode.Activating;

            // Invoke the callback if provided
            if (_onActivate != null)
            {
                await _onActivate(cancellationToken);
            }

            Mode = ActivityMode.Active;
        }

        /**
         * <summary>
         * Deactivates by invoking the deactivation callback, then transitions to Inactive mode.
         * </summary>
         * <param name="cancellationToken">Token to cancel the deactivation.</param>
         */
        public override async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (Mode == ActivityMode.Inactive) return;

            Mode = ActivityMode.Deactivating;

            // Invoke the callback if provided
            if (_onDeactivate != null)
            {
                await _onDeactivate(cancellationToken);
            }

            Mode = ActivityMode.Inactive;
        }
    }
}


