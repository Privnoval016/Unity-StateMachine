using System;
using System.Collections.Generic;
using System.Threading;

namespace Extensions.StateMachine
{
    public class TransitionSequencer<T> where T : class
    {
        public readonly StateMachine<T> Machine;
        
        private ISequence _sequencer;
        private Action _nextPhase;
        private (State<T> from, State<T> to)? _pending;
        private State<T> _lastFrom;
        private State<T> _lastTo;

        private CancellationTokenSource _source;
        public readonly bool UseSequential;

        public TransitionSequencer(StateMachine<T> stateMachine, bool useSequential = true)
        {
            Machine = stateMachine;
            UseSequential = useSequential;
        }

        public void RequestTransition(State<T> from, State<T> to)
        {
            if (to == null || from == to) return;
            
            if (_sequencer != null)
            {
                _pending = (from, to);
                BeginTransition(from, to);
            }
        }

        public void BeginTransition(State<T> from, State<T> to)
        {
            var lca = LowestCommonAncestor(from, to);
            var statesToExit = StatesToExit(from, lca);
            var statesToEnter = StatesToEnter(to, lca);
            
            var exitSteps = GatherPhaseSteps(statesToExit, true);
            
            _sequencer = UseSequential ? new SequentialPhase(exitSteps, _source.Token) : 
                                         new ParallelPhase(exitSteps, _source.Token);
            _sequencer.Start();

            _nextPhase = () =>
            {
                Machine.ChangeState(from, to);
                var enterSteps = GatherPhaseSteps(statesToEnter, false);
                _sequencer = UseSequential ? new SequentialPhase(enterSteps, _source.Token) : 
                                             new ParallelPhase(enterSteps, _source.Token);
                _sequencer.Start();
            };
        }

        public void EndTransition()
        {
            _sequencer = null;
            
            if (_pending.HasValue)
            {
                (State<T> from, State<T> to) = _pending.Value;
                _pending = null;
                BeginTransition(from, to);
            }
        }

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
            
            Machine.InternalTick(deltaTime);
        }

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
                        if (a.Mode == ActivityMode.Active)
                        {
                            steps.Add(ct => a.DeactivateAsync(ct));
                        }
                    }
                    else
                    {
                        if (a.Mode == ActivityMode.Inactive)
                        {
                            steps.Add(ct => a.ActivateAsync(ct));
                        }
                    }
                }
            }
            
            return steps;
        }

        private static List<State<T>> StatesToExit(State<T> from, State<T> lca)
        {
            var states = new List<State<T>>();
            for (var current = from; current != null && current != lca; current = current.Parent)
            {
                states.Add(current);
            }

            return states;
        }

        private static List<State<T>> StatesToEnter(State<T> to, State<T> lca)
        {
            var states = new Stack<State<T>>();
            
            for (var current = to; current != null && current != lca; current = current.Parent)
            {
                states.Push(current);
            }
            
            return new List<State<T>>(states);
        }

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
    }
}