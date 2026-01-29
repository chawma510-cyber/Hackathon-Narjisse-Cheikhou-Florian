using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace unity4dv
{
    [Serializable]
    public class Graph4DContainer : ScriptableObject
    {
		// Graph content data
        public SerializedEntryNode EntryNode = new SerializedEntryNode();
        public List<SerializedNode4D> Nodes = new List<SerializedNode4D>();
        public List<SerializedCommentBlock> CommentBlock = new List<SerializedCommentBlock>();

        // View transform data
        public Vector3 Position = new Vector3(100.0f, 100.0f);
        public Vector3 Scale = new Vector3(1.0f, 1.0f, 1.0f);
    }
}
