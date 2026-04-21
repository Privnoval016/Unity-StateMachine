using System.Collections.Generic;
using UnityEngine;

namespace StateMachine
{
    /**
     * <summary>
     * Builder for cleanly constructing a hierarchical state machine.
     * </summary>
     * 
     * <remarks>
     * State hierarchy relationships are defined through the State constructor (parent parameter),
     * not through the builder. The WithState method validates these relationships and serves
     * as documentation of the expected hierarchy structure.
     * </remarks>
     * 
     * <example>
     * Single-line usage:
     * <code>
     * var sm = new StateMachineBuilder&lt;PlayerController&gt;(root)
     *     .WithState(idle, root)
     *     .WithActivity(idle, new AnimationActivity("idle"))
     *     .Build(playerController);
     * </code>
     * 
     * Multi-line usage for readability:
     * <code>
     * var builder = new StateMachineBuilder&lt;PlayerController&gt;(root)
     *     .WithState(idle, root)
     *     .WithState(run, root)
     *     .WithState(jump, run);
     * 
     * builder
     *     .WithActivity(idle, new AnimationActivity("idle"))
     *     .WithActivity(run, new AnimationActivity("run"))
     *     .WithActivity(jump, new AnimationActivity("jump"));
     * 
     * var stateMachine = builder.Build(playerController);
     * </code>
     * </example>
     * 
     * <typeparam name="T">The MonoBehaviour type that hosts the state machine.</typeparam>
     */
    public class StateMachineBuilder<T> where T : MonoBehaviour
    {
        private readonly State<T> _root;
        private readonly List<(State<T> state, IActivity activity)> _activityBindings = new();
        
        /**
         * <summary>
         * Creates a new builder with the specified root state.
         * </summary>
         * <param name="root">The root state of the hierarchy. Must not be null.</param>
         * <exception cref="System.ArgumentNullException">Thrown if root is null.</exception>
         */
        public StateMachineBuilder(State<T> root)
        {
            if (root == null)
                throw new System.ArgumentNullException(nameof(root), "Root state cannot be null.");
            
            _root = root;
        }

        /**
         * <summary>
         * Validates that a child state is properly configured in the hierarchy.
         * Note: State hierarchy relationships are defined through the State constructor (parent parameter).
         * This method serves as documentation and validation that the child belongs under the parent.
         * </summary>
         * <param name="childState">The state to validate. Must not be null.</param>
         * <param name="parentState">The expected parent state. Must not be null.</param>
         * <returns>This builder instance for method chaining.</returns>
         * <exception cref="System.ArgumentNullException">Thrown if childState or parentState is null.</exception>
         */
        public StateMachineBuilder<T> WithState(State<T> childState, State<T> parentState)
        {
            if (childState == null)
                throw new System.ArgumentNullException(nameof(childState), "Child state cannot be null.");
            if (parentState == null)
                throw new System.ArgumentNullException(nameof(parentState), "Parent state cannot be null.");
            
            // Validate that the child state's parent matches the specified parent
            if (childState.Parent != parentState)
                throw new System.InvalidOperationException(
                    $"Child state's parent does not match the specified parent. " +
                    $"Ensure the state was constructed with the correct parent: " +
                    $"new {childState.GetType().Name}(stateMachine, parentState)");
            
            return this;
        }

        /**
         * <summary>
         * Attaches an activity to a state.
         * Activities are executed during state transitions (activated on enter, deactivated on exit).
         * Multiple activities can be attached to the same state and will execute according to the sequencing mode.
         * </summary>
         * <param name="state">The state to attach the activity to. Must not be null.</param>
         * <param name="activity">The activity to attach. Must not be null.</param>
         * <returns>This builder instance for method chaining.</returns>
         * <exception cref="System.ArgumentNullException">Thrown if state or activity is null.</exception>
         */
        public StateMachineBuilder<T> WithActivity(State<T> state, IActivity activity)
        {
            if (state == null)
                throw new System.ArgumentNullException(nameof(state), "State cannot be null.");
            if (activity == null)
                throw new System.ArgumentNullException(nameof(activity), "Activity cannot be null.");
            
            _activityBindings.Add((state, activity));
            return this;
        }

        /**
         * <summary>
         * Builds and returns the fully constructed state machine.
         * This is the final step that creates the StateMachine instance with all configured activities.
         * The root state and host are verified, then the state machine is instantiated.
         * All registered activities are attached to their respective states.
         * </summary>
         * <param name="host">The MonoBehaviour instance that will host the state machine. Must not be null.</param>
         * <returns>A fully constructed StateMachine instance ready to use. Call Start() to begin state machine execution.</returns>
         * <exception cref="System.ArgumentNullException">Thrown if host is null.</exception>
         * <exception cref="System.InvalidCastException">Thrown if host is not an instance of type T.</exception>
         */
        public StateMachine<T> Build(MonoBehaviour host)
        {
            if (host == null)
                throw new System.ArgumentNullException(nameof(host), "Host cannot be null.");
            
            if (host is not T tHost)
                throw new System.InvalidCastException(
                    $"Host must be of type {typeof(T).Name}, but was {host.GetType().Name}.");
            
            // Create the state machine with the root and host
            var machine = new StateMachine<T>(tHost, _root);
            
            // Attach all activities to their respective states
            foreach (var (state, activity) in _activityBindings)
            {
                state.AddActivity(activity);
            }
            
            return machine;
        }
    }

}