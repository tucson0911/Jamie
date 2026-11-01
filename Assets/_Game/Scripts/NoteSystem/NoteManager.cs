using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance;

    [Header("UI References")]
    [SerializeField] private GameObject noteCanvasPanel;
    [SerializeField] private TextMeshProUGUI titleTextField;
    [SerializeField] private TextMeshProUGUI bodyTextField;
    [SerializeField] private Button closeButton;
    [SerializeField] private UnityEngine.UI.Image noteUIImage;

    private void Awake()
    {
        // Simple Singleton pattern for easy access
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Ensure the note is closed at the start of the game
        CloseNote();

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseNote);
        }
    }

    public void OpenNote(NoteItem note)
    {
        if (titleTextField != null) titleTextField.text = note.noteTitle;
        if (bodyTextField != null) bodyTextField.text = note.noteBody;

        noteCanvasPanel.SetActive(true);

        // Pause the game or freeze player movement while reading
        Time.timeScale = 0f;

        if (noteUIImage != null)
        {
            if (note.noteImage != null)
            {
                noteUIImage.gameObject.SetActive(true);
                noteUIImage.sprite = note.noteImage; // Pulls the image straight from the NoteItem data
            }
            else
            {
                noteUIImage.gameObject.SetActive(false); // Hides the image slot if this note has none
            }
        }
    }

    public void CloseNote()
    {
        noteCanvasPanel.SetActive(false);

        // Resume game time
        Time.timeScale = 1f;
    }
}
