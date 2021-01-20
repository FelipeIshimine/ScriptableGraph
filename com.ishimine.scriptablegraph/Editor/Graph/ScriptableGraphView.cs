using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ishimine.ScriptableGraph;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

namespace Ishimine.ScriptableGraph.Editor
{
    public class ScriptableGraphView : GraphView 
    {
        public readonly Vector2 DefaultNodeSize = new Vector2(200, 150);
        public readonly Vector2 DefaultCommentBlockSize = new Vector2(300, 200);
        public ContentNode EntryPointNode;
        public Blackboard Blackboard = new Blackboard();
        public List<ExposedProperty> ExposedProperties { get; private set; } = new List<ExposedProperty>();
        private NodeSearchWindow _searchWindow;

        public Func<ScriptableObject> OnCreateNewNodeContent;
        public Func<ScriptableObject> OnCreateNewLinkContent;
        EditorWindow _window;

        private Vector2 _lastMousePosition;

        public ScriptableGraphView(ScriptableGraphWindow editorWindow)
        {
            _window = editorWindow;
            styleSheets.Add(Resources.Load<StyleSheet>("NarrativeGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            this.RegisterCallback<MouseMoveEvent>(OnMouseMove);

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            AddElement(GetEntryPointNodeInstance());

            //AddSearchWindow(editorWindow);
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _lastMousePosition = evt.mousePosition;
        }


        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 graphMousePosition = GetMousePosition();

            if (evt.target is GraphView || evt.target is Node)
            {
                evt.menu.AppendAction("Content Node", (e) => CreateNewNode("Content Node", graphMousePosition));
                evt.menu.AppendAction("Comment Node", (e) => CreateCommentBlock(new Rect(graphMousePosition, DefaultCommentBlockSize)));
            }

            base.BuildContextualMenu(evt);

        }

        public Vector2 GetMousePosition()
        {
            var mousePosition = _window.rootVisualElement.ChangeCoordinatesTo(_window.rootVisualElement.parent,
                            _lastMousePosition - _window.position.position);
            var graphMousePosition = contentViewContainer.WorldToLocal(mousePosition);
            return graphMousePosition;
        }

        private void AddSearchWindow(ScriptableGraphWindow editorWindow)
        {
            _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            _searchWindow.Configure(editorWindow, this);
            nodeCreationRequest = context =>
                SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), _searchWindow);
        }
        

        public void ClearBlackBoardAndExposedProperties()
        {
            ExposedProperties.Clear();
            Blackboard.Clear();
        }

        public Group CreateCommentBlock(Rect rect, CommentBlockData commentBlockData = null)
        {
            if(commentBlockData==null)
                commentBlockData = new CommentBlockData();
            var group = new Group
            {
                autoUpdateGeometry = true,
                title = commentBlockData.Title
            };
            AddElement(group);
            group.SetPosition(rect);
            return group;
        }

