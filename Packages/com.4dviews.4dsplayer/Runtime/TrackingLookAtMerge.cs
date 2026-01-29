using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using unity4dv;

namespace unity4dv
{
    [ExecuteInEditMode]
    public class TrackingLookAtMerge : MonoBehaviour
    {

        public GameObject TrackedPoint;

        private Plugin4DS _plugin;

        void Start() {
            InitScript();

            if (!_plugin)
                UnityEngine.Debug.Log("TrackingLookAtMerge : Tracked Point not setted");
        }

        void OnValidate() {
            InitScript();
        }

        // LateUpdate is called once per frame after normal updates
        void LateUpdate()
        {
            if (!_plugin) return;

            var tracked_transform = TrackedPoint.transform;
            _plugin.LookAtPoint(tracked_transform, transform);
        }

        private void InitScript() {
            if (TrackedPoint) {
                _plugin = TrackedPoint.GetComponentInParent<Plugin4DS>();
            
                if (!_plugin)
                    UnityEngine.Debug.LogError("TrackingLookAtMerge : Tracked Point need to be a child of a Mesh4D");
            } else 
                _plugin = null;
        }
    }
}
