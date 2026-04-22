using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace StateMachine
{
    /**
     * <summary>
     * Manages state transitions including exit/enter activity execution.
     * Sequences exit activities -> state change -> enter activities.
     * Supports both sequential (one at a time) and parallel (all at once) activity execution.
     * Also handles queuing of transitions requested during other transitions.
     * </summary>
     */
    public class TransitionSequencer<T> where T : MonoBehaviour
    {
        /**
         * <summary>The state machine this sequencer manages transitions for.</summary>
         */
        public readonly StateMachine<T> Machine;
        
        /**
         * <summary>The current phase being executed (exit activities, enter activities, or null if idle).</summary>
         */
        private ISequence _sequencer;
        
        /**
         * <summary>The callback to execute when the current phase completes (transitions to next phase).</summary>
         */
        private Action _nextPhase;
        
        /**
         * <summary>A transition requested while another transition is in progress. Will execute after current one completes.</summary>
         */
        private (State<T> from, State<T> to)? _pending;
        
        /**
         * <summary>Cancellation token source for cancelling async activities if needed.</summary>
         */
        private CancellationTokenSource _source;
        
        /**
         * <summary>Whether to execute activities sequentially (one at a time) or in parallel (all at once).</summary>
         */
        public readonly bool UseSequential;

        public TransitionSequencer(StateMachine<T> stateMachine, bool useSequential = false)
        {
            Machine = stateMachine;
            UseSequential = useSequential;
            _source = new CancellationTokenSource();
        }

        /**
         * <summary>
         * Requests a transition from one state to another.
         * If a transition is already in progress, this request is queued as a pending transition.
         * </summary>
         */
        public void RequestTransition(State<T> from, State<T> to)
        {
            if (from == null || to == null) return;
            
            //if (from == to) return; // self transitions allowed
            
            if (_sequencer != null)
            {
                _pending = (from, to);
                return;
            }
            
            BeginTransition(from, to);
        }

        /**
         * <summary>
         * Begins executing a transition between two states.
         * Phase 1: Execute exit activities for states being exited (sequential or parallel)
         * Phase 2: Change state hierarchy
         * Phase 3: Execute enter activities for states being entered (sequential or parallel)
         * </summary>
         */
        private void BeginTransition(State<T> from, State<T> to)
        {
            var lca = LowestCommonAncestor(from, to);
            var statesToExit = StatesToExit(from, lca);
            var statesToEnter = StatesToEnter(to, lca);
            
            // Phase 1: Create exit activity steps
            var exitSteps = GatherPhaseSteps(statesToExit, true);
            
            _sequencer = UseSequential ? new SequentialPhase(exitSteps, _source.Token) : 
                                         new ParallelPhase(exitSteps, _source.Token);
            _sequencer.Start();

            _nextPhase = () =>
            {
                // Phase 2: Actually change the state hierarchy
                Machine.ChangeState(from, to);
                
                // Phase 3: Create and start enter activity steps
                var enterSteps = GatherPhaseSteps(statesToEnter, false);
                _sequencer = UseSequential ? new SequentialPhase(enterSteps, _source.Token) : 
                                             new ParallelPhase(enterSteps, _source.Token);
                _sequencer.Start();
            };
        }

        /**
         * <summary>
         * Completes the current transition and checks if there's a pending transition to execute.
         * </summary>
         */
        private void EndTransition()
        {
            _sequencer = null;
            
            // If a transition was requested while we were transitioning, execute it now
            if (_pending.HasValue)
            {
                (State<T> from, State<T> to) p = _pending.Value;
                _pending = null;
                BeginTransition(p.from, p.to);
            }
        }

        /**
         * <summary>
         * Called every frame by the state machine to advance transition execution.
         * Returns early if a transition is in progress, preventing normal state updates.
         * </summary>
         */
        public void Tick(float deltaTime)
        {
            if (_sequencer != null)
            {
                if (_sequencer.Update())
                {
                    if (_nextPhase != null)
                    {
                        var n = _nextPhase;
                        _nextPhase = null;
                        n.Invoke();
                    }
                    else
                    {
                        EndTransition();
                    }
                }
                return;
            }
            
            // No transition in progress - proceed with normal state machine update
            Machine.InternalTick(deltaTime);
        }
        
        #region Static Helpers

        /**
         * <summary>
         * Gathers all async activity steps from a list of states that need to be activated or deactivated.
         * Activities are only included if they're in the appropriate state (Inactive for activate, Active for deactivate).
         * </summary>
         */
        private static List<PhaseStep> GatherPhaseSteps(List<State<T>> chain, bool deactivate)
        {
            var steps = new List<PhaseStep>();
            
            foreach (var state in chain)
            {
                var acts = state.Activities;

                foreach (var a in acts)
                {
                    if (deactivate)
                    {
                        // Only deactivate activities that are currently active
                        if (a.Mode == ActivityMode.Active)
                        {
                            steps.Add(ct => a.DeactivateAsync(ct));
                        }
                    }
                    else
                    {
                        // Only activate activities that are currently inactive
                        if (a.Mode == ActivityMode.Inactive)
                        {
                            steps.Add(ct => a.ActivateAsync(ct));
                        }
                    }
                }
            }
            
            return steps;
        }

        /**
         * <summary>
         * Returns all states that need to exit when transitioning from 'from' to 'to'.
         * These are all ancestors of 'from' up to (but not including) the Lowest Common Ancestor.
         * </summary>
         */
        private static List<State<T>> StatesToExit(State<T> from, State<T> lca)
        {
            var states = new Queue<State<T>>();
            
            states.Enqueue(from); // always add current state for self transitions
            
            // Exit states in order from leaf to root
            for (var current = from.Parent; current != null && current != lca; current = current.Parent)
            {
                states.Enqueue(current);
            }

            return new List<State<T>>(states);
        }

        /**
         * <summary>
         * Returns all states that need to enter when transitioning from 'from' to 'to'.
         * These are all ancestors of 'to' up to (but not including) the Lowest Common Ancestor.
         * Returns them in the correct order (root to leaf) for proper state initialization.
         * </summary>
         */
        private static List<State<T>> StatesToEnter(State<T> to, State<T> lca)
        {
            var states = new Stack<State<T>>();
            
            states.Push(to); // always add target state for self transitions
            
            // Enter states in order from root to leaf
            for (var current = to.Parent; current != null && current != lca; current = current.Parent)
            {
                states.Push(current);
            }
            
            return new List<State<T>>(states);
        }

        /**
         * <summary>
         * Finds the Lowest Common Ancestor (LCA) of two states in the hierarchy.
         * The LCA is the deepest state that is an ancestor of both states.
         * Used to determine which states to exit and enter during a transition.
         * </summary>
         */
        public static State<T> LowestCommonAncestor(State<T> a, State<T> b)
        {
            var aParent = new HashSet<State<T>>();
            for (var current = a; current != null; current = current.Parent)
            {
                aParent.Add(current);
            }
            
            for (var current = b; current != null; current = current.Parent)
            {
                if (aParent.Contains(current))
                {
                    return current;
                }
            }
            
            return null;
        }
        
        #endregion
    }
}