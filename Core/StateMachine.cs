using System;
using System.Collections.Generic;
using Extensions.Other;
using UnityEngine;

namespace StateMachine
{
    /**
     * <summary>
     * A hierarchical state machine that manages state transitions and lifecycle.
     * The state machine maintains a tree of states and ensures only one path from root to leaf is active.
     * Transitions are mediated through the TransitionSequencer to handle async activity execution.
     * </summary>
     * <typeparam name="T">The MonoBehaviour type that hosts this state machine.</typeparam>
     */
    public class StateMachine<T> : MonoBehaviourUpdatable where T : MonoBehaviour
    {
        /**
         * <summary>The root state of the hierarchy. Always active.</summary>
         */
        public readonly State<T> Root;
        
        /**
         * <summary>The MonoBehaviour instance that hosts this state machine.</summary>
         */
        public readonly T Host;
        
        /**
         * <summary>Manages state transitions and activity sequencing.</summary>
         */
        private readonly TransitionSequencer<T> _sequencer;
        
        /**
         * <summary>Whether the state machine has been started.</summary>
         */
        private bool _started;

        private readonly Dictionary<Type, State<T>> _stateRegistry;
        
        /**
         * <summary>
         * Creates a new state machine with the given host and root state.
         * </summary>
         */
        public StateMachine(T host, State<T> root) : base(host.gameObject)
        {
            Host = host;
            Root = root;
            _sequencer = new TransitionSequencer<T>(this);
            
            _stateRegistry = new Dictionary<Type, State<T>>();
        }
        
        #region State Tracking
        
        public bool RegisterState(State<T> state, bool overwrite = false)
        {
            var type = state.GetType();
            if (_stateRegistry.ContainsKey(type))
            {
                if (!overwrite)
                {
                    Debug.LogError($"State of type {type} is already registered. Set overwrite to true to replace it.");
                    return false;
                }
                
                Debug.LogWarning($"State of type {type} is already registered. Overwriting with new instance.");
            }
            
            _stateRegistry[type] = state;
            return true;
        }
        
        public State<T> GetState(Type type)
        {
            if (_stateRegistry.TryGetValue(type, out var state))
            {
                return state;
            }
            
            Debug.LogError($"State of type {type} is not registered in the state machine.");
            return null;
        }

        public State<T> GetState<TState>() where TState : State<T>
        {
            Type type = typeof(TState);
            return GetState(type);
        }

        public bool IsState(Type type)
        {
            return _stateRegistry.ContainsKey(type);
        }

        public bool IsState<TState>() where TState : State<T>
        {
            Type type = typeof(TState);
            return IsState(type);
        }
        
        #endregion

        /**
         * <summary>
         * Starts the state machine by entering the root state and its initial child.
         * Idempotent - subsequent calls have no effect.
         * </summary>
         */
        public void Start()
        {
            if (_started) return;
            
            _started = true;
            Root.Enter();
        }

        /**
         * <summary>
         * Transitions the state machine to the given state. Used as an "Any State" transition for external triggers.
         * </summary>
         *
         * <param name="nextState">The state to transition to. Must be part of the state hierarchy.</param>
         */
        public void TransitionTo(State<T> nextState)
        {
            var leaf = Root.Leaf();
            TransitionTo(leaf, nextState);
        }

        public void TransitionTo(State<T> from, State<T> to)
        {
            _sequencer.RequestTransition(from, to);
        }

        /**
         * <summary>
         * Called every frame by MonoBehaviourUpdater.
         * Advances transition sequences and delegates state updates to the active state tree.
         * </summary>
         */
        public override void Update()
        {
            base.Update();
            
            if (!_started) Start();
            _sequencer.Tick(Time.deltaTime);
            
            Debug.Log($"Current active state: {Root.Leaf().GetType().Name}");
        }
        
        /**
         * <summary>
         * Internal update called by the sequencer when no transitions are active.
         * Propagates OnUpdate through the active state hierarchy.
         * </summary>
         */
        internal void InternalTick(float deltaTime) => Root.Update(deltaTime);

        /**
         * <summary>
         * Called after all Update calls in the frame.
         * Propagates OnLateUpdate through the active state hierarchy.
         * </summary>
         */
        public override void LateUpdate()
        {
            base.LateUpdate();
            Root.OnLateUpdate(Time.deltaTime);
        }

        /**
         * <summary>
         * Called at a fixed timestep for physics.
         * Propagates OnFixedUpdate through the active state hierarchy.
         * </summary>
         */
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            Root.OnFixedUpdate(Time.fixedDeltaTime);
        }

        /**
         * <summary>
         * Changes the state hierarchy from one state to another.
         * Exits states from 'from' up to LCA, then enters states from LCA down to 'to'.
         * This is called by the TransitionSequencer after activities complete.
         * </summary>
         */
        public void ChangeState(State<T> from, State<T> to)
        {
            if (from == to || from == null || to == null) return;
            
            // Find the lowest common ancestor to determine state boundaries
            var lca = TransitionSequencer<T>.LowestCommonAncestor(from, to);
            
            // Exit states up until the parent
            for (var current = from; current != lca; current = current.Parent)
            {
                current.Exit();
            }
            
            // Collect states to enter in root-to-leaf order
            var stack = new Stack<State<T>>();
            for (var current = to; current != lca; current = current.Parent)
            {
                stack.Push(current);
            }
            
            // Enter states in the correct order (root to leaf)
            while (stack.Count > 0)
            {
                stack.Pop().Enter();
            }
        }
    }
}