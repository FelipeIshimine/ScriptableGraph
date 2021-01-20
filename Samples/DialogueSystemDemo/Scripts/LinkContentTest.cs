using UnityEngine;

namespace Ishimine.ScriptableGraph.Editor
{
    public class LinkContentTest : ScriptableObject
    {
        public Sprite sprite;
        [TextArea] public string body;
        public int value;
    }
}