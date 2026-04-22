using System.Collections.Generic;
using UnityEngine;

namespace StateMachine
{
    /**
     * <summary>
     * Represents a state in a hierarchical state machine.
     * States can have child states, creating a tree structure. Only leaf states (deepest active) execute OnUpdate.
     * States can attach async Activities that execute during transitions.
     * </summary>
     * <typeparam name="T">The MonoBehaviour type that hosts this state machine.</typeparam>
     */
    public abstract class State<T> where T : MonoBehaviour
    {
        /**
         * <summary>The state machine managing this state.</summary>
         */
        public StateMachine<T> Machine { get; private set; }

        /**
         * <summary>The MonoBehaviour instance that hosts the state machine.</summary>
         */
        public T Host => Machine.Host;
        
        /**
         * <summary>This state's parent in the hierarchy. Null for the root state.</summary>
         */
        public State<T> Parent { get; private set; }
        
        /**
         * <summary>The currently active child state. Used to determine which branch is active in the tree.</summary>
         */
        public State<T> ActiveChild;
        
        /**
         * <summary>Async activities that activate/deactivate during state transitions.</summary>
         */
        private readonly List<IActivity> _activities = new();
        public IReadOnlyList<IActivity> Activities => _activities;

        private Transition<T> _transition;
        private State<T> _initialState;

        #region Builders
        
        /**
         * <summary>
         * Sets the state machine that manages this state. Must be called before using the state.
         * </summary>
         *
         * <param name="machine">The state machine that will manage this state.</param>
         * <returns>This state instance for method chaining.</returns>
         */
        public State<T> WithMachine(StateMachine<T> machine)
        {
            Machine = machine;
            machine.RegisterState(this);
            return this;
        }
        
        /**
         * <summary>
         * Sets the parent state of this state. Establishes the hierarchy and allows for nested states
         * to be activated when the parent is active. Parent states can have multiple child states, but only one active at a time.
         * </summary>
         *
         * <param name="parent">The parent state of this state. Null if this is the root state.</param>
         * <returns>This state instance for method chaining.</returns>
         */
        public State<T> WithParent(State<T> parent)
        {
            Parent = parent;
            return this;
        }
        
        /**
         * <summary>
         * Sets the initial child state that should automatically activate when this state is entered.
         * </summary>
         *
         * <param name="initialState">The child state to activate when this state is entered.</param>
         * <returns>This state instance for method chaining.</returns>
         */
        public State<T> WithInitialState(State<T> initialState)
        {
            _initialState = initialState;
            initialState.WithParent(this);
            return this;
        }

        /**
         * <summary>
         * Convenience method to set this state as the initial child of a parent state.
         * </summary>
         *
         * <param name="parent">The parent state to set this state as the initial child of.</param>
         * <returns>This state instance for method chaining.</returns>
         */
        public State<T> AsInitialState(State<T> parent)
        {
            parent.WithInitialState(this);
            return this;
        }
        

        /**
         * <summary>
         * Attaches an async activity to this state.
         * Activities will be activated when entering this state and deactivated when exiting.
         * </summary>
         *
         * <param name="activity">The activity to attach to this state.</param>
         * <returns>This state instance for method chaining.</returns>
         */
        public State<T> WithActivity(IActivity activity)
        {
            if (activity == null) return this;
            
            _activities.Add(activity);

            return this;
        }
        
        /**
         * <summary>
         * Sets the transition function for this state.
         * </summary>
         *
         * <param name="transition">The transition function that determines if this state should transition to another state.</param>
         * <returns>This state instance for method chaining.</returns>
         */
        public State<T> WithTransition(Transition<T> transition)
        {
            _transition = transition;
            return this;
        }
        
        #endregion

        /**
         * <summary>
         * Called by the state machine to determine which child state should automatically become active when this state enters.
         * </summary>
         */
        private State<T> GetInitialState() => _initialState;

        /**
         * <summary>
         * Called every frame to check if this state should transition to another state.
         * Return a state to request a transition to that state, or null to stay in current state.
         * </summary>
         */
        private State<T> GetTransition() => _transition?.Evaluate(Host, this);

        #region Lifecycle Hooks
        
        /**
         * <summary>Called when this state becomes active. Use to initialize state-specific behavior.</summary>
         */
        protected virtual void OnEnter()
        {
        
        }

        /**
         * <summary>Called when this state becomes inactive. Use to clean up state-specific behavior.</summary>
         */
        protected virtual void OnExit()
        {
        
        }

        /**
         * <summary>
         * Called every frame while this state (or a descendant) is active.
         * Only the leaf (deepest active) state in the tree receives these calls.
         * </summary>
         */
        protected virtual void OnUpdate(float deltaTime)
        {
        
        }

        /**
         * <summary>
         * Called every physics tick while this state (or a descendant) is active.
         * Only the leaf (deepest active) state in the tree receives these calls.
         * </summary>
         */
        public virtual void OnFixedUpdate(float deltaTime)
        {
        
        }

        /**
         * <summary>
         * Called after all Update calls for the frame.
         * Only the leaf (deepest active) state in the tree receives these calls.
         * </summary>
         */
        public virtual void OnLateUpdate(float deltaTime)
        {
        
        }
        
        #endregion 
        
        #region Internal State Machine Templates

        internal void Enter()
        {
            // Track this as the active child of the parent
            if (Parent != null)
            {
                Parent.ActiveChild = this;
            }
            
            // Call the lifecycle hook
            OnEnter();
            
            // Automatically activate the initial child state if one is specified
            var init = GetInitialState();
            init?.Enter();
        }

        /**
         * <summary>
         * Internal method called by the state machine to exit this state.
         * Handles cleanup of child states and lifecycle calls.
         * </summary>
         */
        internal void Exit()
        {
            // Recursively exit active child states first (depth-first)
            ActiveChild?.Exit();
            ActiveChild = null;
            
            // Call the lifecycle hook
            OnExit();
        }

        /**
         * <summary>
         * Internal method called by the state machine to update this state.
         * Checks for transitions and propagates updates to active children.
         * </summary>
         */
        internal void Update(float deltaTime)
        {
            // Check if this state wants to transition to another state
            State<T> transitionTarget = GetTransition();

            if (transitionTarget != null)
            {
                // Request the transition through the machine's sequencer (handles activity execution)
                Machine.TransitionTo(this, transitionTarget);
                return;
            }
            
            // Propagate update to active child state (only one branch is active)
            ActiveChild?.Update(deltaTime);
            
            // Call the lifecycle hook (only leaf state will actually execute)
            OnUpdate(deltaTime);
        }
        
        #endregion
        
        #region State Tree Traversal Helpers

        /**
         * <summary>
         * Returns the deepest active leaf state in the hierarchy starting from this state.
         * Useful for identifying which concrete state is currently executing.
         * </summary>
         */
        public State<T> Leaf()
        {
            State<T> current = this;
            while (current.ActiveChild != null)
            {
                current = current.ActiveChild;
            }
            return current;
        }
        
        /**
         * <summary>
         * Returns all states from this state up to the root, in order.
         * Useful for debugging or understanding the active state path.
         * </summary>
         */
        public IEnumerable<State<T>> PathToRoot()
        {
            for (State<T> current = this; current != null; current = current.Parent)
            {
                yield return current;
            }
        }
        
        #endregion
    }
}