using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine
{
    /**
     * <summary>
     * Represents the current lifecycle state of an activity.
     * </summary>
     */
    public enum ActivityMode
    {
        /**
         * <summary>The activity is not running.</summary>
         */
        Inactive,
        
        /**
         * <summary>The activity is currently activating (async initialization in progress).</summary>
         */
        Activating,
        
        /**
         * <summary>The activity is fully active and running.</summary>
         */
        Active,
        
        /**
         * <summary>The activity is currently deactivating (async cleanup in progress).</summary>
         */
        Deactivating
    }
    
    /**
     * <summary>
     * Defines an asynchronous activity that can be attached to states.
     * Activities are used for long-running operations (animations, delays, etc.) that should
     * complete before or after state transitions.
     * </summary>
     */
    public interface IActivity
    {
        /**
         * <summary>Gets the current mode of the activity.</summary>
         */
        ActivityMode Mode { get; }
        
        /**
         * <summary>
         * Asynchronously activates the activity. Should transition from Inactive -> Activating -> Active.
         * </summary>
         * <param name="cancellationToken">Token to cancel the activation.</param>
         */
        UniTask ActivateAsync(CancellationToken cancellationToken);
        
        /**
         * <summary>
         * Asynchronously deactivates the activity. Should transition from Active -> Deactivating -> Inactive.
         * </summary>
         * <param name="cancellationToken">Token to cancel the deactivation.</param>
         */
        UniTask DeactivateAsync(CancellationToken cancellationToken);
    }

    /**
     * <summary>
     * Base class for activities with default async behavior.
     * Override ActivateAsync/DeactivateAsync to implement custom activation/deactivation logic.
     * </summary>
     */
    public abstract class Activity : IActivity
    {
        public ActivityMode Mode { get; protected set; } = ActivityMode.Inactive;

        public virtual async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            if (Mode != ActivityMode.Inactive) return;
            
            Mode = ActivityMode.Activating;
            await UniTask.CompletedTask;
            Mode = ActivityMode.Active;
        }

        public virtual async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (Mode != ActivityMode.Active) return;

            Mode = ActivityMode.Deactivating;
            await UniTask.CompletedTask;
            Mode = ActivityMode.Inactive;
        }
    }
}