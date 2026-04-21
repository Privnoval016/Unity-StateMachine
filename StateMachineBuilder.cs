using System.Collections.Generic;
using System.Reflection;

namespace StateMachine
{
    
    public class StateMachineBuilder<T> where T : class
    {
        readonly State<T> root;
    
        public StateMachineBuilder(State<T> root) 
        {
            this.root = root;
        }

        public StateMachine<T> Build() 
        {
            var m = new StateMachine<T>(root);
            //Wire(root, m, new HashSet<State<T>>());
            return m;
        }
    }
    
}