using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ishimine.ScriptableGraph.Editor
{
    public abstract class GenericScriptableGraphWindow<TNode,TLink> : ScriptableGraphWindow where TNode : ScriptableObject where TLink : ScriptableObject
    {
        protected override Toolbar GenerateToolbar()
        {
            var toolbar = base.GenerateToolbar();

            toolbar.Add(new Button(() =>
            {
                _graphView.CreateNewNode("Dialogue Node", _graphView.contentViewContainer.WorldToLocal((Vector2)_graphView.transform.position + _graphView.contentRect.size/2));
            }
            ){text = "New Node"});
            
            return toolbar;
        }
        protected override void ConstructGraphView()
        {
            base.ConstructGraphView();
            _graphView.OnCreateNewNodeContent = CreateInstance<TNode>;
            _graphView.OnCreateNewLinkContent = CreateInstance<TLink>;
        }
    }
}