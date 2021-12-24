using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ishimine.ScriptableGraph
{
    [Serializable]
    public class GraphContainer : ScriptableObject
    {
        public List<ContentNodeData> dialogueNodeData = new List<ContentNodeData>();
        public List<ContentLinkData> nodeLinks = new List<ContentLinkData>();

        public List<ExposedProperty> exposedProperties = new List<ExposedProperty>();
        public List<CommentBlockData> commentBlockData = new List<CommentBlockData>();

        public ContentNodeData GetFirstNode() => dialogueNodeData[0];

        public IEnumerable<ContentLinkData> GeLinksData(string guid) => nodeLinks.Where(x => x.BaseNodeGUID == guid);

        public ContentNodeData GetNodeData(string guid)=> dialogueNodeData.Find(x => x.GUID == guid);

        public NavigationGraph<TNode,TEdge> CreateNavigationGraph<TNode,TEdge>() where TNode:ScriptableObject where TEdge:ScriptableObject
        => new NavigationGraph<TNode,TEdge>(this);
    }

    public class NavigationGraph<TNode,TEdge> where TNode:ScriptableObject where TEdge:ScriptableObject
    {
        private readonly Dictionary<string, TNode> _nodes = new Dictionary<string, TNode>();
        private readonly Dictionary<string, TEdge> _edges = new Dictionary<string, TEdge>();

        internal NavigationGraph(GraphContainer graphContainer)
        {
          
        }
    }
}


[System.Serializable]
public class ContentPair
{
    private string ownerGUID;
    public ScriptableObject Content;

    public string OwnerGUID { get => ownerGUID; set => ownerGUID = value; }

    public ContentPair(string ownerGUID, ScriptableObject content)
    {
        OwnerGUID = ownerGUID;
        Content = content;
    }
}
