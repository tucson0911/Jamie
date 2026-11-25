using Jamie.Gameplay;
using UnityEngine;
using UnityEngine.Events;

public class NoteItem : MonoBehaviour
{
    [Header("Journal")]
    public JournalEntry journalEntry;

    [Header("Content used without a journal entry")]
    [TextArea(3, 10)]
    public string noteTitle = "A Dusty Note";

    [TextArea(5, 15)]
    public string noteBody;

    public Sprite noteImage;
    public float noteImageWidth;
    public float noteImageHeight;

    [Header("After Reading")]
    public UnityEvent onFinishedReading = new UnityEvent();

    public event System.Action ReadingCompleted;

    public void FinishReading()
    {
        ReadingCompleted?.Invoke();
        onFinishedReading?.Invoke();
    }

    public string ContentTitle =>
        journalEntry != null ? journalEntry.Title : noteTitle;

    public string ContentBody =>
        journalEntry != null ? journalEntry.Body : noteBody;

    public Sprite ContentImage =>
        journalEntry != null ? journalEntry.Image : noteImage;
}