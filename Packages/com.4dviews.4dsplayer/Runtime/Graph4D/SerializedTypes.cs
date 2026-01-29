using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace unity4dv
{
    [Serializable]
    public class SerializedEntryNode
    {
        public Vector2 Position;
        public string LinkedNodeGuid;
    }

    [Serializable]
    public class SerializedNode4D
    {
        public string NodeGUID;
        public Vector2 Position;
        public bool Collapsed;

        public GameObject Sequence4D;
        public GameObject LoopSequence4D;
        public bool IsLooping;
        public List<SerializedTransition4D> Transitions4D = new List<SerializedTransition4D>();
    }

    [Serializable]
    public class SerializedTransition4D
    {
        public bool Opened;
        public string LinkedNodeGuid;
        public string TransitionName;
        public string TriggerName;
        public GameObject TransitionSequence4D;
    }

    [Serializable]
    public class SerializedCommentBlock
    {
        public List<string> ChildNodes = new List<string>();
        public Vector2 Position;
        public string Title = "Comment Block";
    }
}
