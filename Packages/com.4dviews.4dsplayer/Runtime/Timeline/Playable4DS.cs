using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace unity4dv
{
    [Serializable]
    public class Playable4DS : PlayableAsset, ITimelineClipAsset
    {
        internal double clipStartTime;
        internal double clipEndTime;

        [SerializeField]
        public TimelineBehaviour4DS sequence4DS = new TimelineBehaviour4DS();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            sequence4DS.SetPlayableInfos(owner,clipStartTime);
            
            return ScriptPlayable<TimelineBehaviour4DS>.Create(graph, sequence4DS);
        }
    }
}
