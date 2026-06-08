using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace StateMachine
{
    /**
     * <summary>
     * Manages state transitions including exit/enter activity execution.
     * Sequences exit activities → state change → enter activities.
     * Supports both sequential (one at a time) and parallel (all at once) activity execution.
     * Also handles queuing of transitions requested during other transitions.
     * </summary>
     *
     * <remarks>
     * <b>Leaf normalization:</b> <see cref="RequestTransition"/> calls <c>from.Leaf()</c> so that
     * parent-initiated transitions (e.g. <c>GroundedState</c> jumping while <c>SprintState</c> is the
     * active child) always exit the deepest active state. Without this, the child's activities are never
     * deactivated and their <c>Mode</c> stays <c>Active</c>, causing them to be skipped on re-entry.
     *
     * <b>Phase 1 interrupt:</b> while exit activities are running (<c>_inExitPhase</c>),
     * <see cref="Tick"/> continues calling <c>Machine.InternalTick</c> so the state machine can
     * detect new inputs. If a new transition with a different target arrives during Phase 1,
     * the exit is cancelled immediately via the cancellation token and the new target replaces the
     * queued Phase 2+3 callback. This ensures input actions (jump, dodge) override deceleration
     * clips without a perceptible delay.
     * </remarks>
     */
    public class TransitionSequencer<T> where T : MonoBehaviour
    {
        /** <summary>The state machine this sequencer manages transitions for.</summary> */
        public readonly StateMachine<T> Machine;

        /** <summary>The current phase being executed (exit activities, enter activities, or null if idle).</summary> */
        private ISequence _sequencer;

        /** <summary>The callback to execute when the current phase completes (transitions to next phase).</summary> */
        private Action _nextPhase;

        /** <summary>A transition requested while another transition is in progress. Fires after the current one completes.</summary> */
        private (State<T> from, State<T> to)? _pending;

        /** <summary>Cancellation token source for cancelling async activities.</summary> */
        private CancellationTokenSource _source;

        /** <summary>Whether to execute activities sequentially (one at a time) or in parallel (all at once).</summary> */
        public readonly bool UseSequential;

        // Phase 1 interrupt tracking
        private bool _inExitPhase;
        private State<T> _transitionFrom;
        private State<T> _transitionTo;

        /** <summary>Optional delegate that decides whether to suppress exit animations for a given transition.</summary> */
        private readonly Func<State<T>, State<T>, bool> _exitSkipPolicy;

        public TransitionSequencer(StateMachine<T> stateMachine, bool useSequential = false,
            Func<State<T>, State<T>, bool> exitSkipPolicy = null)
        {
            Machine         = stateMachine;
            UseSequential   = useSequential;
            _source         = new CancellationTokenSource();
            _exitSkipPolicy = exitSkipPolicy;
        }

        /**
         * <summary>
         * Requests a transition from one state to another.
         * </summary>
         * <remarks>
         * <c>from</c> is normalized to its deepest active leaf before processing so that
         * parent-initiated transitions exit the correct child states. If exit activities are currently
         * running and the new target differs from the in-flight target, Phase 1 is cancelled
         * immediately and the new target becomes the Phase 2+3 destination. If a different kind of
         * transition is already in progress, the request is queued as pending.
         * </remarks>
         */
        public void RequestTransition(State<T> from, State<T> to)
        {
            if (from == null || to == null) return;

            // Normalize to the deepest active descendant so child activities are always deactivated
            from = from.Leaf();

            // Self-transitions are no-ops — parent states may re-request the current active child
            if (from == to) return;

            if (_sequencer != null)
            {
                if (_inExitPhase && _nextPhase != null && to != _transitionTo)
                {
                    // Interrupt the running exit phase: cancel activities, redirect to new target
                    var interruptFrom = _transitionFrom;
                    var interruptTo   = to;

                    _source.Cancel();
                    _source = new CancellationTokenSource();

                    var lca   = LowestCommonAncestor(interruptFrom, interruptTo);
                    var enter = StatesToEnter(interruptTo, lca);

                    _transitionTo = interruptTo;
                    _nextPhase = () =>
                    {
                        _inExitPhase = false;
                        Machine.ChangeState(interruptFrom, interruptTo);
                        var steps = GatherPhaseSteps(enter, false);
                        _sequencer = UseSequential
                            ? new SequentialPhase(steps, _source.Token)
                            : new ParallelPhase(steps, _source.Token);
                        _sequencer.Start();
                    };
                    return;
                }

                // InternalTick re-fires the same target every frame; ignore to avoid spurious re-entry
                if (to == _transitionTo) return;
                _pending = (from, to);
                return;
            }

            BeginTransition(from, to);
        }

        /**
         * <summary>
         * Begins executing a transition between two states.
         * Phase 1 — exit activities; Phase 2 — state change; Phase 3 — enter activities.
         * </summary>
         */
        private void BeginTransition(State<T> from, State<T> to)
        {
            var lca          = LowestCommonAncestor(from, to);
            var statesToExit  = StatesToExit(from, lca);
            var statesToEnter = StatesToEnter(to, lca);

            _transitionFrom = from;
            _transitionTo   = to;
            _inExitPhase    = true;

            // If the registered policy says to skip exits for this pair, pre-cancel the exit token
            CancellationToken exitToken;
            if (_exitSkipPolicy?.Invoke(from, to) == true)
            {
                _source.Cancel();
                exitToken = _source.Token;               // already cancelled — DeactivateAsync skips clips
                _source   = new CancellationTokenSource(); // fresh token for Phase 2+3
            }
            else
            {
                exitToken = _source.Token;
            }

            var exitSteps = GatherPhaseSteps(statesToExit, true);
            _sequencer = UseSequential
                ? new SequentialPhase(exitSteps, exitToken)
                : new ParallelPhase(exitSteps, exitToken);
            _sequencer.Start();

            _nextPhase = () =>
            {
                _inExitPhase = false;
                Machine.ChangeState(from, to);

                var enterSteps = GatherPhaseSteps(statesToEnter, false);
                _sequencer = UseSequential
                    ? new SequentialPhase(enterSteps, _source.Token)
                    : new ParallelPhase(enterSteps, _source.Token);
                _sequencer.Start();
            };
        }

        /**
         * <summary>
         * Completes the current transition and checks if there is a pending transition to execute.
         * </summary>
         */
        private void EndTransition()
        {
            _sequencer = null;

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
         * </summary>
         * <remarks>
         * During Phase 1 (exit activities), <c>Machine.InternalTick</c> is still called so new
         * inputs can fire <see cref="RequestTransition"/> and interrupt the exit. During Phase 2+3
         * (enter activities), <c>InternalTick</c> is suppressed to prevent double-transitions.
         * </remarks>
         */
        public void Tick(float deltaTime)
        {
            if (_sequencer != null)
            {
                bool done = _sequencer.Update();
                if (done)
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
                else if (_inExitPhase)
                {
                    // Continue ticking the state machine during exit so new inputs can interrupt
                    Machine.InternalTick(deltaTime);
                }
                return;
            }

            Machine.InternalTick(deltaTime);
        }

        /**
         * <summary>
         * Activates all <see cref="ActivityMode.Inactive"/> activities on the initial root-to-leaf path.
         * Called once from <see cref="StateMachine{T}.Start"/> because <c>Root.Enter()</c> bypasses the
         * sequencer entirely, so <see cref="GatherPhaseSteps"/> is never invoked for the first state.
         * </summary>
         */
        internal void BootstrapActivities()
        {
            for (var s = Machine.Root; s != null; s = s.ActiveChild)
                foreach (var a in s.Activities)
                    if (a.Mode == ActivityMode.Inactive)
                        a.ActivateAsync(_source.Token).Forget();
        }

        #region Static Helpers

        /**
         * <summary>
         * Gathers all async activity steps from a list of states that need to be activated or deactivated.
         * Activities are only included if they are in the appropriate mode (Inactive for activate, Active for deactivate).
         * </summary>
         */
        private static List<PhaseStep> GatherPhaseSteps(List<State<T>> chain, bool deactivate)
        {
            var steps = new List<PhaseStep>();

            foreach (var state in chain)
            {
                foreach (var a in state.Activities)
                {
                    if (deactivate)
                    {
                        if (a.Mode == ActivityMode.Active)
                            steps.Add(ct => a.DeactivateAsync(ct));
                    }
                    else
                    {
                        if (a.Mode == ActivityMode.Inactive)
                            steps.Add(ct => a.ActivateAsync(ct));
                    }
                }
            }

            return steps;
        }

        /**
         * <summary>
         * Returns all states that need to exit when transitioning from <paramref name="from"/> to a target.
         * Walks from <paramref name="from"/> upward to (but not including) the LCA.
         * </summary>
         */
        private static List<State<T>> StatesToExit(State<T> from, State<T> lca)
        {
            var states = new Queue<State<T>>();

            states.Enqueue(from);

            for (var current = from.Parent; current != null && current != lca; current = current.Parent)
                states.Enqueue(current);

            return new List<State<T>>(states);
        }

        /**
         * <summary>
         * Returns all states that need to enter when transitioning to <paramref name="to"/>.
         * Walks from <paramref name="to"/> upward to (but not including) the LCA, returned root-to-leaf.
         * </summary>
         */
        private static List<State<T>> StatesToEnter(State<T> to, State<T> lca)
        {
            var states = new Stack<State<T>>();

            states.Push(to);

            for (var current = to.Parent; current != null && current != lca; current = current.Parent)
                states.Push(current);

            return new List<State<T>>(states);
        }

        /**
         * <summary>
         * Finds the Lowest Common Ancestor (LCA) of two states in the hierarchy.
         * The LCA is the deepest state that is an ancestor of both states.
         * </summary>
         */
        public static State<T> LowestCommonAncestor(State<T> a, State<T> b)
        {
            var aParent = new HashSet<State<T>>();
            for (var current = a; current != null; current = current.Parent)
                aParent.Add(current);

            for (var current = b; current != null; current = current.Parent)
                if (aParent.Contains(current))
                    return current;

            return null;
        }

        #endregion
    }
}
