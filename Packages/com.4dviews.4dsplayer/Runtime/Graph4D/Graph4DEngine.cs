using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace unity4dv
{
    public class EngineTransition {
        public EngineState nextState;
        public EngineSequence4D seq4D;
        public string transitionName;

        public string triggerName;
        public bool isTriggered = false;
    }

    public class EngineState {
        public List<EngineTransition> transitions;
        public EngineSequence4D seq4D;
        public string stateName;

        public bool isLooping;
    }

    public class Graph4DEngine {
        Graph4D _reader;
        EngineState _currentState;

        bool _running = false;
        int _firstStateId = -1;
        List<EngineState> _states;
        List<EngineTransition> _transitions;

        HashSet<EngineSequence4D> _lastBufferedSequences;

        public bool IsRunning { get { return _running; } }

        public Graph4DEngine(Graph4D reader,
                SerializedEntryNode entryNode,
                List<SerializedNode4D> nodes)
        {
            _reader = reader;
            buildGraph(entryNode, nodes);

            if (_firstStateId < 0)
            {
                Debug.Log("Warning : the starting node of the graph is not connected");
                return;
            }

            _currentState = _states[_firstStateId];
            updateBuffering();
        }

        public IEnumerator Start()
        {
            _running = (_firstStateId >= 0); 

            _reader.OnGraph4DStart.Invoke();
            while (_running)
            {
                playCurrentState();
                yield return _currentState.seq4D.WaitEnd();
                stopCurrentState();

                var transition = chooseTransition();
                if (transition != null)
                {
                    if (transition.seq4D.gameObject)
                    {
                        playTransition(transition);
                        yield return transition.seq4D.WaitEnd();
                        stopTransition(transition);
                    }

                    _currentState = transition.nextState;
                    updateBuffering();
                }
                else if (checkLoop())
                    continue;
                else
                    _running = false;
            }

            _reader.OnGraph4DStop.Invoke();
        }

        public void Reset()
        {
            _running = false;
            _states.ForEach(s => s.seq4D.Reset());
            _transitions.ForEach(t => t.seq4D.Reset());
        }

        public void SetTrigger(string name, bool value)
        {
            bool found = false;
            foreach (var t in _transitions)
            {
                if (t.triggerName == name) {
                    t.isTriggered = value;
                    found = true;
                }
            }
            if (!found)
                Debug.Log("Graph4DEngine : Trigger not found : " + name);
        }

        private EngineTransition chooseTransition() {
            foreach (var t in _currentState.transitions) {
                if (t.isTriggered && t.triggerName != "") {
                    t.isTriggered = false;
                    return t;
                }
            }
            foreach (var t in _currentState.transitions) {
                if (t.triggerName == "")
                    return t;
            }
            return null;
        }

        private bool checkLoop() {
            return _currentState.isLooping;
        }

        private void playCurrentState() {
            _currentState.seq4D.Play();
            _reader.OnSequence4DStart.Invoke(_currentState.seq4D.gameObject);
        }

        private void stopCurrentState() {
            _currentState.seq4D.Reset();
            _reader.OnSequence4DStop.Invoke(_currentState.seq4D.gameObject);
        }

        private void playTransition(EngineTransition transition) {
            transition.seq4D.Play();
            _reader.OnSequence4DStart.Invoke(transition.seq4D.gameObject);
        }

        private void stopTransition(EngineTransition transition) {
            transition.seq4D.Reset();
            _reader.OnSequence4DStop.Invoke(transition.seq4D.gameObject);
        }

        private void updateBuffering() {
            var nextSequences = nextPossibleSequences(_currentState, 2);

            // stop buffering OLD\NEW
            if (_lastBufferedSequences != null) {
                _lastBufferedSequences.ExceptWith(nextSequences);
                foreach (var s in _lastBufferedSequences) 
                    s.StopBuffering();
            }

            // start buffering NEW
            foreach (var s in nextSequences) 
                s.StartBuffering();

            _lastBufferedSequences = nextSequences;
        }

        private void buildGraph(SerializedEntryNode entryNode,
                List<SerializedNode4D> nodes) {
            _states = new List<EngineState>();
            _transitions = new List<EngineTransition>();

            var uidToState = new Dictionary<string, EngineState>();

            // NODES
            foreach (var node in nodes) {
                var newState = new EngineState() {
                    isLooping = node.IsLooping,
                    seq4D = new EngineSequence4D(node.Sequence4D, node.IsLooping),
                    transitions = new List<EngineTransition>(),
                };

                uidToState.Add(node.NodeGUID, newState);
                _states.Add(newState);
            }

            if (entryNode.LinkedNodeGuid != "")
                _firstStateId = _states.IndexOf(uidToState[entryNode.LinkedNodeGuid]);

            // EDGES
            foreach (var node in nodes) {
                foreach (var transition in node.Transitions4D) {
                    if (transition.LinkedNodeGuid == "") continue;

                    var t = new EngineTransition() {
                        nextState = uidToState[transition.LinkedNodeGuid],
                        seq4D = new EngineSequence4D(transition.TransitionSequence4D, false),
                        triggerName = transition.TriggerName,
                    };

                    var precState = uidToState[node.NodeGUID];
                    precState.transitions.Add(t);
                    _transitions.Add(t);
                }
            }

            // Create transition sequence for looping nodes
            foreach (var node in nodes) {
                if (node.IsLooping && (node.LoopSequence4D))
                {
                    var t = new EngineTransition() {
                        nextState = uidToState[node.NodeGUID],
                        seq4D = new EngineSequence4D(node.LoopSequence4D, false),
                        triggerName = "",
                    };

                    var engineNode = uidToState[node.NodeGUID];
                    engineNode.transitions.Add(t);
                    _transitions.Add(t);
                }
            }

            _states.ForEach(s => s.seq4D.Reset());
            _transitions.ForEach(s => s.seq4D.Reset());
        }

        private HashSet<EngineSequence4D> nextPossibleSequences(EngineState state, int breadthMax)
        {
            var found = new HashSet<EngineSequence4D>();

            var toSee = new Queue<Tuple<EngineTransition, int> >();
            foreach (var t in state.transitions)
                toSee.Enqueue(new Tuple<EngineTransition, int>(t, 1));

            while (toSee.Count != 0) {
                var p = toSee.Dequeue();
                found.Add(p.Item1.seq4D);
                found.Add(p.Item1.nextState.seq4D);

                if (p.Item2 < breadthMax) {
                    var newState = p.Item1.nextState;
                    foreach (var t in newState.transitions) {
                        if (!found.Contains(t.nextState.seq4D) && !found.Contains(t.seq4D))
                            toSee.Enqueue(new Tuple<EngineTransition, int>(t, p.Item2 + 1));
                    }
                }
            }
            return found;
        }
    }

}
