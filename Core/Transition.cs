using System;
using UnityEngine;

namespace StateMachine
{
    public abstract class Transition<T> where T : MonoBehaviour
    {
        public abstract State<T> Evaluate(T ctx, State<T> currentState);
    }
    
    public class FuncTransition<T> : Transition<T> where T : MonoBehaviour
    {
        private readonly Func<T, State<T>, State<T>> _evaluator;

        public FuncTransition(Func<T, State<T>, State<T>> evaluator)
        {   
            _evaluator = evaluator;
        }
        
        public override State<T> Evaluate(T ctx, State<T> currentState)
        {
            return _evaluator(ctx, currentState);
        }
    }
}