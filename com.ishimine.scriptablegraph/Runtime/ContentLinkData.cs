using System;
using System.Linq;
using UnityEngine;

namespace Ishimine.ScriptableGraph
{
    [Serializable]
    public class ContentLinkData
    {
        public string BaseNodeGUID;
        public string PortName;
        public string TargetNodeGUID;
        public ScriptableObject Content;
    }
}