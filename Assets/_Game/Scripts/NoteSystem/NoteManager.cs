using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject noteCanvasPanel = null;
    [SerializeField] private TextMeshProUGUI titleTextField = null;
    [SerializeField] private TextMeshProUGUI bodyTextField = null;
    [SerializeField] private Button closeButton = null;
    [SerializeField] private Image noteUIImage = null;

    private float timeScaleBeforeReading = 1f;

    public bool IsOpen { get; private set; }
    public NoteItem CurrentNote { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
            return;

        if (noteCanvasPanel != null)
            noteCanvasPanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseNote);
    }

    public void OpenNote(NoteItem note)
    {
        if (Instance != this || note == null || IsOpen)
            return;

        if (noteCanvasPanel == null)
        {
            Debug.LogError("Assign Note Canvas Panel on NoteManager.", this);
            return;
        }

        CurrentNote = note;
        IsOpen = true;
        timeScaleBeforeReading = Time.timeScale;

        if (titleTextField != null)
            titleTextField.text = note.noteTitle;

        if (bodyTextField != null)
            bodyTextField.text = note.noteBody;

        if (noteUIImage != null)
        {
            noteUIImage.gameObject.SetActive(note.noteImage != null);

            if (note.noteImage != null)
            {
                noteUIImage.sprite = note.noteImage;

                Vector2 size = noteUIImage.rectTransform.sizeDelta;
                if (note.noteImageWidth > 0f)
                    size.x = note.noteImageWidth;
                if (note.noteImageHeight > 0f)
                    size.y = note.noteImageHeight;
                noteUIImage.rectTransform.sizeDelta = size;
            }
        }

        noteCanvasPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void CloseNote()
    {
        if (Instance != this || !IsOpen)
            return;

        NoteItem finishedNote = CurrentNote;
        CurrentNote = null;
        IsOpen = false;

        if (noteCanvasPanel != null)
            noteCanvasPanel.SetActive(false);

        Time.timeScale = timeScaleBeforeReading;

        if (finishedNote != null)
            finishedNote.onFinishedReading?.Invoke();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseNote);

        if (Instance == this)
        {
            if (IsOpen)
                Time.timeScale = timeScaleBeforeReading;

            Instance = null;
        }
    }
}
