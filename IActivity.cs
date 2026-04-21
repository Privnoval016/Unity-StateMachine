using System.Threading;
using Cysharp.Threading.Tasks;

namespace Extensions.StateMachine
{
    public enum ActivityMode
    {
        Inactive,
        Activating,
        Active,
        Deactivating
    }
    
    public interface IActivity
    {
        ActivityMode Mode { get; }
        UniTask ActivateAsync(CancellationToken cancellationToken);
        UniTask DeactivateAsync(CancellationToken cancellationToken);
    }

    public class DelayActivationActivity : Activity
    {
        public readonly float DelaySeconds;
        public DelayActivationActivity(float delaySeconds)
        {
            DelaySeconds = delaySeconds;
        }

        public override async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(DelaySeconds), cancellationToken: cancellationToken);
            await base.ActivateAsync(cancellationToken);
        }
    }

    public abstract class Activity : IActivity
    {
        public ActivityMode Mode { get; protected set; } = ActivityMode.Inactive;

        public virtual async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            if (Mode == ActivityMode.Inactive) return;
            
            Mode = ActivityMode.Activating;
            await UniTask.CompletedTask;
            Mode = ActivityMode.Active;
        }

        public virtual async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (Mode == ActivityMode.Inactive) return;

            Mode = ActivityMode.Deactivating;
            await UniTask.CompletedTask;
            Mode = ActivityMode.Inactive;
        }
    }
}