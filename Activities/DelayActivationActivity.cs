using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine
{
    /**
     * <summary>
     * An activity that delays before fully activating.
     * Useful for implementing startup animations, asset loading delays, or synchronization waits.
     * </summary>
     */
    public class DelayActivationActivity : Activity
    {
        /**
         * <summary>The number of seconds to delay activation.</summary>
         */
        public readonly float DelaySeconds;
        
        /**
         * <summary>
         * Creates a new delay activity with the specified duration.
         * </summary>
         * <param name="delaySeconds">The delay duration in seconds.</param>
         */
        public DelayActivationActivity(float delaySeconds)
        {
            DelaySeconds = delaySeconds;
        }

        /**
         * <summary>
         * Overrides activation to include a delay before marking as Active.
         * Waits for DelaySeconds, then calls base activation to set Mode to Active.
         * </summary>
         * <param name="cancellationToken">Token to cancel the delay operation.</param>
         */
        public override async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            // Delay for the specified duration
            await UniTask.Delay(System.TimeSpan.FromSeconds(DelaySeconds), cancellationToken: cancellationToken);
            
            // Then run the default activation (Mode: Inactive -> Activating -> Active)
            await base.ActivateAsync(cancellationToken);
        }
    }
}