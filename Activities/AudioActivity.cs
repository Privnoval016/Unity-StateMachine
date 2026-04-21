using System;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace StateMachine.Activities
{
    /**
     * <summary>
     * Activity that manages audio playback on state entry and exit.
     * Useful for playing state-specific sound effects or music loops.
     * </summary>
     */
    public class AudioActivity : Activity
    {
        /**
         * <summary>The audio source to control.</summary>
         */
        private readonly AudioSource _audioSource;

        /**
         * <summary>The audio clip to play.</summary>
         */
        private readonly AudioClip _clip;

        /**
         * <summary>Whether the audio should loop while the state is active.</summary>
         */
        private readonly bool _loop;

        /**
         * <summary>The volume to play the audio at. Range 0-1.</summary>
         */
        private readonly float _volume;

        /**
         * <summary>
         * Creates a new audio activity.
         * </summary>
         * <param name="audioSource">The AudioSource component to control. Must not be null.</param>
         * <param name="clip">The audio clip to play. Must not be null.</param>
         * <param name="loop">Whether the audio should loop while active. Default is false.</param>
         * <param name="volume">The volume to play at (0-1). Default is 1.</param>
         * <exception cref="System.ArgumentNullException">Thrown if audioSource or clip is null.</exception>
         * <exception cref="System.ArgumentException">Thrown if volume is out of range.</exception>
         */
        public AudioActivity(AudioSource audioSource, AudioClip clip, bool loop = false, float volume = 1f)
        {
            if (audioSource == null)
                throw new ArgumentNullException(nameof(audioSource), "AudioSource cannot be null.");
            if (clip == null)
                throw new ArgumentNullException(nameof(clip), "AudioClip cannot be null.");
            if (volume < 0f || volume > 1f)
                throw new ArgumentException("Volume must be between 0 and 1.", nameof(volume));

            _audioSource = audioSource;
            _clip = clip;
            _loop = loop;
            _volume = volume;
        }

        /**
         * <summary>
         * Activates by starting audio playback with the configured settings.
         * </summary>
         * <param name="cancellationToken">Token to cancel the activation.</param>
         */
        public override async UniTask ActivateAsync(CancellationToken cancellationToken)
        {
            if (Mode != ActivityMode.Inactive) return;

            Mode = ActivityMode.Activating;

            // Configure and play the audio
            _audioSource.clip = _clip;
            _audioSource.loop = _loop;
            _audioSource.volume = _volume;
            _audioSource.Play();

            // Wait for the audio to complete if not looping
            if (!_loop)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_clip.length),
                    cancellationToken: cancellationToken);
            }

            Mode = ActivityMode.Active;
        }

        /**
         * <summary>
         * Deactivates by stopping audio playback.
         * </summary>
         * <param name="cancellationToken">Token to cancel the deactivation.</param>
         */
        public override async UniTask DeactivateAsync(CancellationToken cancellationToken)
        {
            if (Mode == ActivityMode.Inactive) return;

            Mode = ActivityMode.Deactivating;

            // Stop the audio
            _audioSource.Stop();

            await UniTask.CompletedTask;

            Mode = ActivityMode.Inactive;
        }
    }
}




