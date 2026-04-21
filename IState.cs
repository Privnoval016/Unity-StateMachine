namespace StateMachine
{
    public interface IState
    {
        void OnEnter();
        void OnExit();
        void OnUpdate(float deltaTime);
        void OnFixedUpdate(float deltaTime);
        void OnLateUpdate(float deltaTime);
    }
}