        public void AddPropertyToBlackBoard(ExposedProperty property, bool loadMode = false)
        {
            var localPropertyName = property.PropertyName;
            var localPropertyValue = property.PropertyValue;
            if (!loadMode)
            {
                while (ExposedProperties.Any(x => x.PropertyName == localPropertyName))
                    localPropertyName = $"{localPropertyName}(1)";
            }

            var item = ExposedProperty.CreateInstance();
            item.PropertyName = localPropertyName;
            item.PropertyValue = localPropertyValue;
            ExposedProperties.Add(item);

            var container = new VisualElement();
            var field = new BlackboardField {text = localPropertyName, typeText = "string"};
            container.Add(field);

            var propertyValueTextField = new TextField("Value:")
            {
                value = localPropertyValue
            };
            propertyValueTextField.RegisterValueChangedCallback(evt =>
            {
                var index = ExposedProperties.FindIndex(x => x.PropertyName == item.PropertyName);
                ExposedProperties[index].PropertyValue = evt.newValue;
            });
            var sa = new BlackboardRow(field, propertyValueTextField);
            container.Add(sa);
            Blackboard.Add(container);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            var compatiblePorts = new List<Port>();
            var startPortView = startPort;

            ports.ForEach((port) =>
            {
                var portView = port;
                if (startPortView != portView && startPortView.node != portView.node)
                    compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        public  void CreateNewNode(string nodeName, Vector2 position) => AddElement(CreateNode(nodeName, position));

        public  ContentNode CreateNode(string nodeName, Vector2 position) => _CreateNode(nodeName, position);
        public ContentNode CreateNode(string nodeName, Vector2 position, ScriptableObject nodeContent) => _CreateNode(nodeName, position, nodeContent);
        public ContentNode _CreateNode(string nodeName, Vector2 position, ScriptableObject nodeContent = null)
        {
            var tempDialogueNode = new ContentNode()
            {
                title = nodeName,
                Content = nodeContent? Object.Instantiate(nodeContent):OnCreateNewNodeContent?.Invoke(),
                GUID = Guid.NewGuid().ToString()
            };
            var inputPort = GetPortInstance(tempDialogueNode, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            tempDialogueNode.inputContainer.Add(inputPort);
            tempDialogueNode.RefreshExpandedState();
            tempDialogueNode.RefreshPorts();
            tempDialogueNode.SetPosition(new Rect(position,
                DefaultNodeSize)); //To-Do: implement screen center instantiation positioning

            var textField = new TextField("");
            textField.RegisterValueChangedCallback(evt => { tempDialogueNode.title = evt.newValue; });

            if (tempDialogueNode.Content is INodeTitle nodeTitle && nodeTitle.GetTitle() != string.Empty)
            {
                tempDialogueNode.title = textField.value = nodeTitle.GetTitle();
                textField.SetEnabled(false);
                tempDialogueNode.RegisterCallback<MouseMoveEvent>(x =>
                {
                    tempDialogueNode.title = textField.value = nodeTitle.GetTitle();
                    textField.SetEnabled(false);
                });
            }
            else
            {
                textField.value = tempDialogueNode.title;
                textField.SetEnabled(true);
            }

            IMGUIContainer imgGUI = RenderContent(tempDialogueNode.Content);
            tempDialogueNode.inputContainer.Add(textField);
            tempDialogueNode.inputContainer.Add(imgGUI);

            if (tempDialogueNode.Content is INodeColor useColor)
            {
                tempDialogueNode.titleContainer.style.backgroundColor = useColor.GetColor();
                tempDialogueNode.RegisterCallback<MouseMoveEvent>(x =>
                {
                    tempDialogueNode.titleContainer.style.backgroundColor = useColor.GetColor();
                });
            }
            else
                tempDialogueNode.titleContainer.style.backgroundColor = new Color(29/255f,80/255f,115/255f);

            var button = new Button(() => 
            {
                AddChoicePort(tempDialogueNode);
            })
            {
                text = "Add Choice"

            };
            tempDialogueNode.titleButtonContainer.Add(button);
            return tempDialogueNode;
        }

        private static IMGUIContainer RenderContent(ScriptableObject content)
        {
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(content);
            IMGUIContainer imgGUI = new IMGUIContainer() { name = "IMGUIContainer" };
            imgGUI.onGUIHandler = () => editor.OnInspectorGUI();
            return imgGUI;
        }

        private static string GuidFromAsset(Object value)=> AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));

        public void AddChoicePort(ContentNode nodeCache, string overriddenPortName = "", ScriptableObject content = null)
        {
            Port generatedPort = GetPortInstance(nodeCache, Direction.Output);
            generatedPort.style.marginBottom = 7;
            generatedPort.style.paddingBottom = 7;
            generatedPort.style.borderBottomColor = new StyleColor(new Color(.1f, .1f, .1f));
            generatedPort.style.borderBottomWidth = 1;
            var portLabel = generatedPort.contentContainer.Q<Label>("type");
            var connector = generatedPort.contentContainer.Q<VisualElement>("connector");
            //generatedPort.Remove(connector);
            generatedPort.contentContainer.Remove(portLabel);

            generatedPort.style.height = new StyleLength(StyleKeyword.Auto);

            var mainContainer = new VisualElement();
            mainContainer.style.height = new StyleLength(StyleKeyword.Auto);
            mainContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);

            var upperContainer = new VisualElement();
            upperContainer.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);

            var textField = new TextField()
            {
                name = string.Empty,
                value = string.IsNullOrEmpty(overriddenPortName)?$"Option {nodeCache.outputContainer.Query("connector").ToList().Count().ToString()}": overriddenPortName
            };
            generatedPort.portName = textField.value;
            textField.RegisterValueChangedCallback(x =>
            {
                textField.value = x.newValue;
                generatedPort.portName = textField.value;
            });

            textField.style.flexGrow = new StyleFloat(StyleKeyword.Auto);

            if (!content) content = OnCreateNewLinkContent?.Invoke();
            ContentPair contentPair = new ContentPair(nodeCache.GUID, content);
            var deleteButton = new Button(
                () =>
                {
                    nodeCache.LinksContent.Remove(contentPair);
                    RemovePort(nodeCache, generatedPort);
                }
                )
            {
                text = "X"
            };

            upperContainer.Add(deleteButton);
            upperContainer.Add(textField);

            mainContainer.Add(upperContainer);

            nodeCache.LinksContent.Add(contentPair);
            mainContainer.Add(RenderContent(content));

            generatedPort.contentContainer.Add(mainContainer);

            nodeCache.outputContainer.Add(generatedPort);
            nodeCache.RefreshPorts();
            nodeCache.RefreshExpandedState();
        }

        private void RemovePort(Node node, Port socket)
        {
            var targetEdge = edges.ToList()
                .Where(x => x.output.portName == socket.portName && x.output.node == socket.node);
            if (targetEdge.Any())
            {
                var edge = targetEdge.First();
                edge.input.Disconnect(edge);
                RemoveElement(targetEdge.First());
            }

            node.outputContainer.Remove(socket);
            node.RefreshPorts();
            node.RefreshExpandedState();
        }

        private Port GetPortInstance(ContentNode node, Direction nodeDirection,
            Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, nodeDirection, capacity, typeof(float));
        }

        private ContentNode GetEntryPointNodeInstance()
        {
            var nodeCache = new ContentNode()
            {
                title = "START",
                GUID = Guid.NewGuid().ToString(),
                EntryPoint = true
            };

            var generatedPort = GetPortInstance(nodeCache, Direction.Output);
            generatedPort.portName = "Next";
            nodeCache.outputContainer.Add(generatedPort);

            nodeCache.capabilities &= ~Capabilities.Movable;
            nodeCache.capabilities &= ~Capabilities.Deletable;

            nodeCache.RefreshExpandedState();
            nodeCache.RefreshPorts();
            nodeCache.SetPosition(new Rect(100, 200, 100, 150));
            return nodeCache;
        }
    }
}
public interface INodeColor
{
    Color GetColor();
}

public interface INodeTitle
{
    string GetTitle();
}