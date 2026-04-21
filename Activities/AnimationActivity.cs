using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace StateMachine.Activities
{
    /**
     * <summary>
     * Activity that plays and stops animations on entry and exit of a state.
     * Bridges the animation system with state machine lifecycle.
     * </summary>
     */
    public class AnimationActivity : Activity
    {
        /**
         * <summary>The animator component that will play the animation.</summary>
         */
        private readonly Animator _animator;

        /**
         * <summary>The name of the animation state to play. Must match the animator's state name.</summary>
         */
        private readonly string _animationStateName;

        /**
         * <summary>
         * Creates a new animation activity.
         * </summary>
         * <param name="animator">The Animator component to control. Must not be null.</param>
         * <param name="animationStateName">The name of the animation state to play. Must not be null or empty.</param>
         * <exception cref="System.ArgumentNullException">Thrown if animator or animationStateName is null.</exception>
         * <exception cref="System.ArgumentException">Thrown if animationStateName is empty.</exception>
         */
        public AnimationActivity(Animator animator, string animationStateName)
        {
            if (animator == null)
                throw new System.ArgumentNullException(nameof(animator), "Animator cannot be null.");
            if (animationStateName == null)
                throw new System.ArgumentNullException(nameof(animationStateName), "Animation state name cannot be null.");
            if (string.IsNullOrWhiteSpace(animationStateName))
                throw new System.ArgumentException("Animation state name cannot be empty.", nameof(animationStateName));

            _animator = animator;
            _animationStateName = animationStateName;
        }

        /**
         * <summary>
         * Activates the animation by transitioning the animator to the configured animation state.
         * </summary>
         * <param name="cancellationToken">Token to cancel the activation.</param>
         */
        public override async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            // Transition to the animation state
            _animator.SetTrigger(_animationStateName);

            // Call base to set mode to Active
            await base.ActivateAsync(cancellationToken);
        }

        /**
         * <summary>
         * Deactivates the animation by stopping playback.
         * </summary>
         * <param name="cancellationToken">Token to cancel the deactivation.</param>
         */
        public override async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            // Optional: Stop the animation by transitioning to an idle state or setting a parameter
            // This example just clears the trigger, but you could set a "Idle" animation instead
            _animator.ResetTrigger(_animationStateName);

            // Call base to set mode to Inactive
            await base.DeactivateAsync(cancellationToken);
        }
    }
}


