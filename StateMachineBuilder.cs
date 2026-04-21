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
     *     .Build(playerController);
     * </code>
     *
     * Multi-line usage:
     * <code>
     * var builder = new StateMachineBuilder&lt;PlayerController&gt;(root)
     *     .WithState(idle, root)
     *     .WithState(run, root)
     *     .WithState(jump, run);
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
        
        private readonly List<State<T>> _states = new();
        
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
            
            _states.Add(root);
        }

        /**
         * <summary>
         * Registers a state to be hooked up to the state machine.
         * </summary>
         * <param name="state">The state to register. Must not be null.</param>
         * <returns>This builder instance for method chaining.</returns>
         * <exception cref="System.ArgumentNullException">Thrown if state is null.</exception>
         */
        public StateMachineBuilder<T> WithState(State<T> state)
        {
            if (state == null)
                throw new System.ArgumentNullException(nameof(state), "State cannot be null.");
            
            _states.Add(state);
            
            return this;
        }

        /**
         * <summary>
         * Builds and returns the fully constructed state machine.
         * This is the final step that creates the StateMachine instance with all configured activities.
         * The root state and host are verified, then the state machine is instantiated.
         * All registered states are provided the state machine.
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

            foreach (var state in _states)
            {
                state.WithMachine(machine);
            }

            return machine;
        }
    }

}