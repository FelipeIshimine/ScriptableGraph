using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Ishimine.ScriptableGraph.Editor
{
    public class ContentNode : Node
    {
        public string GUID;
        public ScriptableObject Content;
        public List<ContentPair> LinksContent = new List<ContentPair>(); 
        public bool EntryPoint = false;
    }
}