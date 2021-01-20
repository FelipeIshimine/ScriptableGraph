using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ishimine.ScriptableGraph
{
    [Serializable]
    public class ContentNodeData
    {
        public string GUID;
        public ScriptableObject Content;
        public Vector2 Position;
    }
}