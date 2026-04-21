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
    public abstract class State<T> : IState where T : MonoBehaviour
    {
        /**
         * <summary>The state machine managing this state.</summary>
         */
        public readonly StateMachine<T> Machine;
        
        /**
         * <summary>The MonoBehaviour instance that hosts the state machine.</summary>
         */
        public readonly T Host;
        
        /**
         * <summary>This state's parent in the hierarchy. Null for the root state.</summary>
         */
        public readonly State<T> Parent;
        
        /**
         * <summary>The currently active child state. Used to determine which branch is active in the tree.</summary>
         */
        public State<T> ActiveChild;
        
        /**
         * <summary>Async activities that activate/deactivate during state transitions.</summary>
         */
        private readonly List<IActivity> _activities = new();
        public IReadOnlyList<IActivity> Activities => _activities;

        protected State(StateMachine<T> stateMachine, State<T> parent)
        {
            Machine = stateMachine;
            Host = Machine.Host;
            Parent = parent;
        }

        /**
         * <summary>
         * Attaches an async activity to this state.
         * Activities will be activated when entering this state and deactivated when exiting.
         * </summary>
         */
        public void AddActivity(IActivity activity)
        {
            if (activity == null) return;
            
            _activities.Add(activity);
        }

        /**
         * <summary>
         * Called by the state machine to determine which child state should automatically become active when this state enters.
         * Override to implement default child state selection. Return null to have no initial child.
         * </summary>
         */
        protected virtual State<T> GetInitialState() => null;
        
        /**
         * <summary>
         * Called every frame to check if this state should transition to another state.
         * Return a state to request a transition to that state, or null to stay in current state.
         * </summary>
         */
        protected virtual State<T> GetTransition() => null;

        #region Lifecycle Hooks
        
        /**
         * <summary>Called when this state becomes active. Use to initialize state-specific behavior.</summary>
         */
        public virtual void OnEnter()
        {
        
        }

        /**
         * <summary>Called when this state becomes inactive. Use to clean up state-specific behavior.</summary>
         */
        public virtual void OnExit()
        {
        
        }

        /**
         * <summary>
         * Called every frame while this state (or a descendant) is active.
         * Only the leaf (deepest active) state in the tree receives these calls.
         * </summary>
         */
        public virtual void OnUpdate(float deltaTime)
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
                // Request the transition through the sequencer (handles activity execution)
                Machine.Sequencer.RequestTransition(this, transitionTarget);
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