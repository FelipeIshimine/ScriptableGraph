using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Ishimine.ScriptableGraph;
using UnityEngine.UIElements;

namespace Ishimine.ScriptableGraph.Editor
{
    public class GraphSaveUtility
    {
        private List<Edge> Edges => _graphView.edges.ToList();
        private List<ContentNode> Nodes => _graphView.nodes.ToList().Cast<ContentNode>().ToList();

        private List<Group> CommentBlocks =>
            _graphView.graphElements.ToList().Where(x => x is Group).Cast<Group>().ToList();

        private GraphContainer _dialogueContainer;
        private ScriptableGraphView _graphView;

        public static GraphSaveUtility GetInstance(ScriptableGraphView graphView)
        {
            return new GraphSaveUtility
            {
                _graphView = graphView
            };
        }

        public void SaveGraph(string path)
        {
            var dialogueContainerObject = ScriptableObject.CreateInstance<GraphContainer>();
            AssetDatabase.CreateAsset(dialogueContainerObject, path);

            if (!SaveNodes(dialogueContainerObject)) return;
            SaveExposedProperties(dialogueContainerObject);
            SaveCommentBlocks(dialogueContainerObject);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private bool SaveNodes(GraphContainer dialogueContainerObject)
        {
            if (!Edges.Any()) return false;
            var connectedSockets = Edges;

            foreach (var node in Nodes) //Nodos
            {
                if (!node.EntryPoint) //Salteamos el nodo de entrada
                {
                    var nodeContent = UnityEngine.Object.Instantiate(node.Content);
                    nodeContent.name = node._title;
                    AssetDatabase.AddObjectToAsset(nodeContent, dialogueContainerObject);
                    //dialogueContainerObject.NodesContent.Add(new ContentPair(node.GUID, nodeContent));

                    dialogueContainerObject.dialogueNodeData.Add(new ContentNodeData
                    {
                        GUID = node.GUID,
                        Position = node.GetPosition().position,
                        Content= nodeContent
                    });
                }

                var ports = _graphView.ports.ToList().Where(x => x.node == node && x.direction == Direction.Output)
                    .ToList();

                //Edges
                HashSet<ContentPair> savedContent = new HashSet<ContentPair>();
                var edges = connectedSockets.Where(x => x.output.node == node).ToList();
                for (int i = 0; i < edges.Count; i++)
                {
                    ScriptableObject linkContent = null;
                    if (node.LinksContent.Count > 0)
                    {
                        ContentPair pair = node.LinksContent[i];
                        linkContent = UnityEngine.Object.Instantiate(pair.Content);
                        linkContent.name = i.ToString();
                        AssetDatabase.AddObjectToAsset(linkContent, dialogueContainerObject);
                        //dialogueContainerObject.LinksContent.Add(new ContentPair(node.GUID, linkContent));
                        savedContent.Add(node.LinksContent[i]);
                    }

                    var inputNode = (ContentNode)edges[i].input.node;


                    dialogueContainerObject.nodeLinks.Add(new ContentLinkData()
                    {
                        BaseNodeGUID= node.GUID,
                        PortName = edges[i].output.portName,
                        TargetNodeGUID = inputNode.GUID,
                        Content = linkContent
                    });

                }

                //Guardar Links sin destino
                for (var i = 0; i < node.LinksContent.Count; i++)
                {
                    ContentPair contentPair = node.LinksContent[i];

                    if (!savedContent.Contains(contentPair))
                    {
                        ScriptableObject linkContent = null;

                        ContentPair pair = node.LinksContent[i];
                        linkContent = UnityEngine.Object.Instantiate(pair.Content);
                        linkContent.name = i.ToString();
                        AssetDatabase.AddObjectToAsset(linkContent, dialogueContainerObject);
                        //dialogueContainerObject.LinksContent.Add(new ContentPair(node.GUID, linkContent));
                        savedContent.Add(node.LinksContent[i]);

                        Debug.Log(contentPair.Content.GetType());
                        dialogueContainerObject.nodeLinks.Add(new ContentLinkData()
                        {
                            BaseNodeGUID= node.GUID,
                            PortName = ports[i].portName,
                            TargetNodeGUID = null,
                            Content = linkContent
                        });
                    }
                }
            }
            
            return true;
        }

        private void SaveExposedProperties(GraphContainer dialogueContainer)
        {
            dialogueContainer.exposedProperties.Clear();
            dialogueContainer.exposedProperties.AddRange(_graphView.ExposedProperties);
        }

        private void SaveCommentBlocks(GraphContainer dialogueContainer)
        {
            foreach (var block in CommentBlocks)
            {
                var nodes = block.containedElements.Where(x => x is ContentNode).Cast<ContentNode>().Select(x => x.GUID)
                    .ToList();

                dialogueContainer.commentBlockData.Add(new CommentBlockData
                {
                    ChildNodes = nodes,
                    Title = block.title,
                    Position = block.GetPosition().position
                });
            }
        }

        public void LoadNarrative(string path)
        {
            _dialogueContainer = AssetDatabase.LoadAssetAtPath<GraphContainer>(path);
            if (_dialogueContainer == null)
            {
                EditorUtility.DisplayDialog($"File Not Found: {path}", "Target Narrative Data does not exist!", "OK");
                return;
            }

            ClearGraph();
            GenerateDialogueNodes();
            ConnectDialogueNodes();
            AddExposedProperties();
            GenerateCommentBlocks();
        }

        /// <summary>
        /// Set Entry point GUID then Get All Nodes, remove all and their edges. Leave only the entrypoint node. (Remove its edge too)
        /// </summary>
        private void ClearGraph()
        {
            Nodes.Find(x => x.EntryPoint).GUID = _dialogueContainer.nodeLinks[0].BaseNodeGUID;
            foreach (var perNode in Nodes)
            {
                if (perNode.EntryPoint) continue;
                Edges.Where(x => x.input.node == perNode).ToList()
                    .ForEach(edge => _graphView.RemoveElement(edge));
                _graphView.RemoveElement(perNode);
            }
        }

        /// <summary>
        /// Create All serialized nodes and assign their guid and dialogue text to them
        /// </summary>
        private void GenerateDialogueNodes()
        {
            foreach (var perNode in _dialogueContainer.dialogueNodeData)
            {
                var tempNode = _graphView.CreateNode(perNode.Content.name, Vector2.zero, ScriptableObject.Instantiate(perNode.Content));
                tempNode.GUID = perNode.GUID;
                _graphView.AddElement(tempNode);

                var nodePorts = _dialogueContainer.nodeLinks.Where(x => x.BaseNodeGUID == perNode.GUID).ToList();

                for (int i = 0; i < nodePorts.Count; i++)
                    _graphView.AddChoicePort(tempNode, nodePorts[i].PortName, ScriptableObject.Instantiate(nodePorts[i].Content));
            }
        }

        private void ConnectDialogueNodes()
        {
            for (var i = 0; i < Nodes.Count; i++)
            {
                var k = i; //Prevent access to modified closure
                var connections = _dialogueContainer.nodeLinks.Where(x => x.BaseNodeGUID == Nodes[k].GUID).ToList();
                for (var j = 0; j < connections.Count(); j++)
                {
                    var targetNodeGUID = connections[j].TargetNodeGUID;
                    var targetNode = Nodes.Find(x => x.GUID == targetNodeGUID);

                    if (targetNode == null)
                        continue;
                    LinkNodesTogether(Nodes[i].outputContainer[j].Q<Port>(), (Port) targetNode.inputContainer[0]);

                    targetNode.SetPosition(new Rect(
                        _dialogueContainer.dialogueNodeData.First(x => x.GUID == targetNodeGUID).Position,
                        _graphView.DefaultNodeSize));
                }
            }
        }

        private void LinkNodesTogether(Port outputSocket, Port inputSocket)
        {
            var tempEdge = new Edge()
            {
                output = outputSocket,
                input = inputSocket
            };
            tempEdge?.input.Connect(tempEdge);
            tempEdge?.output.Connect(tempEdge);
            _graphView.Add(tempEdge);
        }

        private void AddExposedProperties()
        {
            _graphView.ClearBlackBoardAndExposedProperties();
            foreach (var exposedProperty in _dialogueContainer.exposedProperties)
            {
                _graphView.AddPropertyToBlackBoard(exposedProperty);
            }
        }

        private void GenerateCommentBlocks()
        {
            foreach (var commentBlock in CommentBlocks)
            {
                _graphView.RemoveElement(commentBlock);
            }

            foreach (var commentBlockData in _dialogueContainer.commentBlockData)
            {
               var block = _graphView.CreateCommentBlock(new Rect(commentBlockData.Position, _graphView.DefaultCommentBlockSize),
                    commentBlockData);
               block.AddElements(Nodes.Where(x=>commentBlockData.ChildNodes.Contains(x.GUID)));
            }
        }
    }
}