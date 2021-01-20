using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ishimine.ScriptableGraph
{
    [Serializable]
    public class GraphContainer : ScriptableObject
    {
        public List<ContentNodeData> DialogueNodeData = new List<ContentNodeData>();
        public List<ContentLinkData> NodeLinks = new List<ContentLinkData>();

        public List<ExposedProperty> ExposedProperties = new List<ExposedProperty>();
        public List<CommentBlockData> CommentBlockData = new List<CommentBlockData>();

        public List<ContentPair> NodesContent = new List<ContentPair>();
        public List<ContentPair> LinksContent = new List<ContentPair>();

        public ContentNodeData GetFirstNode() => DialogueNodeData[0];

        public IEnumerable<ContentLinkData> GeLinksData(string guid) => NodeLinks.Where(x => x.BaseNodeGUID == guid);

        public ContentNodeData GetNodeData(string guid)=> DialogueNodeData.Find(x => x.GUID == guid);
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

[System.Serializable]
public class LinkContentPair
{
    private string ownerGUID;
    public ScriptableObject Content;

    public string OwnerGUID { get => ownerGUID; set => ownerGUID = value; }

    public LinkContentPair(string ownerGUID, ScriptableObject content)
    {
        OwnerGUID = ownerGUID;
        Content = content;
    }
}