using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using unity4dv;
using UnityEngine.Playables;

namespace unity4dv
{
    [TrackColor(1,1,1)]
    [TrackBindingType(typeof(Plugin4DS))]
    [TrackClipType(typeof(Playable4DS))]
    public class Track4DS : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            Plugin4DS plugin = go.GetComponent<PlayableDirector>().GetGenericBinding(this) as Plugin4DS;
            // When creating a track for the first time, the instance of Plugin4DS that is supposed to be played on said
            // track has not been initialized yet, thus the following line gives incorrect result, i.e. Framerate is equal
            // to 0 and SpeedRatio to 1.
            float fps = plugin.Framerate * plugin.SpeedRatio;

            foreach (var clip in GetClips())
            {
                Playable4DS myAsset = clip.asset as Playable4DS;
                if (myAsset)
                {
                    myAsset.clipStartTime = clip.start;
                    myAsset.clipEndTime = clip.end;

                    if (myAsset.sequence4DS.lastFrame == -1)
                    {
                        myAsset.sequence4DS.firstFrame = plugin.FirstActiveFrame;
                        myAsset.sequence4DS.lastFrame = plugin.LastActiveFrame;

                        if (fps == 0) continue;

                        if (plugin.ActiveNbOfFrames > 0)
                            clip.duration = plugin.ActiveNbOfFrames / fps;
                        else
                            clip.duration = plugin.SequenceNbOfFrames / fps;
                    }
                }
            }

            return base.CreateTrackMixer(graph, go, inputCount);
        }
    }
}
