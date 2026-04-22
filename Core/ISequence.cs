using System.Threading;
using Cysharp.Threading.Tasks;

namespace StateMachine
{
    /**
     * <summary>
     * Defines a phase sequence that executes a series of async steps.
     * Used during state transitions to manage exit/enter activity execution.
     * </summary>
     */
    public interface ISequence
    {
        /**
         * <summary>Gets whether the sequence has completed all steps.</summary>
         */
        bool IsDone { get; }
        
        /**
         * <summary>Starts executing the sequence. Should be called once before Update() calls.</summary>
         */
        void Start();
        
        /**
         * <summary>
         * Updates the sequence execution. Should be called every frame.
         * Returns true when the sequence is complete.
         * </summary>
         */
        bool Update();
    }
    
    /**
     * <summary>Delegate for an individual phase step that performs async work.</summary>
     */
    public delegate UniTask PhaseStep(CancellationToken cancellationToken);
}