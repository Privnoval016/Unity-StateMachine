using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Extensions.StateMachine
{
    public interface ISequence
    {
        bool IsDone { get; }
        void Start();
        bool Update();
    }
    
    public delegate UniTask PhaseStep(CancellationToken cancellationToken);

    public class SequentialPhase : ISequence
    {
        private readonly List<PhaseStep> _phaseSteps;
        private readonly CancellationToken _cancellationToken;
        private int _index = -1;
        private UniTask _currentTask;
        
        public bool IsDone { get; private set; }
        
        public SequentialPhase(List<PhaseStep> phaseSteps, CancellationToken cancellationToken)
        {
            _phaseSteps = phaseSteps;
            _cancellationToken = cancellationToken;
        }

        public void Start() => Next();

        private void Next()
        {
            _index++;
            if (_index >= _phaseSteps.Count)
            {
                IsDone = true;
                return;
            }
            
            _currentTask = _phaseSteps[_index](_cancellationToken);
        }

        public bool Update()
        {
            if (IsDone) return true;
            
            if (_currentTask.Status.IsCompleted())
            {
                Next();
            }

            return IsDone;
        }
    }

    public class ParallelPhase : ISequence
    {
        private readonly List<PhaseStep> _phaseSteps;
        private readonly CancellationToken _cancellationToken;
        List<UniTask> _tasks;
        
        public bool IsDone { get; private set; }
        
        public ParallelPhase(List<PhaseStep> phaseSteps, CancellationToken cancellationToken)
        {
            _phaseSteps = phaseSteps;
            _cancellationToken = cancellationToken;
        }
        
        public void Start()
        {
            if (_phaseSteps == null || _phaseSteps.Count == 0)
            {
                IsDone = true;
                return;
            }
            
            _tasks = new List<UniTask>();
            foreach (var phaseStep in _phaseSteps)
            {
                _tasks.Add(phaseStep(_cancellationToken));
            }
        }

        public bool Update()
        {
            if (IsDone) return true;

            bool trueForAllTasks = true;
            foreach (var task in _tasks)
            {
                if (!task.Status.IsCompleted())
                {
                    trueForAllTasks = false;
                    break;
                }
            }
            
            return _tasks == null || trueForAllTasks;
        }
    }
    
    public class NoOpPhase : ISequence
    {
        public bool IsDone { get; private set; }

        public void Start() => IsDone = true;

        public bool Update() => true;
    }
}