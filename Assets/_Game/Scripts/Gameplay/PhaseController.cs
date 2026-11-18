using System;
using UnityEngine;

namespace Jamie.Gameplay
{
    public enum HospitalPhase
    {
        First = 1,
        Second = 2,
        Third = 3
    }

    [DisallowMultipleComponent]
    public sealed class PhaseController : MonoBehaviour
    {
        [SerializeField]
        private HospitalPhase startingPhase = HospitalPhase.First;

        public HospitalPhase CurrentPhase { get; private set; }
            = HospitalPhase.First;

        public int DisplayNumber => 4 - (int)CurrentPhase;

        public event Action<HospitalPhase> PhaseChanged;

        private void Awake()
        {
            CurrentPhase = startingPhase;
        }

        private void Start()
        {
            AnnouncePhase();
        }

        [ContextMenu("Advance Phase (Play Mode)")]
        public void AdvancePhase()
        {
            if (!Application.isPlaying ||
                CurrentPhase == HospitalPhase.Third)
            {
                return;
            }

            CurrentPhase =
                (HospitalPhase)((int)CurrentPhase + 1);

            AnnouncePhase();
        }

        [ContextMenu("Reset Phase (Play Mode)")]
        public void ResetPhase()
        {
            if (!Application.isPlaying)
                return;

            CurrentPhase = startingPhase;
            AnnouncePhase();
        }

        private void AnnouncePhase()
        {
            Debug.Log(
                $"[Phase] Phase {(int)CurrentPhase}, " +
                $"display number {DisplayNumber}",
                this);

            PhaseChanged?.Invoke(CurrentPhase);
        }
    }
}