namespace StateMachine
{
    /**
     * <summary>
     * Defines the lifecycle hooks for a state in the hierarchical state machine.
     * States execute these methods during their lifetime, typically triggered by the StateMachine.
     * </summary>
     */
    public interface IState
    {
        /**
         * <summary>
         * Called when the state becomes active. Use this to initialize state-specific behavior.
         * </summary>
         */
        void OnEnter();
        
        /**
         * <summary>
         * Called when the state becomes inactive. Use this to clean up state-specific behavior.
         * </summary>
         */
        void OnExit();
        
        /**
         * <summary>
         * Called every frame while the state is active. Only the leaf (deepest) active state executes this.
         * </summary>
         * <param name="deltaTime">Time elapsed since the last frame in seconds.</param>
         */
        void OnUpdate(float deltaTime);
        
        /**
         * <summary>
         * Called every physics tick while the state is active. Only the leaf (deepest) active state executes this.
         * </summary>
         * <param name="deltaTime">Time elapsed since the last physics update in seconds.</param>
         */
        void OnFixedUpdate(float deltaTime);
        
        /**
         * <summary>
         * Called after all Update calls have completed. Only the leaf (deepest) active state executes this.
         * </summary>
         * <param name="deltaTime">Time elapsed since the last frame in seconds.</param>
         */
        void OnLateUpdate(float deltaTime);
    }
}