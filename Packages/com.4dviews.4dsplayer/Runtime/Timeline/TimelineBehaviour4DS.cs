using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using unity4dv;

namespace unity4dv
{
    [Serializable]
    public class TimelineBehaviour4DS : PlayableBehaviour
    {
        [SerializeField]
        public int firstFrame = 0;
        [SerializeField]
        public int lastFrame = -1;

        private GameObject owner;
        private double clipstart;

        Plugin4DS Plugin;

        private PlayableDirector _director;
        private AudioSource _audio4ds;

        bool onstart = false;

        public override void OnPlayableCreate(Playable Playable)
        {
            _director = owner.GetComponent<PlayableDirector>();
            _director.played  += OnPlayableDirectorPlayed;
            _director.paused  += OnPlayableDirectorPausedOrStopped;
            _director.stopped += OnPlayableDirectorPausedOrStopped;

            _audio4ds = owner.GetComponentInChildren<AudioSource>();
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            Plugin = playerData as Plugin4DS;
            
            if (Plugin == null) return;
            if (Plugin.Framerate == 0) Plugin.Initialize();

            float duration = (float)(playable.GetDuration() * Plugin.Framerate);
            float clipSpeed = 1;
            if (firstFrame < lastFrame && firstFrame >= 0){
                clipSpeed = (lastFrame - firstFrame) / duration;
            }
            else{
                clipSpeed = (Plugin.SequenceNbOfFrames - firstFrame) / duration;
            }
            Plugin.SpeedRatio = clipSpeed;

            int frameid = firstFrame + (int) (playable.GetTime() * Plugin.Framerate * clipSpeed);

            if (Application.isPlaying) {
                if (onstart)
                {
                    // Application running.
                    if (_audio4ds is not null) { 
                        _audio4ds.time  = (float) (_director.time - clipstart);
                        _audio4ds.pitch = clipSpeed;
                    }
                    
                    Plugin.GoToFrame(firstFrame);
                    Plugin.Play(true);
                    onstart = false;
                }
            } else {
                Plugin.FromTimeline = true;
                
                // Seek or animation preview.
                Plugin.PreviewFrame = frameid;
                Plugin.Preview();

                if (_audio4ds != null && _director.state == PlayState.Playing && _director.time > clipstart)
                {
                    _audio4ds.time  = (float)(_director.time - clipstart) * clipSpeed;
                    _audio4ds.pitch = clipSpeed;
                    _audio4ds.Play();
                }
            }

            base.ProcessFrame(playable, info, playerData);
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            onstart = true;
            base.OnBehaviourPlay(playable, info);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            //        if (Plugin)
            //Plugin.Play(false);
        }

        private void OnPlayableDirectorPlayed(PlayableDirector aDirector)
        {
        }

        private void OnPlayableDirectorPausedOrStopped(PlayableDirector aDirector)
        {
            if (_director == aDirector)
            {
                if (_audio4ds != null) {
                    _audio4ds.Stop();
                }
            }
        }

        public void SetPlayableInfos(GameObject owner, double clipstarttime)
        {
            this.owner = owner;
            clipstart = clipstarttime;
        }
    }
}
