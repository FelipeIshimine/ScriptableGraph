using UnityEditor;
using UnityEngine;

namespace Ishimine.ScriptableGraph.Editor
{
    public class SO_GenericStoryGraph : GenericStoryGraph<NoteContentTest, LinkContentTest>
    {
        [MenuItem("Graph/Test Graph")]
        public static void CreateGraphViewWindow()
        {
            var window = GetWindow<SO_GenericStoryGraph>();
            window.titleContent = new GUIContent("SO_GenericStoryGraph");
        }
    }
}