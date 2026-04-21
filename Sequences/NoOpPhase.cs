namespace StateMachine
{
    /**
     * <summary>
     * A no-op phase that completes immediately. Used as a placeholder or when no activities need execution.
     * </summary>
     */
    public class NoOpPhase : ISequence
    {
        public bool IsDone { get; private set; }

        /**
         * <summary>Immediately marks the phase as done.</summary>
         */
        public void Start() => IsDone = true;
        
        /**
         * <summary>Returns true since this phase is instantly complete.</summary>
         */
        public bool Update() => IsDone;
    }
}