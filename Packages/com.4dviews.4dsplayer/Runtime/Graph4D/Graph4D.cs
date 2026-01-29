using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace unity4dv
{
    public class Graph4D : MonoBehaviour
    {
        [SerializeField]
        public Graph4DContainer Container;

        [SerializeField]
        public bool PlayOnStart = true;

        private Graph4DEngine _engine;
        private Coroutine _coroutine;

#region Events
        public class Graph4DEvent : UnityEngine.Events.UnityEvent {}
        public class Graph4DEventGameObject : UnityEngine.Events.UnityEvent<GameObject> {}

        public Graph4DEvent OnGraph4DStart = new Graph4DEvent();
        public Graph4DEvent OnGraph4DStop = new Graph4DEvent();

        public Graph4DEventGameObject OnSequence4DStart = new Graph4DEventGameObject();
        public Graph4DEventGameObject OnSequence4DStop = new Graph4DEventGameObject();
#endregion

        // Start is called before the first frame update
        void Start()
        {
            _engine = new Graph4DEngine(this, Container.EntryNode, Container.Nodes);

            if (PlayOnStart)
                Play();
        }

        // Start the Graph execution
        public void Play()
        {
            _coroutine = StartCoroutine(_engine.Start());
        }

        // Stop the Graph execution
        public void Stop()
        {
            StopCoroutine(_coroutine);
            _engine.Reset();
        }

        public bool IsPlaying()
        {
            return (_engine is null) ? false : _engine.IsRunning;
        }

        // Set name as triggered
        public void SetTrigger(string name)
        {
            _engine.SetTrigger(name, true);
        }

        // Set name as untriggered
        public void UnsetTrigger(string name)
        {
            _engine.SetTrigger(name, false);
        }
    }

}
