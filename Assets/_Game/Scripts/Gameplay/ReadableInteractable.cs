using UnityEngine;

namespace Jamie.Gameplay
{
    [RequireComponent(typeof(NoteItem))]
    [DisallowMultipleComponent]
    public sealed class ReadableInteractable : WorldInteractable
    {
        [SerializeField] private string displayName = "note";
        [SerializeField] private bool requireFlashlight;
        [SerializeField] private bool collectAfterReading = true;

        private NoteItem note;

        public override string Prompt =>
            $"E - Read {displayName}";

        public override bool RequiresFlashlight =>
            requireFlashlight;

        private void Awake()
        {
            note = GetComponent<NoteItem>();
        }

        private void OnEnable()
        {
            if (note != null)
                note.onFinishedReading.AddListener(FinishedReading);
        }

        private void OnDisable()
        {
            if (note != null)
                note.onFinishedReading.RemoveListener(FinishedReading);
        }

        public override bool CanInteract(WorldInteractor interactor)
        {
            return note != null &&
                NoteManager.Instance != null &&
                NoteManager.Instance.isActiveAndEnabled &&
                !NoteManager.Instance.IsOpen;
        }

        public override void Interact(WorldInteractor interactor)
        {
            if (CanInteract(interactor))
                NoteManager.Instance.OpenNote(note);
        }

        private void FinishedReading()
        {
            if (!collectAfterReading)
                return;

            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}