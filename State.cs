using System.Collections.Generic;

namespace Extensions.StateMachine
{
    public abstract class State<T> : IState where T : class
    {
        public readonly StateMachine<T> Machine;
        public readonly T Host;
        
        public readonly State<T> Parent;
        public State<T> ActiveChild;
        
        private readonly List<IActivity> _activities = new List<IActivity>();
        public IReadOnlyList<IActivity> Activities => _activities;

        protected State(StateMachine<T> stateMachine, State<T> parent)
        {
            Machine = stateMachine;
            Parent = parent;
        }

        public void AddActivity(IActivity activity)
        {
            if (activity == null) return;
            
            _activities.Add(activity);
        }

        protected virtual State<T> GetInitialState() => null;
        protected virtual State<T> GetTransition() => null;

        #region Lifecycle Hooks
        
        public virtual void OnEnter()
        {
        
        }

        public virtual void OnExit()
        {
        
        }

        public virtual void OnUpdate(float deltaTime)
        {
        
        }

        public virtual void OnFixedUpdate(float deltaTime)
        {
        
        }

        public virtual void OnLateUpdate(float deltaTime)
        {
        
        }
        
        #endregion 
        
        #region Templates

        internal void Enter()
        {
            if (Parent != null)
            {
                Parent.ActiveChild = this;
            }
            
            OnEnter();
            
            var init = GetInitialState();
            init?.Enter();
        }

        internal void Exit()
        {
            ActiveChild?.Exit();
            ActiveChild = null;
            OnExit();
        }

        internal void Update(float deltaTime)
        {
            State<T> t = GetTransition();

            if (t != null)
            {
                Machine.Sequencer.RequestTransition(this, t);
                return;
            }
            
            ActiveChild?.Update(deltaTime);
            
            OnUpdate(deltaTime);
        }
        
        #endregion
        
        #region Retrieval

        public State<T> Leaf()
        {
            State<T> current = this;
            while (current.ActiveChild != null)
            {
                current = current.ActiveChild;
            }
            return current;
        }
        
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