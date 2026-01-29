using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace unity4dv {
    public class EngineSequence4D {
        public GameObject gameObject;

        private Plugin4DS _plugin;
        private bool _buffering = false;
        public bool _triggerLastFrame = false;
        private Vector3 _objectScale;

        public EngineSequence4D() {}
        public EngineSequence4D(GameObject go, bool isLooping) {
            gameObject = go;

            if (gameObject) {
                var tmpPlugin = gameObject.GetComponent(typeof(Plugin4DS));
                if (tmpPlugin is null)
                    tmpPlugin = gameObject.GetComponentInChildren(typeof(Plugin4DS));
                if (tmpPlugin is null)
                {
                    Debug.Log("No plugin assigned to object " + gameObject);
                    return;
                }

                gameObject.SetActive(true);
                _plugin = tmpPlugin as Plugin4DS;

                _plugin.AutoPlay = false;
                _plugin.Loop = isLooping;

                _plugin.Initialize();
                _plugin.OnLastFrame.AddListener((_) => _triggerLastFrame = true);

                _objectScale = gameObject.transform.localScale;
            }
        }

        public void Play() {
            if (_plugin is null) return;

            //            gameObject.transform.localScale = _objectScale;
            Display(true);
            gameObject.SetActive(true);
            _plugin.Play(true);
        }

        public bool IsPlaying() {
            if (_plugin is null) return false;
            return (_plugin.IsPlaying);
        }

        public void StartBuffering() {
            if (_plugin == null || _buffering) return;
            _buffering = true;
            _plugin.StartBuffering();
        }

        public void StopBuffering() {
            if (_plugin == null || !_buffering || _plugin.IsPlaying) return;
            _buffering = false;
            _plugin.StopBuffering();
        }

        public void Reset() {
            if (_plugin is null) return;

            // gameObject.transform.localScale = Vector3.zero;
            Display(false);
            _plugin.Play(false);
            _plugin.GoToFrame(_plugin.FirstActiveFrame);
            _buffering = false;
            //gameObject.SetActive(false);

        }

        public int CurrentFrame() {
            if (_plugin is null) return -1;
            return _plugin.CurrentFrame;
        }

        public IEnumerator WaitEnd() {
            _triggerLastFrame = false;
            yield return new WaitUntil(() => _triggerLastFrame /*|| !IsPlaying()*/ );
        }

        public void Display(bool show)
        {
            //if we just disable gameobject, it doesn't work well with lookAt (glitch at first frame)
            gameObject.GetComponent<MeshRenderer>().enabled = show;
            for (int i=0; i < gameObject.transform.childCount; i++) {
                Transform child = gameObject.transform.GetChild(i);
                Animator anim = child.GetComponent<Animator>();
                if (anim != null)
                    anim.enabled = show;
                child.gameObject.SetActive(show);

            }
        }

    }

}
