using UnityEngine;

namespace Ishimine.ScriptableGraph.Editor
{
    public class NoteContentTest : ScriptableObject
    {
        public string head;
        [TextArea] public string body;
        public float percentage;
        public int value;
        public Vector2 vector;
        public Sprite sprite;
    }
}