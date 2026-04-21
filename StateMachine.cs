using System.Collections.Generic;
using Extensions.Other;
using UnityEngine;

namespace StateMachine
{
    public class StateMachine<T> : MonoBehaviourUpdatable where T : class
    {
        public readonly State<T> Root;
        public readonly TransitionSequencer<T> Sequencer;
        private bool started;
        
        public StateMachine(State<T> root)
        {
            Root = root;
            Sequencer = new TransitionSequencer<T>(this);
        }

        public void Start()
        {
            if (started) return;
            
            started = true;
            Root.Enter();
        }

        public void Stop()
        {
            if (!started) return;
            started = false;
        }

        public override void Update()
        {
            base.Update();
            Sequencer.Tick(Time.deltaTime);
        }
        
        internal void InternalTick(float deltaTime) => Root.Update(deltaTime);

        public override void LateUpdate()
        {
            base.LateUpdate();
            Root.OnLateUpdate(Time.deltaTime);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            Root.OnFixedUpdate(Time.fixedDeltaTime);
        }

        public void ChangeState(State<T> from, State<T> to)
        {
            if (from == to || from == null || to == null) return;
            
            var lca = TransitionSequencer<T>.LowestCommonAncestor(from, to);
            
            for (var current = from; current != lca; current = current.Parent)
            {
                current.Exit();
            }
            
            var stack = new Stack<State<T>>();
            for (var current = to; current != lca; current = current.Parent)
            {
                stack.Push(current);
            }
            
            while (stack.Count > 0)
            {
                stack.Pop().OnEnter();
            }
        }
    }
}