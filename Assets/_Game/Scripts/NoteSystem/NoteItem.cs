using UnityEngine;
using UnityEngine.Events;

public class NoteItem : MonoBehaviour
{
    [TextArea(3, 10)]
    public string noteTitle = "A Dusty Note";
    [TextArea(5, 15)]
    [Tooltip("The actual text content written on the note.")]
    public string noteBody;

    public Sprite noteImage;
    public float noteImageWidth;
    public float noteImageHeight;

    [Header("After Reading")]
    public UnityEvent onFinishedReading = new UnityEvent();
}
