using UnityEditor;
using UnityEngine;

namespace Ishimine.ScriptableGraph.Editor
{
    public class SO_GenericStoryGraph : GenericScriptableGraphWindow<NoteContentTest, LinkContentTest>
    {
        [MenuItem("Graph/Test Graph")]
        public static void CreateGraphViewWindow()
        {
            var window = GetWindow<SO_GenericStoryGraph>();
            window.titleContent = new GUIContent("SO_GenericStoryGraph");
        }
    }

}