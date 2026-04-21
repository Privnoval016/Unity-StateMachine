using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine
{
    /**
     * <summary>
     * Executes phase steps in parallel - all steps start immediately and run concurrently.
     * Used for state transitions where multiple activities can activate/deactivate simultaneously.
     * </summary>
     */
    public class ParallelPhase : ISequence
    {
        private readonly List<PhaseStep> _phaseSteps;
        private readonly CancellationToken _cancellationToken;
        private List<UniTask> _tasks;
        
        public bool IsDone { get; private set; }
        
        public ParallelPhase(List<PhaseStep> phaseSteps, CancellationToken cancellationToken)
        {
            _phaseSteps = phaseSteps;
            _cancellationToken = cancellationToken;
        }
        
        /**
         * <summary>
         * Starts all phase steps concurrently. Each step begins immediately.
         * Sets IsDone immediately if there are no steps to execute.
         * </summary>
         */
        public void Start()
        {
            if (_phaseSteps == null || _phaseSteps.Count == 0)
            {
                IsDone = true;
                return;
            }
            
            _tasks = new List<UniTask>(_phaseSteps.Count);
            foreach (var phaseStep in _phaseSteps)
            {
                // All tasks start immediately in parallel
                _tasks.Add(phaseStep(_cancellationToken));
            }
        }

        /**
         * <summary>
         * Updates parallel execution. Returns true when all tasks have completed.
         * Sets IsDone to true when all concurrent tasks finish.
         * </summary>
         */
        public bool Update()
        {
            if (IsDone) return true;
            
            IsDone = _tasks == null || _tasks.TrueForAll(t => t.Status.IsCompleted());
            
            return IsDone;
        }
    }
}