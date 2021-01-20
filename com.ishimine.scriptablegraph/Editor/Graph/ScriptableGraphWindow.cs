using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Ishimine.ScriptableGraph;

namespace Ishimine.ScriptableGraph.Editor
{
    public abstract class ScriptableGraphWindow : EditorWindow
    {
        protected ScriptableGraphView _graphView;
        
        protected TextField filePath;

        protected virtual void ConstructGraphView()
        {
            _graphView = new ScriptableGraphView(this)
            {
                name = "ScriptableGraphWindow",
            };
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        protected virtual Toolbar GenerateToolbar()
        {
            var toolbar = new Toolbar();

            if(filePath == null)
                filePath = new TextField();

            toolbar.Add(new Button(() => RequestDataOperation(DataOperation.Load)) { text = "Load" });
            toolbar.Add(new Button(() => RequestDataOperation(DataOperation.SaveAs)) { text = "Save As..."});
            toolbar.Add(new Button(() => RequestDataOperation(DataOperation.Save)) { text = "Quick Save" });

            rootVisualElement.Add(toolbar);

            
            filePath.style.minWidth = new StyleLength(StyleKeyword.Auto);
            filePath.SetEnabled(false);
            toolbar.Add(filePath);


            return toolbar;
        }

        private enum DataOperation { Save, Load, SaveAs}
        private void RequestDataOperation(DataOperation dataOperation)
        {
            string startPath = "Assets";
            if (filePath != null && !string.IsNullOrEmpty(filePath.text))
                startPath = filePath.text;
            string path;

            switch (dataOperation)
            {
                case DataOperation.Save:
                    if (filePath == null || string.IsNullOrEmpty(filePath.text))
                    {
                        EditorUtility.DisplayDialog("No file selected", "Please select a file before saving", "OK");
                        return;
                    }
                    path = filePath.text;
                    break;
                case DataOperation.Load:
                    path = EditorUtility.OpenFilePanel("Open File", "New ScriptableGraph", "asset");
                    break;
                case DataOperation.SaveAs:
                    path = EditorUtility.SaveFilePanel("Save File", startPath, "New ScriptableGraph", "asset");
                    break;
                default:
                    throw new Exception("Invalid case");
            }

            path = FullPathToRelativePath(path);
            if (!string.IsNullOrEmpty(path))
            {
                var saveUtility = GraphSaveUtility.GetInstance(_graphView);
                if (dataOperation == DataOperation.Load)
                    saveUtility.LoadNarrative(path);
                else
                    saveUtility.SaveGraph(path);

                if (filePath != null) filePath.SetValueWithoutNotify(path);
            }
        }

        public string FullPathToRelativePath(string absolutePath)
        {
            return absolutePath.StartsWith(Application.dataPath) ? "Assets" + absolutePath.Substring(Application.dataPath.Length) : absolutePath;
        }

        protected virtual void OnEnable()
        {
            ConstructGraphView();
            GenerateToolbar();
            GenerateMiniMap();
            //GenerateBlackBoard();
        }

        private void GenerateMiniMap()
        {
            var miniMap = new MiniMap {anchored = true};
            var cords = _graphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));
            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            _graphView.Add(miniMap);
        }

        private void GenerateBlackBoard()
        {
            var blackboard = new Blackboard(_graphView);
            blackboard.Add(new BlackboardSection {title = "Exposed Variables"});
            blackboard.addItemRequested = _blackboard =>
            {
                _graphView.AddPropertyToBlackBoard(ExposedProperty.CreateInstance(), false);
            };
            blackboard.editTextRequested = (_blackboard, element, newValue) =>
            {
                var oldPropertyName = ((BlackboardField) element).text;
                if (_graphView.ExposedProperties.Any(x => x.PropertyName == newValue))
                {
                    EditorUtility.DisplayDialog("Error", "This property name already exists, please chose another one.",
                        "OK");
                    return;
                }

                var targetIndex = _graphView.ExposedProperties.FindIndex(x => x.PropertyName == oldPropertyName);
                _graphView.ExposedProperties[targetIndex].PropertyName = newValue;
                ((BlackboardField) element).text = newValue;
            };
            blackboard.SetPosition(new Rect(10,30,200,300));
            _graphView.Add(blackboard);
            _graphView.Blackboard = blackboard;
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(_graphView);
        }
    }
}