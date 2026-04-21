using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine
{
    /**
     * <summary>
     * Executes phase steps sequentially - each step must complete before the next begins.
     * Used for state transitions that require ordered activity execution (exit, then enter).
     * </summary>
     */
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

        /**
         * <summary>Starts the sequence by loading the first step.</summary>
         */
        public void Start() => Next();

        /**
         * <summary>Advances to the next phase step. Sets IsDone when all steps are exhausted.</summary>
         */
        private void Next()
        {
            _index++;
            if (_index >= _phaseSteps.Count)
            {
                IsDone = true;
                return;
            }
            
            // Start the async task for this step
            _currentTask = _phaseSteps[_index](_cancellationToken);
        }

        /**
         * <summary>
         * Updates the current step. When the current task completes, advances to the next.
         * Returns true when all steps are done.
         * </summary>
         */
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
}