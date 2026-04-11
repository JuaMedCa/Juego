using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNote", menuName = "Notes/Note")]
public class NoteData : ScriptableObject
{
    public string title;

    [TextArea(5, 10)]
    public string noteText;
}
