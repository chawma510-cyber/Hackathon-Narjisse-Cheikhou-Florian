//vision pro is so shitty it can't handle properly mesh data structures
#if !UNITY_VISIONOS
#define USE_JOB
#endif


using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.LowLevel;
using static UnityEngine.Mesh;
using UnityEngine.Rendering;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace unity4dv
{
    public enum OutRangeMode
    {
        Loop    = 0,
        Reverse = 1,
        Stop    = 2,
        Hide    = 3
    }

    public enum SourceType
    {
        Local   = 0,
        Network = 1
    }

    interface IPlugin4DSInterface
    {
        void Initialize(bool ResetRange = false);
        void Close();

        void Play(bool On);
        void GoToFrame(int Frame);
    }

    [Serializable]
    public class ListEventGUI
	{
        public bool show;
        public List<int> eventsFrames;
        public List<string> eventsNames;

        public void Clear()
        {
            eventsFrames.Clear();
            eventsNames.Clear();
        }

        public void Add(int Frame, string Name)
        {
            eventsFrames.Add(Frame);
            eventsNames.Add(Name);
        }

        public void Sort()
        {
            for (int i = 0; i < eventsFrames.Count; i++)
            {
                int item    = eventsFrames[i];
                string name = eventsNames[i];
                int currentIndex = i;

                while (currentIndex > 0 && eventsFrames[currentIndex - 1] > item)
                {
                    eventsFrames[currentIndex] = eventsFrames[currentIndex - 1];
                    eventsNames[currentIndex]  = eventsNames[currentIndex  - 1];
                    currentIndex--;
                }

                eventsFrames[currentIndex] = item;
                eventsNames[currentIndex]  = name;
            }
        }
    }


    [BurstCompile]
    internal struct UpdateMeshJob : IJob
    {
        public unsafe void Execute()
        {
            int key         = IdentificationNumbers[1];
            int lastModelID = IdentificationNumbers[2];
            int nbVertices  = 0;
            int nbTriangles = 0;
            
            IdentificationNumbers[0] = Bridge4DS.UpdateModelNative(
                key,
                Vertices.GetUnsafePtr(),
                UVs.GetUnsafePtr(),
                Triangles.GetUnsafePtr(),
                Texture.GetUnsafePtr(),
                Normals.GetUnsafePtr(),
                Velocities.GetUnsafePtr(),
                BBoxes.GetUnsafePtr(),
                lastModelID,
                ref nbVertices,
                ref nbTriangles,
                EnableLookAt,
                LookAtTarget.x,// can't pass vector3 directly without breaking on android
                LookAtTarget.y,
                LookAtTarget.z,
                LookAtMaxAngle);

            MeshDescriptor[0] = nbVertices;
            MeshDescriptor[1] = nbTriangles;
        }
        
        public NativeArray<int> IdentificationNumbers;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> Vertices;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector2> UVs;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<uint>    Triangles;
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<byte>    Texture;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> Normals;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> Velocities;
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<Vector3> BBoxes;

        public NativeArray<int> MeshDescriptor;

        [ReadOnly]
        public bool EnableLookAt;
        public Vector3 LookAtTarget;
        [ReadOnly]
        public int LookAtMaxAngle;
    }

    [BurstCompile]
    internal struct UpdateMeshLiveJob : IJob
    {
        public unsafe void Execute()
        {
            int key         = IdentificationNumbers[1];
            int lastModelID = IdentificationNumbers[2];
            int nbVertices  = 0;
            int nbTriangles = 0;
            
            IdentificationNumbers[0] = Bridge4DS.UpdateModelLiveNative(
                key,
                Vertices.GetUnsafePtr(),
                Triangles.GetUnsafePtr(),
                BBoxes.GetUnsafePtr(),
                Colors.GetUnsafePtr(),
                lastModelID,
                ref nbVertices,
                ref nbTriangles,
                EnableLookAt,
                LookAtTarget.x,
                LookAtTarget.y,
                LookAtTarget.z,
                LookAtMaxAngle);

            MeshDescriptor[0] = nbVertices;
            MeshDescriptor[1] = nbTriangles;
        }
        
        public NativeArray<int> IdentificationNumbers;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<Vector3> Vertices;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<uint>    Triangles;
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<Vector3> BBoxes;
        [NativeDisableUnsafePtrRestriction]
        public NativeArray<byte> Colors;

        public NativeArray<int> MeshDescriptor;

        [ReadOnly]
        public bool EnableLookAt;
        public Vector3 LookAtTarget;
        [ReadOnly]
        public int LookAtMaxAngle;
    }
    
    public class Plugin4DS : MonoBehaviour, IPlugin4DSInterface
    {
        #region Properties
        
        /// <summary>
        /// Currently displayed mesh frame.
        /// </summary>
        public int CurrentFrame { get => GetCurrentFrame(); set => GoToFrame(value); }
        
        /// <summary>
        /// Frame rate of the sequence.
        /// </summary>
        public float Framerate => GetFrameRate();

        /// <summary>
        /// Number of frames in the sequence.
        /// </summary>
        public int SequenceNbOfFrames => GetSequenceNbFrames();

        /// <summary>
        /// Number of frames in the active range (between first and last active frame).
        /// </summary>
        public int ActiveNbOfFrames => GetActiveNbFrames();

        /// <summary>
        /// First frame being played.
        /// </summary>
        public int FirstActiveFrame { get => (int) activeRangeMin; set => activeRangeMin = value; }
        
        /// <summary>
        /// Last frame being played.
        /// </summary>
        public int LastActiveFrame
        {
            get => (int) activeRangeMax == -1 ? SequenceNbOfFrames - 1 : (int) activeRangeMax;
            set => activeRangeMax = value;
        }
        
        /// <summary>
        /// Sequence texture image encoding (astc for mobile or dxt for desktop).
        /// </summary>
        public TextureFormat TextureEncoding => GetTextureFormat();

        /// <summary>
        /// Sequence texture image size.
        /// </summary>
        public int TextureSize => Bridge4DS.GetTextureSize(_dataSource.UUID);

        /// <summary>
        /// Number of vertices in the current mesh.
        /// </summary>
        public int NbVertices => _nbVertices;

        /// <summary>
        /// Number of triangles in the current mesh.
        /// </summary>
        public int NbTriangles => _nbTriangles;

        /// <summary>
        /// Does sequence play automatically at start?
        /// </summary>
        public bool AutoPlay { get => autoPlay; set => autoPlay = value; }
        
        /// <summary>
        /// Is the sequence currently playing?
        /// </summary>
        public bool IsPlaying { get => isPlaying; set => isPlaying = value; }
        
        /// <summary>
        /// Has the plugin been initialised?
        /// </summary>
        public bool IsInitialized => isInitialized;

        /// <summary>
        /// Sequence file name.
        /// </summary>
        public string SequenceName { get => sequenceName; set => sequenceName = value; }
        
        /// <summary>
        /// Sequence file path.
        /// </summary>
        public string SequenceDataPath { get => mainDataPath; set => mainDataPath = value; }
        
        /// <summary>
        /// Local file or network stream?
        /// </summary>
        public SourceType SourceType { get => sourceType; set => sourceType = value; }
        
        /// <summary>
        /// Is the sequence looping?
        /// </summary>
        public bool Loop { get => loop; set => loop = value; }
        /// <summary>
        /// decryption key
        /// </summary>
        public string DecryptionKey { get => decryptionKey;  set => decryptionKey = value; }

        /// <summary>
        /// Frame used as preview mesh.
        /// </summary>
        public int PreviewFrame { get => previewFrame; set => previewFrame = value; }
        
        public DataSource4DS DataSource => _dataSource;

        /// <summary>
        /// Speed ratio used (1 for normal speed, 0.5 for half speed).
        /// </summary>
        public float SpeedRatio
        {
            get => speedRatio;
            set
            {
                speedRatio = value;
                if (_dataSource != null)
                {
                    Bridge4DS.SetSpeed(_dataSource.UUID, speedRatio);
                }
            }
        }

        /// <summary>
        /// Number of meshes ready in the buffer.
        /// </summary>
        public int MeshBufferSize => Bridge4DS.GetMeshBufferSize(_dataSource.UUID);

        /// <summary>
        /// Number of data chunks ready for decoding in the buffer.
        /// </summary>
        public int ChunkBufferSize => Bridge4DS.GetChunkBufferSize(_dataSource.UUID);

        /// <summary>
        /// Maximum number of meshes in the buffer.
        /// </summary>
        public int MeshBufferMaxSize { get => meshBufferMaxSize; set => meshBufferMaxSize = value; }
        
        /// <summary>
        /// Maximum number of chunks in the buffer.
        /// </summary>
        public int ChunkBufferMaxSize { get => chunkBufferMaxSize; set => chunkBufferMaxSize = value; }
        
        /// <summary>
        /// Payload size for each http request (network stream).
        /// </summary>
        public int HTTPDownloadSize { get => httpDownloadSize; set => httpDownloadSize = value; }
        
        /// <summary>
        /// Size of the downloaded data cache size (network stream).
        /// </summary>
        public bool HTTPKeepInCache { get => httpKeepInCache; set => httpKeepInCache = value; }
        
        /// <summary>
        /// Is the downloaded data kept in the cache (network stream)?
        /// </summary>
        public long HTTPCacheSize { get => _httpCacheSize; set => _httpCacheSize = value; }

        /// <summary>
        /// Does the sequence use vertex color instead of texture image?
        /// </summary>
        public bool HasVertexColor => _dataSource.colorPerVertex;

        /// <summary>
        /// When look at is active, position where the look at targets.
        /// </summary>
        public ref Vector3 LookAtTarget => ref _lookAtTarget;
        
        /// <summary>
        /// When look at is active, maximum angle between target direction and default look direction to apply the
        /// transformation.
        /// </summary>
        public int LookAtMaxAngle { get => _lookAtMaxAngle; set => _lookAtMaxAngle = value; }

        /// <summary>
        /// List of events in the 4ds file.
        /// </summary>
        public ListEventGUI ListEvents { get => listEvents; set => listEvents = value; }

        public bool FromTimeline { get => _fromTimeline; set => _fromTimeline = value; }

        #endregion

        #region Events

        public delegate void EventFDV();
        public event EventFDV OnNewModel;
        public event EventFDV OnModelNotFound;

        public class IntEventFDV : UnityEvent<int>
        {
        }
        
        public IntEventFDV OnFirstFrame = new();
        public IntEventFDV OnLastFrame  = new();

        public class UserEventFDV : UnityEvent<int, string>
        {
        }
        
        public UserEventFDV OnUserEvent = new();
        
        #endregion

        #region Class Members

        [SerializeField]
        private SourceType sourceType = SourceType.Local;
        
        // Path containing the 4DR data (edited in the Unity Inspector panel).
        [SerializeField]
        private string sequenceName;
        
        [SerializeField]
        private string mainDataPath;
        
        public bool dataInStreamingAssets;

        [SerializeField]
        private int meshBufferMaxSize      = 10;
        
        [SerializeField]
        private int chunkBufferMaxSize     = 180;
        
        [SerializeField]
        private int httpDownloadSize       = 10000000;
        
        private static long _httpCacheSize = 1000000000;
        
        [SerializeField]
        private bool httpKeepInCache;

        // Playback
        [SerializeField]
        private bool autoPlay = true;
        
        [SerializeField]
        private OutRangeMode outRangeMode = OutRangeMode.Loop;
        
        [SerializeField]
        private bool loop = true;

        [SerializeField]
        public bool playAudio = true;

        // Active Range
        [SerializeField]
        private float activeRangeMin;
        
        [SerializeField]
        private float activeRangeMax = -1;

	    //Decryption Key
	    [SerializeField]
	    private string decryptionKey;

        // Infos
        public bool      debugInfo;
        private float    _decodingFPS;
        private int      _lastDecodingID;
        private DateTime _lastDecodingTime;
        private float    _updatingFPS;
        private int      _lastUpdatingID;
        private DateTime _lastUpdatingTime;
        private int      _totalFramesPlayed;
        private DateTime _playDate;
        private int      _prevFrame = -1;

        // 4D source.
        private DataSource4DS _dataSource;
        
        [SerializeField]
        private int lastModelID = -1;

        // Mesh and texture objects.
        private Mesh[]      _meshes;
        private Texture2D[] _textures;
        
        private MeshFilter _meshComponent;
        private Renderer   _rendererComponent;
        private LookAt     _lookAtComponent;

        // Receiving geometry and texture buffers.
        private MeshDataArray _meshDataArray;
        private MeshData      _meshData;

        private IJob _updateMeshJob;
        //private IJob _updateMeshLiveJob;
        private JobHandle _updateMeshJobHandle;

        //old mesh buffer for shitty vision pro which can't handle meshData properly
#if !USE_JOB
        private Vector3[] _newVertices;
        private Vector2[] _newUVs = null;
        private int[] _newTriangles;
        private byte[] _newTextureData = null;
        private Vector3[] _newNormals = null;
        private Vector3[] _newVelocities = null;
        private Color32[] _newColors = null;
#endif
        private Vector3[] _newBBox = null;
        private GCHandle _newVerticesHandle;
        private GCHandle _newUVsHandle;
        private GCHandle _newTrianglesHandle;
        private GCHandle _newTextureDataHandle;
        private GCHandle _newNormalsHandle;
        private GCHandle _newVelocitiesHandle;
        private GCHandle _newBBoxHandle;
        private GCHandle _newColorsHandle;

        private float[] _samples;
        GCHandle _audioBufferHandle;

        // Mesh and texture multi-buffering (optimization).
        private int _nbGeometryBuffers = 2;
        private int _nbTextureBuffers  = 2;
        private int _currentGeometryBuffer;
        private int _currentTextureBuffer;

        private int _textureBufferSize;

        private bool _newMeshAvailable;

        // Pointer to the mesh Collider, if present (=> will update it at each frames for collisions).
        private MeshCollider _meshCollider;
        private BoxCollider  _boxCollider;

        private Sync4DS _sync4DS;

        // Events
        private string   _newEventString;
        private GCHandle _newEventHandle;

        // Has the plugin been initialised?
        [SerializeField]
        private bool isInitialized;
        
        [SerializeField]
        private bool isPlaying;

        [SerializeField]
        private int previewFrame;
        
        public DateTime LastPreviewTime = DateTime.Now;

        [SerializeField]
        private int nbFrames;
        
        [SerializeField]
        private float speedRatio = 1.0f;

        [SerializeField]
        private ListEventGUI listEvents = new();

        private readonly Dictionary<string, int> _eventsToFrame = new();

        private int _nbVertices;
        private int _nbTriangles;

        private const int MaxShort = 65535;

#if UNITY_IOS || UNITY_VISIONOS || UNITY_ANDROID
        private bool wasPlayingWhenFocusLost = false;
#endif
        
        private float _unityTimeScale = 1.0f;

        private Vector3 _lookAtTarget;
        private int     _lookAtMaxAngle = 90;

        private const string TrackingNodeName = "4DVTrackings";

        private const int EarlyUpdateIndex = 2; // According to the result from TraversePlayerLoopSystem.
        
        private bool _fromPreview;
        private Material _previewMaterial;
        
        private bool _fromTimeline;
        
        private static readonly int BaseMap       = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorMap  = Shader.PropertyToID("_BaseColorMap");
        private static readonly int UnlitColorMap = Shader.PropertyToID("_UnlitColorMap");

#endregion

        #region Public Methods

        //function used to print message in consle from plugin dll
        [AOT.MonoPInvokeCallback(typeof(Bridge4DS.DebugCallback))]
        private static void DebugMethod(string message)
        {
            Debug.LogError("From Plugin4DS Dll: " + message);
        }


        /**
         * Load the data and initialize the 4D sequence.
	     * @param ResetRange: should the active frame range be reset to the sequence default values or not.
         */
        public void Initialize(bool ResetRange = false)
        {
            Bridge4DS.RegisterDebugCallback(new Bridge4DS.DebugCallback(DebugMethod));

            // Initialize already called successfully.
            if (isInitialized)
            {
                return;
            }

            Bridge4DS.RegisterDebugCallback(new Bridge4DS.DebugCallback(DebugMethod));


            if (_dataSource == null)
            {
                const int key = 0;

                if (sourceType == SourceType.Network &&
                    !(sequenceName[..4] == "http" || sequenceName[..7] == "holosys"))
                {
                    Debug.LogError("Plugin 4DS: When using Network Source Type, " +
                                   "URL should start with http://, or holosys:// for live");
                    return;
                }

                if (ResetRange)
                {
                    activeRangeMax = -1;
                    activeRangeMin =  0;
                }

                // Creates data source from the given path.
                _dataSource = DataSource4DS.CreateDataSource(
                    key,
                    sequenceName,
                    dataInStreamingAssets,
                    mainDataPath,
                    (int) activeRangeMin,
                    (int) activeRangeMax,
                    outRangeMode,
                    decryptionKey);
                
                if (_dataSource == null)
                {
                    OnModelNotFound?.Invoke();
                    return;
                }
            }
            
            lastModelID        = -1;
            _meshComponent     = GetComponent<MeshFilter>();
            _rendererComponent = GetComponent<Renderer>();
            _meshCollider      = GetComponent<MeshCollider>();
            _boxCollider       = GetComponent<BoxCollider>();
            _lookAtComponent   = GetComponent<LookAt>();

            nbFrames        = Bridge4DS.GetSequenceNbFrames(_dataSource.UUID);
            _unityTimeScale = Time.timeScale;
            Bridge4DS.SetSpeed(_dataSource.UUID, speedRatio * _unityTimeScale);

            if (sourceType == SourceType.Network && !(sequenceName.Length > 7 && sequenceName[..7] == "holosys"))
            {
                Bridge4DS.SetHTTPDownloadSize(_dataSource.UUID, httpDownloadSize);
                Bridge4DS.SetHTTPKeepInCache(_dataSource.UUID, httpKeepInCache);
                Bridge4DS.SetHTTPCacheSize(_dataSource.UUID, _httpCacheSize);
            }
            
            Bridge4DS.SetChunkBufferMaxSize(_dataSource.UUID, chunkBufferMaxSize);
            Bridge4DS.SetMeshBufferMaxSize(_dataSource.UUID, meshBufferMaxSize);

            if (_dataSource.colorPerVertex)
            {
                var name = _rendererComponent.sharedMaterial.shader.name;
                if ( name != "Particles/Standard Surface" && name != "Particles/Standard Unlit" &&
                    name != "Universal Render Pipeline/Particles/Unlit" && name != "Universal Render Pipeline/Particles/Lit")
                {
                    if (PipelineIs("URP") || PipelineIs("PC_RPAsset")) {
                        _rendererComponent.sharedMaterial.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); 
                    } else { 
                        _rendererComponent.sharedMaterial.shader = Shader.Find("Particles/Standard Unlit");
                    }
                    if (_rendererComponent.sharedMaterial.HasProperty("_Cull"))
                        _rendererComponent.sharedMaterial.SetFloat("_Cull", 0.0f);
                }

                if (!_fromPreview)
                {
#if USE_JOB
                    // Add a step in the Player Loop in which C# jobs are created to perform the mesh update.
                    PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    currentPlayerLoop.subSystemList[EarlyUpdateIndex].updateDelegate += OnEarlyUpdateLive;
                    PlayerLoop.SetPlayerLoop(currentPlayerLoop);
#endif
                }
            } else
            {
                // Allocation occurs later, when scheduling Jobs.
                _textureBufferSize = _dataSource.textureSize * _dataSource.textureSize / 2; // Default is 4 bpp.
                if (_dataSource.textureFormat == TextureFormat4DS.PVRTC_RGB2) // pvrtc2 is 2bpp.
                {
                    _textureBufferSize /= 2;
                }
                else if (_dataSource.textureFormat == TextureFormat4DS.ASTC_8x8)
                {
                    const int blockSize = 8;
                    int xBlocks = (_dataSource.textureSize + blockSize - 1) / blockSize;
                    _textureBufferSize = xBlocks * xBlocks * 16;
                }
                else if (_dataSource.textureFormat == TextureFormat4DS.RGBA32)
                {
                    _textureBufferSize = _dataSource.textureSize * _dataSource.textureSize * 4;
                }

                if (!_fromPreview)
                {
#if USE_JOB
                    // Add a step in the Player Loop in which C# jobs are created to perform the mesh update.
                    PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    currentPlayerLoop.subSystemList[EarlyUpdateIndex].updateDelegate += OnEarlyUpdate;
                    PlayerLoop.SetPlayerLoop(currentPlayerLoop);
#endif
                }
                // Set the correct default shader.
                // "PC_RPAsset" is the name of the pipeline asset in versions 6000.0 and above.
                if (PipelineIs("URP") || PipelineIs("PC_RPAsset"))
                {
                    var rend = _rendererComponent.GetComponent<Renderer>();
                    if (rend.sharedMaterial.shader.name == "Legacy Shaders/Self-Illumin/VertexLit")
                    { 
                        rend.sharedMaterial.shader = Shader.Find("Universal Render Pipeline/Unlit"); 
                    }
                }
                else if (PipelineIs("HDRP"))
                {
                    var rend = _rendererComponent.GetComponent<Renderer>();
                    if (rend.sharedMaterial.shader.name == "Legacy Shaders/Self-Illumin/VertexLit")
                    {
                        rend.sharedMaterial.shader = Shader.Find("HDRP/Unlit");
                    }
                }

                _textures = new Texture2D[_nbTextureBuffers];

                for (int i = 0; i < _nbTextureBuffers; i++) {
                    // Texture

#if UNITY_2019_1_OR_NEWER
                    // Since unity 2019, ASTC and RGBA are no longer supported.
                    if (_dataSource.textureFormat == TextureFormat4DS.ASTC_8x8) {
                        _dataSource.textureFormat = TextureFormat4DS.ASTC_8x8;
                    }
#endif

                    Texture2D texture = new Texture2D(
                        _dataSource.textureSize, _dataSource.textureSize, (TextureFormat)_dataSource.textureFormat, false)
                    {
                        wrapMode = TextureWrapMode.Clamp,
                        filterMode = FilterMode.Bilinear
                    };
                    texture.Apply(); // Upload to the GPU.
                    _textures[i] = texture;
                }
            } //!datasource.colorPervertex

#if !USE_JOB
            //Allocates geometry buffers
            AllocateGeometryBuffers(ref _newVertices, ref _newUVs, ref _newNormals, ref _newVelocities, ref _newBBox, ref _newTriangles, ref _newColors, _dataSource.maxVertices, _dataSource.maxTriangles);
            //Gets pinned memory handle
            _newVerticesHandle = GCHandle.Alloc(_newVertices, GCHandleType.Pinned);
            _newTrianglesHandle = GCHandle.Alloc(_newTriangles, GCHandleType.Pinned);
            _newBBoxHandle = GCHandle.Alloc(_newBBox, GCHandleType.Pinned);

            if (_dataSource.colorPerVertex) {
                _newColorsHandle = GCHandle.Alloc(_newColors, GCHandleType.Pinned);
            } else {
                _newUVsHandle = GCHandle.Alloc(_newUVs, GCHandleType.Pinned);
                _newNormalsHandle = GCHandle.Alloc(_newNormals, GCHandleType.Pinned);
                _newVelocitiesHandle = GCHandle.Alloc(_newVelocities, GCHandleType.Pinned);
                _newTextureData = new byte[_textureBufferSize];
                _newTextureDataHandle = GCHandle.Alloc(_newTextureData, GCHandleType.Pinned);
            }
#endif

            // Allocates objects buffers for double buffering.
            _meshes = new Mesh[_nbGeometryBuffers];
            
            for (int i = 0; i < _nbGeometryBuffers; i++)
            {
                // Mesh
                Mesh mesh = new Mesh();
                if (_dataSource.maxVertices > MaxShort)
                {
                    mesh.indexFormat = IndexFormat.UInt32;
                }
                mesh.MarkDynamic(); // Optimize mesh for frequent updates. Call this before assigning vertices. 

                Bounds newBounds  = mesh.bounds;
                newBounds.extents = new Vector3(4, 4, 4);
                
                mesh.bounds = newBounds;
                _meshes[i]  = mesh;
            }

            _currentGeometryBuffer = _currentTextureBuffer = 0;

            nbFrames = Bridge4DS.GetSequenceNbFrames(_dataSource.UUID);

            if (playAudio)
            {
                InitAudio();
            }

            // Events
            _newEventString = new string(' ', 100);
            _newEventHandle = GCHandle.Alloc(_newEventString, GCHandleType.Pinned);

#if UNITY_EDITOR
            Transform trackingNode = transform.Find(TrackingNodeName);
            if (trackingNode == null)
            {
                CreateTrackings();
            }
            FillEventsList();
#endif

            isInitialized = true;
        }

        /**
         * Starts decoding and buffering the meshes.
         */
        public void StartBuffering()
        {
            if (isInitialized)
            {
                Bridge4DS.StartBuffering(_dataSource.UUID);
            }
        }

        /**
         * Stops decoding and buffering the meshes.
         */
        public void StopBuffering()
        {
            if (isInitialized)
            {
                Bridge4DS.StopBuffering(_dataSource.UUID);
            }
        }

        /**
         * Start or pause the playback.
	     * @param On: should start or pause the playback.
         */
        public void Play(bool On)
        {
            if (!isInitialized)
            {
                return;
            }
            
            if (On)
            {
                Bridge4DS.Play4D(_dataSource.UUID, true);
                _totalFramesPlayed = 0;
                _playDate = DateTime.Now;
            }
            else
            {
                Bridge4DS.Play4D(_dataSource.UUID, false);
            }
            
            isPlaying = On;
        }

        /**
         * Stop and uninitialise the sequence.
         */
        public void Close()
        {
            Stop();
            Uninitialize();
        }

        /**
         * Reach a specific mesh frame.
	     * @param Frame: frame number looked for. Must be in the active range
	     */
        public void GoToFrame(int Frame)
        {
            bool wasPlaying = isPlaying;
            Play(false);
            
            Bridge4DS.GotoFrame(_dataSource.UUID, Frame);
            _prevFrame = CurrentFrame;
            
            Play(wasPlaying);
        }

        /**
         * Reach a specific mesh frame defined by an event.
	     * @param Name: event name looked for. Must be in the active range.
         * If two events have the same name, it reaches at random.
	     */
        public void GoToFrame(string Name)
        {
            if (_eventsToFrame.TryGetValue(Name, out var frame))
            {
                GoToFrame(frame);
            }
            else
            {
                Debug.LogError("Error GoToFrame : unknown event '" + Name + "'");
            }
        }

        /**
         * Update the preview mesh in the editor.
         */
        public void Preview()
        {
            _fromPreview = true;
            
            if (sourceType == SourceType.Network && sequenceName.Length > 7 && sequenceName[..7] == "holosys")
            {
                Texture2D live  = Resources.Load<Texture2D>("4DViews/LogoLive");
                Shader particle;
                if (PipelineIs("URP") || PipelineIs("PC_RPAsset"))
                    particle = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                else 
                    particle = Shader.Find("Particles/Standard Unlit");
                Material tempMaterial = new Material(GetComponent<Renderer>().sharedMaterial)
                {
                    mainTexture = live,
                    shader = particle
                };
                if (tempMaterial.HasProperty("_Cull"))
                    tempMaterial.SetFloat("_Cull", 0.0f);
                GetComponent<Renderer>().sharedMaterial = tempMaterial;
                
                return;
            }

            // Save params values.
            int nbGeometryTmp = _nbGeometryBuffers;
            int nbTexturesTmp = _nbTextureBuffers;
            bool debugInfoTmp = debugInfo;

            // Set params values for preview.
            _nbGeometryBuffers = 1;
            _nbTextureBuffers  = 1;
            debugInfo          = false;

            if (isInitialized && _dataSource == null)
            {
                Uninitialize();
                isInitialized = false;
            }

            if (!_fromTimeline)
            {
                // Get the sequence.
                Initialize();
            }

            if (isInitialized)
            {
                // Set mesh to the preview frame.
                GoToFrame(previewFrame);
                
                Update();

                // Assign current texture to new material to have it saved.
                if (_previewMaterial == null) {
                    _previewMaterial = new Material(_rendererComponent.sharedMaterial);
                }
                _previewMaterial.mainTexture = _rendererComponent.sharedMaterial.mainTexture;
                _rendererComponent.sharedMaterial = _previewMaterial;
            }

            // Restore params values.
            _nbGeometryBuffers = nbGeometryTmp;
            _nbTextureBuffers  = nbTexturesTmp;
            debugInfo          = debugInfoTmp;

            // Look for trackings to update.
            Transform trackingNode = transform.Find(TrackingNodeName);
            if (trackingNode is null)
            {
                return;
            }
            
            Animator animator            = trackingNode.GetComponent<Animator>();
            AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(0);
            animator.Update(previewFrame / Framerate);

            if (clipInfos == null || clipInfos.Length <= 0)
            {
                return;
            }
            
            foreach (AnimatorClipInfo clipInfo in clipInfos)
            {
                AnimationClip clip = clipInfo.clip;
                clip.SampleAnimation(trackingNode.gameObject, previewFrame / Framerate);
            }
        }

        public void LookAtPoint(in Transform TrackedTransform, Transform ModifiedTransform)
        {
            ModifiedTransform.SetPositionAndRotation(TrackedTransform.position, TrackedTransform.rotation);

            Vector3 pivot = new Vector3();
            Vector3 axis  = new Vector3();
            float angle   = 0.0f;

            if (_dataSource == null || !Bridge4DS.LookAtData(_dataSource.UUID, ref pivot, ref axis, ref angle))
            {
                return;
            }
            
            var worldAxis  = TrackedTransform.parent.TransformVector(axis);
            var worldPivot = TrackedTransform.parent.TransformPoint(pivot);
            ModifiedTransform.RotateAround(worldPivot, -worldAxis, angle);
        }
        
#endregion

        #region Unity Methods
        
        void Awake()
        {
            if (isInitialized)
            {
                Uninitialize();
            }

            if (sequenceName != "")
            {
                Initialize();
            }

            // Hide preview mesh.
            if (_meshComponent != null)
            {
                _meshComponent.mesh = null;
            }

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += HandlePauseState;
#endif
        }
        
        void Start()
        {
            if (!isInitialized && sequenceName != "")
            {
                Initialize();
            }

            if (_dataSource == null)
            {
                return;
            }

            // Launch sequence play.
            if (autoPlay)
            {
                Play(true);
            } else { 
                //display the first frame in auto playback not enabled
                GoToFrame(FirstActiveFrame); 
            }
        }
        
        // Called every frame.
        // Get the geometry from the plugin and update the unity GameObject mesh and texture.
        void Update()
        {
            if (!isInitialized && sequenceName != "")
            {
                Initialize();
            }

            if (_dataSource == null)
            {
                Debug.LogError("No data source.");
                return;
            }

#if USE_JOB
            if (_fromPreview || _fromTimeline)
            {
                if (_dataSource.colorPerVertex)
                {
                    OnEarlyUpdateLive();
                }
                else
                {
                    OnEarlyUpdate();
                }
                _fromPreview = false;
            }
#endif

#if UNITY_VISIONOS
            // Ugly hack to be compatible with visionOS. PolySpatial calls a method non synchro when modifying a mesh,
            // whereas it is sync when creating a new one.
            DestroyImmediate(_meshes[_currentGeometryBuffer]);
            _meshes[_currentGeometryBuffer] = new Mesh();
#endif

#if USE_JOB
            // Call native code.
            if (_dataSource.colorPerVertex)
                UpdateMeshLive();
            else
                UpdateMesh();
#else
            UpdateMeshNoJob();
#endif


#if UNITY_EDITOR
            // Called when the step button in editor is clicked.
            if (EditorApplication.isPaused)
            {
                GoToFrame((GetCurrentFrame() + 1) % GetSequenceNbFrames());
            }
#endif
            // Adjust Unity timescale speed if needed.
            if (Math.Abs(Time.timeScale - _unityTimeScale) > 0.0001f)
            {
                _unityTimeScale = Time.timeScale;
                Bridge4DS.SetSpeed(_dataSource.UUID, speedRatio * _unityTimeScale);
            }

            if (!_newMeshAvailable)
            {
                return;
            }
            
            // Get current object buffers (double buffering).
            Mesh mesh = _meshes[_currentGeometryBuffer];

            // Optimize mesh for frequent updates. Call this before assigning vertices.
            // Seems to be useless :(
            mesh.MarkDynamic();

            if (_textures != null)
            {
                Texture2D texture = _textures[_currentTextureBuffer];
#if !USE_JOB
                //Update texture
                texture.LoadRawTextureData(_newTextureData);
                texture.Apply();
#endif

                if (_rendererComponent.sharedMaterial.HasProperty(BaseMap))
                {
                    _rendererComponent.sharedMaterial.SetTexture(BaseMap, texture);
                }
                else if (_rendererComponent.sharedMaterial.HasProperty(BaseColorMap))
                {
                    _rendererComponent.sharedMaterial.SetTexture(BaseColorMap, texture);
                }
                else if (_rendererComponent.sharedMaterial.HasProperty(UnlitColorMap))
                {
                    _rendererComponent.sharedMaterial.SetTexture(UnlitColorMap, texture);
                }
                else
                {
#if UNITY_EDITOR
                    //var tempMaterial = new Material(_rendererComponent.sharedMaterial)
                    //{
                    //    mainTexture = texture
                    //};
                    //_rendererComponent.sharedMaterial = tempMaterial;
                    _rendererComponent.sharedMaterial.mainTexture = texture;
#else
                    _rendererComponent.material.mainTexture = texture;
#endif
                }
            }

#if !USE_JOB
            //Update geometry
            mesh.vertices = _newVertices;
            if (_nbTriangles == 0)  //case empty mesh
                mesh.triangles = null;
            else
                mesh.triangles = _newTriangles;

            if (_newUVs != null) mesh.uv = _newUVs;
            if (_newNormals != null) mesh.normals = _newNormals;
            if (_newVelocities != null) mesh.SetUVs(5, _newVelocities);
            if (_textures == null) mesh.colors32 = _newColors;
            mesh.UploadMeshData(false); //Good optimization ! nbGeometryBuffers must be = 1
#endif

            // Assign current mesh buffers and texture.
            _meshComponent.sharedMesh = mesh;

            // Switch buffer indices.
            _currentGeometryBuffer = (_currentGeometryBuffer + 1) % _nbGeometryBuffers;
            _currentTextureBuffer  = (_currentTextureBuffer  + 1) % _nbTextureBuffers;

            // Send event.
            OnNewModel?.Invoke();

            if (IsPlaying && (CurrentFrame == LastActiveFrame || CurrentFrame < _prevFrame))
            {
                if (!loop)
                {
                    Stop();
                }
            }
            _prevFrame = CurrentFrame;

            _newMeshAvailable = false;
            if (_meshCollider && _meshCollider.enabled)
            {
                _meshCollider.sharedMesh = mesh;
            }

            _totalFramesPlayed++;
            if (!debugInfo)
            {
                return;
            }
            
            double totalMilliseconds = DateTime.Now.Subtract(_lastUpdatingTime).TotalMilliseconds;
            _lastUpdatingID++;

            if (totalMilliseconds <= 500.0f)
            {
                return;
            }
            
            _updatingFPS      = (float) ((float) _lastUpdatingID / totalMilliseconds * 1000.0f);
            _lastUpdatingTime = DateTime.Now;
            _lastUpdatingID   = 0;
        }
        
        void OnGUI()
        {
            if (!debugInfo)
            {
                return;
            }
            
            double delay    = DateTime.Now.Subtract(_playDate).TotalMilliseconds -  (float) _totalFramesPlayed * 1000 / GetFrameRate();
            string decoding = _decodingFPS.ToString("00.00") + " fps";
            string updating = _updatingFPS.ToString("00.00") + " fps";
            delay /= 1000;
            
            if (!isPlaying)
            {
                delay    = 0.0f;
                decoding = "paused";
                updating = "paused";
            }
            
            int top = 20;
            GUIStyle title = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white
                },
                fontStyle = FontStyle.Bold
            };
            
            GUI.Button(new Rect(Screen.width - 210, top - 10, 200, 330), "");
            GUI.Label(new Rect(Screen.width - 200, top, 190, 20), "Sequence ", title);
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20),
                "Length: " + (GetSequenceNbFrames() / GetFrameRate()).ToString("00.00") + " sec");
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20),
                "Nb Frames: " + GetSequenceNbFrames() + " frames");
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20),
                "Frame rate: " + GetFrameRate().ToString("00.00") + " fps");
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Max vertices: "   + _dataSource.maxVertices);
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Max triangles: "  + _dataSource.maxTriangles);
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Texture format: " + _dataSource.textureFormat);
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20),
                "Texture size: " + _dataSource.textureSize + "x" + _dataSource.textureSize + "px");
            
            GUI.Label(new Rect(Screen.width - 200, top += 25, 190, 20), "Current Mesh", title);
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Nb vertices: "  + _nbVertices);
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Nb triangles: " + _nbTriangles);
            GUI.Label(new Rect(Screen.width - 200, top += 25, 190, 20), "Playback", title);
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20),
                "Time: " + (CurrentFrame / GetFrameRate()).ToString("00.00") + " sec");
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20), "Decoding rate: " + decoding);
            
            GUI.Label(new Rect(Screen.width - 200, top += 15, 190, 20),
                "Decoding delay: " + delay.ToString("00.00") + " sec");
            
            GUI.Label(new Rect(Screen.width - 200, top + 15, 190, 20), "Updating rate: " + updating);
        }
        
        void OnDestroy()
        {
            Close();
        }

#if UNITY_IOS || UNITY_VISIONOS
        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                if (wasPlayingWhenFocusLost)
                {
                    Play(true);
                }
            }
            else
            {
                wasPlayingWhenFocusLost = IsPlaying;
                Play(false);
            }
        }
#endif

#if UNITY_ANDROID
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                wasPlayingWhenFocusLost = IsPlaying;
                Play(false);
            }
            else
            {
                if (wasPlayingWhenFocusLost)
                {
                    Play(true);
                }
            }
        }
#endif
        
#endregion

#region Private Methods
        
        /**
         * Unload the data, reset all the settings, clear the buffers.
         */
        private void Uninitialize()
        {
            if (!isInitialized)
            {
                return;
            }
            
            // Releases sequence.
            if (_dataSource != null)
            {
#if USE_JOB
                // Remove OnEarlyUpdate from the current Player Loop, as it is no longer needed since the 4D sequence is
                // being uninitialized.
                if (_dataSource.colorPerVertex)
                {
                    PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    currentPlayerLoop.subSystemList[EarlyUpdateIndex].updateDelegate -= OnEarlyUpdateLive;
                    PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                }
                else
                {
                    PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    currentPlayerLoop.subSystemList[EarlyUpdateIndex].updateDelegate -= OnEarlyUpdate;
                    PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                }
#else
                //Releases memory
                if (_newVerticesHandle.IsAllocated) _newVerticesHandle.Free();
                if (_newUVsHandle.IsAllocated) _newUVsHandle.Free();
                if (_newTrianglesHandle.IsAllocated) _newTrianglesHandle.Free();
                if (_newTextureDataHandle.IsAllocated) _newTextureDataHandle.Free();
                if (_newNormalsHandle.IsAllocated) _newNormalsHandle.Free();
                if (_newVelocitiesHandle.IsAllocated) _newVelocitiesHandle.Free();
                if (_newBBoxHandle.IsAllocated) _newBBoxHandle.Free();
                if (_newColorsHandle.IsAllocated) _newColorsHandle.Free();
                _newVertices = null;
                _newUVs = null;
                _newTriangles = null;
                _newNormals = null;
                _newVelocities = null;
                _newBBox = null;
                _newColors = null;
                _newTextureData = null;
#endif
                Bridge4DS.DestroySequence(_dataSource.UUID);
                _dataSource = null;
            }

            // Releases memory.
            if (_newEventHandle.IsAllocated)
            {
                _newEventHandle.Free();
            }
            if (_audioBufferHandle.IsAllocated)
            {
                _audioBufferHandle.Free();
            }

            _samples = null;

            if (_meshes != null)
            {
                foreach (Mesh mesh in _meshes)
                {
                    DestroyImmediate(mesh);
                }
                _meshes = null;
            }
            
            if (_textures != null)
            {
                foreach (Texture2D texture in _textures)
                {
                    DestroyImmediate(texture);
                }
                _textures = null;
            }

            isInitialized = false;

#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= HandlePauseState;
#endif
        }
        
        private void InitAudio()
        {
            // Setup audio if there is one inside the 4ds file.
            int audioSize = Bridge4DS.GetAudioBufferSize(_dataSource.UUID);
            if (audioSize <= 0)
            {
                return;
            }

            AudioSource audioSource;

            // Check if audio node already exists.
            _sync4DS = GetComponent<Sync4DS>();
            if (_sync4DS._audioSources.Count > 0 && _sync4DS._audioSources[0].audioSource)
            {
                audioSource = _sync4DS._audioSources[0].audioSource;
            }
            else
            {
                var audioNode = new GameObject("Audio4DS");
                audioNode.transform.SetParent(transform.parent, false);
                audioSource = audioNode.AddComponent<AudioSource>();
                _sync4DS._audioSources.Add(new AudioSource4DS());
            }

            int nbSamples      = Bridge4DS.GetAudioNbSamples(_dataSource.UUID);
            _samples           = new float[nbSamples * Bridge4DS.GetAudioNbChannels(_dataSource.UUID)];
            _audioBufferHandle = GCHandle.Alloc(_samples, GCHandleType.Pinned);

            Bridge4DS.GetAudioBuffer(_dataSource.UUID, _audioBufferHandle.AddrOfPinnedObject());

            audioSource.clip = AudioClip.Create(
                "audioInside4ds",
                nbSamples,
                Bridge4DS.GetAudioNbChannels(_dataSource.UUID),
                Bridge4DS.GetAudioSampleRate(_dataSource.UUID),
                false);
            audioSource.clip.SetData(_samples, 0);

            _sync4DS._audioSources[0].audioSource = audioSource;
            _sync4DS.enabled = true;
        }

        private void OnEarlyUpdate()
        {
            if (_dataSource == null || !isInitialized)
            {
                return;
            }
            
            bool lookAtEnabled = _lookAtComponent && _lookAtComponent.enabled;

            VertexAttributeDescriptor[] vertexLayout =
            {
                new(VertexAttribute.Position , dimension: 3, stream: 0),
                new(VertexAttribute.Normal   , dimension: 3, stream: 1),
                new(VertexAttribute.TexCoord0, dimension: 2, stream: 2),
                new(VertexAttribute.TexCoord5, dimension: 3, stream: 3)
            };

            _meshDataArray = AllocateWritableMeshData(1);
            _meshData = _meshDataArray[0];
            _meshData.SetIndexBufferParams(_dataSource.maxTriangles * 3, IndexFormat.UInt32);
            _meshData.SetVertexBufferParams(_dataSource.maxVertices, vertexLayout);

            NativeArray<Vector3> bBoxes = new NativeArray<Vector3>(2, Allocator.TempJob);
            NativeArray<byte> texture   = new NativeArray<byte>(_textureBufferSize, Allocator.TempJob);

            NativeArray<int> meshDescriptor =
                new NativeArray<int>(2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> identificationNumbers =
                new NativeArray<int>(3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            identificationNumbers[0] = 0;
            identificationNumbers[1] = _dataSource.UUID;
            identificationNumbers[2] = lastModelID;
           
            _updateMeshJob = new UpdateMeshJob
            {
                IdentificationNumbers = identificationNumbers,
                Vertices   = _meshData.GetVertexData<Vector3>(stream: 0),
                UVs        = _meshData.GetVertexData<Vector2>(stream: 2),
                Triangles  = _meshData.GetIndexData<uint>(),
                Texture    = texture,
                Normals    = _meshData.GetVertexData<Vector3>(stream: 1),
                Velocities = _meshData.GetVertexData<Vector3>(stream: 3),
                BBoxes     = bBoxes,
                MeshDescriptor = meshDescriptor,
                EnableLookAt = lookAtEnabled,
                LookAtTarget   = _lookAtTarget,
                LookAtMaxAngle = _lookAtMaxAngle 
            };

            UpdateMeshJob job = (UpdateMeshJob)_updateMeshJob;
            _updateMeshJobHandle = job.Schedule();
        }

        private void OnEarlyUpdateLive()
        {
            if (_dataSource == null || !isInitialized)
            {
                return;
            }
            
            bool lookAtEnabled = _lookAtComponent && _lookAtComponent.enabled;

            VertexAttributeDescriptor[] vertexLayout =
            {
                new(VertexAttribute.Position, VertexAttributeFormat.Float32, dimension: 3, stream: 0),
                new(VertexAttribute.Color   , VertexAttributeFormat.UNorm8 , dimension: 4, stream: 1)
            };

            _meshDataArray = AllocateWritableMeshData(1);
            _meshData = _meshDataArray[0];
            _meshData.SetIndexBufferParams(_dataSource.maxTriangles * 3, IndexFormat.UInt32);
            _meshData.SetVertexBufferParams(_dataSource.maxVertices, vertexLayout);
            
            NativeArray<Vector3> bBoxes = new NativeArray<Vector3>(2, Allocator.TempJob);

            NativeArray<int> meshDescriptor = new NativeArray<int>(2, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<int> identificationNumbers = new NativeArray<int>(3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            identificationNumbers[0] = 0;
            identificationNumbers[1] = _dataSource.UUID;
            identificationNumbers[2] = lastModelID;

            _updateMeshJob = new UpdateMeshLiveJob
            {
                IdentificationNumbers = identificationNumbers,
                Vertices  = _meshData.GetVertexData<Vector3>(stream: 0),
                Triangles = _meshData.GetIndexData<uint>(),
                BBoxes    = bBoxes,
                Colors    = _meshData.GetVertexData<byte>(stream: 1),
                MeshDescriptor = meshDescriptor,
                EnableLookAt   = lookAtEnabled,
                LookAtTarget   = _lookAtTarget,
                LookAtMaxAngle = _lookAtMaxAngle
            };

            UpdateMeshLiveJob job = (UpdateMeshLiveJob)_updateMeshJob;
            _updateMeshJobHandle = job.Schedule();
        }
        
        private void UpdateMesh()
        {
            if (_dataSource == null || !isInitialized)
            {
                return;
            }
            
            const MeshUpdateFlags updateFlags = MeshUpdateFlags.DontRecalculateBounds |
                                                MeshUpdateFlags.DontValidateIndices   |
                                                MeshUpdateFlags.DontNotifyMeshUsers;

            _updateMeshJobHandle.Complete();

            UpdateMeshJob job = (UpdateMeshJob)_updateMeshJob;
            if (job.IdentificationNumbers.Length == 0)
            {
                // This means that the job was either never scheduled,
                // or something went awry during its execution. In any case, we
                // cannot proceed (happens with Meta Quest 3).
                _meshDataArray.Dispose();
                job.IdentificationNumbers.Dispose();
                job.BBoxes.Dispose();
                job.MeshDescriptor.Dispose();
                job.Texture.Dispose();
                return;
            }
            
            // Check if there is model.
            int modelID = job.IdentificationNumbers[0];
            if (!_newMeshAvailable)
            {
                bool lookAtEnabled = _lookAtComponent && _lookAtComponent.enabled;
                _newMeshAvailable = modelID != -1 && (modelID != lastModelID || lookAtEnabled);
            }

            if (_newMeshAvailable)
            {
                _nbVertices  = job.MeshDescriptor[0];
                _nbTriangles = job.MeshDescriptor[1];

                _meshData.subMeshCount = 1;
                SubMeshDescriptor subMesh = new SubMeshDescriptor(0, _nbTriangles * 3)
                {
                    vertexCount = _nbVertices
                };
                _meshData.SetSubMesh(0, subMesh, updateFlags);

                Mesh mesh = _meshes[_currentGeometryBuffer];
                mesh.bounds = new Bounds((job.BBoxes[0] + job.BBoxes[1]) / 2.0f,
                                         job.BBoxes[1] - job.BBoxes[0]);

                if (_boxCollider) {
                    _boxCollider.center = mesh.bounds.center;
                    if (this.gameObject.transform.localScale.x < 0) {
                        _boxCollider.size = new Vector3(-mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
                    } else {
                        _boxCollider.size = mesh.bounds.size;
                    }
                }

                if (modelID == -1)
                {
                    modelID = lastModelID;
                }
                else
                {
                    lastModelID = modelID;
                }

                ApplyAndDisposeWritableMeshData(_meshDataArray, mesh, updateFlags);

                Texture2D texture = _textures[_currentTextureBuffer];
                texture.LoadRawTextureData(job.Texture);
                texture.Apply();
            }

            if (!_newMeshAvailable)
            {
                _meshDataArray.Dispose();
            }
            job.IdentificationNumbers.Dispose();
            job.BBoxes.Dispose();
            job.MeshDescriptor.Dispose();
            job.Texture.Dispose();
            
            int nbNewEvents = Bridge4DS.PullNewEvents(_dataSource.UUID);
            for (int i = 0; i < nbNewEvents; ++i)
            {
                int typeEvent   = 0;
                int eventFrame  = Bridge4DS.GetEvent(
                    _dataSource.UUID, i, _newEventHandle.AddrOfPinnedObject(), ref typeEvent);
                _newEventString = Marshal.PtrToStringAnsi(_newEventHandle.AddrOfPinnedObject());

                switch (typeEvent)
                {
                    case 254:
                        OnFirstFrame.Invoke(eventFrame);
                        _playDate = DateTime.Now;
                        break;
                    case 255:
                        OnLastFrame.Invoke(eventFrame);
                        break;
                    default:
                        OnUserEvent.Invoke(eventFrame, _newEventString);
                        break;
                }
            }
            
            if (!debugInfo)
            {
                return;
            }
            
            double totalMilliseconds = DateTime.Now.Subtract(_lastDecodingTime).TotalMilliseconds;
            if (_lastDecodingID != 0 && totalMilliseconds <= 500.0f)
            {
                return;
            }
            
            _decodingFPS      = (float) (Mathf.Abs((float) (modelID - _lastDecodingID)) / totalMilliseconds) * 1000.0f;
            _lastDecodingTime = DateTime.Now;
            _lastDecodingID   = modelID;
        }

        private void UpdateMeshLive()
        {
            if (_dataSource == null || !isInitialized)
            {
                return;
            }
            
            const MeshUpdateFlags updateFlags = MeshUpdateFlags.DontRecalculateBounds |
                                                MeshUpdateFlags.DontValidateIndices   |
                                                MeshUpdateFlags.DontNotifyMeshUsers;

            _updateMeshJobHandle.Complete();

            UpdateMeshLiveJob job = (UpdateMeshLiveJob)_updateMeshJob;
            if (job.IdentificationNumbers.Length == 0)
            {
                // This means that the job was either never scheduled,
                // or something went awry during its execution. In any case, we
                // cannot proceed (happens with Meta Quest 3).
                _meshDataArray.Dispose();
                job.IdentificationNumbers.Dispose();
                job.BBoxes.Dispose();
                job.MeshDescriptor.Dispose();
                return;
            }

            // Check if there is model.
            int modelID = job.IdentificationNumbers[0];
            if (!_newMeshAvailable)
            {
                bool lookAtEnabled = _lookAtComponent && _lookAtComponent.enabled;
                _newMeshAvailable = modelID != -1 && (modelID != lastModelID || lookAtEnabled);
            }

            if (_newMeshAvailable)
            {
                _nbVertices  = job.MeshDescriptor[0];
                _nbTriangles = job.MeshDescriptor[1];

                _meshData.subMeshCount = 1;
                SubMeshDescriptor subMesh = new SubMeshDescriptor(0, _nbTriangles * 3)
                {
                    vertexCount = _nbVertices
                };
                _meshData.SetSubMesh(0, subMesh, updateFlags);

                Mesh mesh = _meshes[_currentGeometryBuffer];
                mesh.bounds = new Bounds((job.BBoxes[0] + job.BBoxes[1]) / 2.0f,
                                         job.BBoxes[1] - job.BBoxes[0]);

                if (_boxCollider) {
                    _boxCollider.center = mesh.bounds.center;
                    if (this.gameObject.transform.localScale.x < 0) {
                        _boxCollider.size = new Vector3(-mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
                    } else {
                        _boxCollider.size = mesh.bounds.size;
                    }
                }

                if (modelID == -1)
                {
                    modelID = lastModelID;
                }
                else
                {
                    lastModelID = modelID;
                }

                ApplyAndDisposeWritableMeshData(_meshDataArray, mesh, updateFlags);

            }

            if (!_newMeshAvailable)
            {
                _meshDataArray.Dispose();
            }
            job.IdentificationNumbers.Dispose();
            job.BBoxes.Dispose();
            job.MeshDescriptor.Dispose();

            int nbNewEvents = Bridge4DS.PullNewEvents(_dataSource.UUID);
            for (int i = 0; i < nbNewEvents; ++i)
            {
                int typeEvent   = 0;
                int eventFrame  = Bridge4DS.GetEvent(
                    _dataSource.UUID, i, _newEventHandle.AddrOfPinnedObject(), ref typeEvent);
                _newEventString = Marshal.PtrToStringAnsi(_newEventHandle.AddrOfPinnedObject());

                switch (typeEvent)
                {
                    case 254:
                        OnFirstFrame.Invoke(eventFrame);
                        break;
                    case 255:
                        OnLastFrame.Invoke(eventFrame);
                        break;
                    default:
                        OnUserEvent.Invoke(eventFrame, _newEventString);
                        break;
                }
            }
            
            if (!debugInfo)
            {
                return;
            }
            
            double totalMilliseconds = DateTime.Now.Subtract(_lastDecodingTime).TotalMilliseconds;
            if (_lastDecodingID != 0 && totalMilliseconds <= 500.0f)
            {
                return;
            }
            
            _decodingFPS      = (float) (Mathf.Abs((float) (modelID - _lastDecodingID)) / totalMilliseconds) * 1000.0f;
            _lastDecodingTime = DateTime.Now;
            _lastDecodingID   = modelID;
        }

        private unsafe void UpdateMeshNoJob()
        {
            if (_dataSource == null || !isInitialized) {
                return;
            }

            var lookat = GetComponent<LookAt>();
            bool lookAtEnabled = (/*_isPlaying && */lookat != null && lookat.enabled);
            int modelId;
            if (_dataSource.colorPerVertex) {
                //Get the new model
                modelId = Bridge4DS.UpdateModelLive(_dataSource.UUID,
                                                    _newVerticesHandle.AddrOfPinnedObject(),
                                                    _newTrianglesHandle.AddrOfPinnedObject(),
                                                    _newBBoxHandle.IsAllocated ? _newBBoxHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    _newColorsHandle.IsAllocated ? _newColorsHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    lastModelID,
                                                    ref _nbVertices,
                                                    ref _nbTriangles,
                                                    lookAtEnabled,
                                                    LookAtTarget,
                                                    LookAtMaxAngle);
            } else { 
                //Get the new model
                modelId = Bridge4DS.UpdateModel(_dataSource.UUID,
                                                    _newVerticesHandle.AddrOfPinnedObject(),
                                                    _newUVsHandle.IsAllocated ? _newUVsHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    _newTrianglesHandle.AddrOfPinnedObject(),
                                                    _newTextureDataHandle.IsAllocated ? _newTextureDataHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    _newNormalsHandle.IsAllocated ? _newNormalsHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    _newVelocitiesHandle.IsAllocated ? _newVelocitiesHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    _newBBoxHandle.IsAllocated ? _newBBoxHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    //_newColorsHandle.IsAllocated ? _newColorsHandle.AddrOfPinnedObject() : System.IntPtr.Zero,
                                                    lastModelID,
                                                    ref _nbVertices,
                                                    ref _nbTriangles,
                                                    lookAtEnabled,
                                                    LookAtTarget,
                                                    LookAtMaxAngle);
            }

            Mesh mesh = _meshes[_currentGeometryBuffer];

            mesh.bounds = new Bounds((_newBBox[0] + _newBBox[1]) / 2.0f, (_newBBox[1] - _newBBox[0]));

            if (_boxCollider && _boxCollider.enabled) {
                _boxCollider.center = mesh.bounds.center;
                _boxCollider.size = mesh.bounds.size;
            }

            //Check if there is model
            if (!_newMeshAvailable)
                _newMeshAvailable = (modelId != -1 && (modelId != lastModelID || lookAtEnabled));

            if (modelId == -1) modelId = lastModelID;
            else lastModelID = modelId;

            int nb_new_events = Bridge4DS.PullNewEvents(_dataSource.UUID);
            for (int i = 0; i < nb_new_events; ++i) {
                int typeEvent = 0;
                int eventFrame = Bridge4DS.GetEvent(_dataSource.UUID, i, _newEventHandle.AddrOfPinnedObject(), ref typeEvent);
                _newEventString = Marshal.PtrToStringAnsi(_newEventHandle.AddrOfPinnedObject());

                switch (typeEvent) {
                    case 254:
                        OnFirstFrame.Invoke(eventFrame);
                        break;
                    case 255:
                        OnLastFrame.Invoke(eventFrame);
                        break;
                    default:
                        OnUserEvent.Invoke(eventFrame, _newEventString);
                        break;
                }
            }

            if (debugInfo) {
                double timeInMSeconds = System.DateTime.Now.Subtract(_lastDecodingTime).TotalMilliseconds;
                if (_lastDecodingID == 0 || timeInMSeconds > 500f) {
                    _decodingFPS = (float)((double)(Mathf.Abs((float)(modelId - _lastDecodingID))) / timeInMSeconds) * 1000f;
                    _lastDecodingTime = System.DateTime.Now;
                    _lastDecodingID = modelId;
                }
            }
        }


#if UNITY_EDITOR
        private void HandlePauseState(PauseState State)
        {
            if (!autoPlay)
            {
                return;
            }
            
            Play(State > 0);
        }
#endif

        private int GetSequenceNbFrames()
        {
            return _dataSource != null ? Bridge4DS.GetSequenceNbFrames(_dataSource.UUID) : nbFrames;
        }

        private int GetActiveNbFrames()
        {
            return (int) activeRangeMax - (int) activeRangeMin + 1;
        }

        private int GetCurrentFrame()
        {
            return lastModelID < 0 ? 0 : lastModelID;
        }

        private float GetFrameRate()
        {
            return _dataSource?.frameRate ?? 0.0f;
        }

        private TextureFormat GetTextureFormat()
        {
            return (TextureFormat) _dataSource.textureFormat;
        }

        private void ConvertPreviewTexture()
        {
            if (sourceType == SourceType.Network && sequenceName.Length > 7 && sequenceName[..7] == "holosys")
            {
                return;
            }

            DateTime currentTime = DateTime.Now;
            
            if (_rendererComponent == null || _rendererComponent.sharedMaterial.mainTexture == null)
            {
                return;
            }

            if ((currentTime - LastPreviewTime).TotalMilliseconds < 1000 ||
                ((Texture2D)_rendererComponent.sharedMaterial.mainTexture).format == TextureFormat.DXT1)
            {
                return;
            }

            LastPreviewTime = currentTime;

            if (_rendererComponent == null)
            {
                return;
            }
            
            Texture2D tex = (Texture2D) _rendererComponent.sharedMaterial.mainTexture;
            if (!tex || tex.format == TextureFormat.RGBA32)
            {
                return;
            }
            
            Color32[] pix = tex.GetPixels32();
            Texture2D textureRGBA = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp
            };
            textureRGBA.SetPixels32(pix);
            textureRGBA.Apply();

            _rendererComponent.sharedMaterial.mainTexture = textureRGBA;
        }

        /**
         * Stops the playback.
         */
        private void Stop()
        {
            if (_dataSource != null)
            {
                Bridge4DS.Stop(_dataSource.UUID);
            }
            isPlaying = false;
        }

        private void AllocateGeometryBuffers(ref Vector3[] verts, ref Vector2[] uvs, ref Vector3[] norms, ref Vector3[] vels, ref Vector3[] bbox, ref int[] tris, ref Color32[] colors, int nbMaxVerts, int nbMaxTris)
        {
            verts = new Vector3[nbMaxVerts];
            tris = new int[nbMaxTris * 3];
            bbox = new Vector3[2];

            if (_dataSource.colorPerVertex)
                colors = new Color32[nbMaxVerts];
            else {
                uvs = new Vector2[nbMaxVerts];
                norms = new Vector3[nbMaxVerts];
                vels = new Vector3[nbMaxVerts];
            }
        }

#if UNITY_EDITOR
        private void CreateTrackings()
        {
            //_isTrackingsCreated = true;
            int nbTrackings = Bridge4DS.GetNbTrackings(_dataSource.UUID);
            if (nbTrackings == 0)
            {
                return;
            }

            GameObject trackingNode = new GameObject(TrackingNodeName);
            trackingNode.transform.SetParent(transform, false);

            var inSeqName = sequenceName[..^4];
            AnimationClip clip = AnimatorController.AllocateAnimatorClip("4DVCurves");
            for (int id = 0; id < nbTrackings; id++)
            {
                CreateTracking(clip, id);
            }

            if (!AssetDatabase.IsValidFolder("Assets/Trackings"))
            {
                AssetDatabase.CreateFolder("Assets", "Trackings");
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(
                "Assets/Trackings/" + inSeqName + "Controller.controller");

            Animator animator = trackingNode.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;

            AssetDatabase.AddObjectToAsset(clip, controller);
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(clip));
            controller.AddMotion(clip);

            _sync4DS = GetComponent<Sync4DS>();
            _sync4DS.enabled = true;
            AnimationSource4DS animSrc = new AnimationSource4DS
            {
                animationSource = animator
            };
            _sync4DS._animationSources.Add(animSrc);
        }

        private void CreateTracking(AnimationClip Clip, int Id)
        {
            int firstFrame       = 0;
            int lastFrame        = 0;
            int rotationType     = 0;
            string trackingName  = new string(' ', 100) + Id;
            GCHandle trackingNameHandle = GCHandle.Alloc(trackingName, GCHandleType.Pinned);
            Bridge4DS.GetTrackingInfos(
                _dataSource.UUID,
                Id,
                ref firstFrame,
                ref lastFrame,
                ref rotationType,
                trackingNameHandle.AddrOfPinnedObject());
            
            trackingName = Marshal.PtrToStringAnsi(trackingNameHandle.AddrOfPinnedObject());
            trackingName += Id;

            int trackNbFrames   = lastFrame - firstFrame + 1;
            Vector3[] positions = new Vector3[trackNbFrames];
            GCHandle posHandle = GCHandle.Alloc(positions, GCHandleType.Pinned);

            Vector3[] rotations_euler = null;
            Vector4[] rotations_quat = null;
            GCHandle rotHandle;
            if (rotationType == 0) {//euler angles
                rotations_euler = new Vector3[trackNbFrames];
                rotHandle = GCHandle.Alloc(rotations_euler, GCHandleType.Pinned);
            } else {//quaternion
                rotations_quat = new Vector4[trackNbFrames];
                rotHandle = GCHandle.Alloc(rotations_quat, GCHandleType.Pinned);
            }

            Bridge4DS.GetTrackingBuffers(
                _dataSource.UUID, Id, posHandle.AddrOfPinnedObject(), rotHandle.AddrOfPinnedObject());

            AnimationCurve translateX = new AnimationCurve();
            AnimationCurve translateY = new AnimationCurve();
            AnimationCurve translateZ = new AnimationCurve();
            AnimationCurve rotateX = new AnimationCurve();
            AnimationCurve rotateY = new AnimationCurve();
            AnimationCurve rotateZ = new AnimationCurve();
            AnimationCurve rotateW = new AnimationCurve();
            float fps = Framerate;
            for (int i = 0; i < trackNbFrames; i++) {
                translateX.AddKey(new Keyframe(i / fps, positions[i].x));
                translateY.AddKey(new Keyframe(i / fps, positions[i].y));
                translateZ.AddKey(new Keyframe(i / fps, positions[i].z));
                if (rotationType == 0) {//euler angles
                    rotateX.AddKey(new Keyframe(i / fps, rotations_euler[i].x));
                    rotateY.AddKey(new Keyframe(i / fps, rotations_euler[i].y));
                    rotateZ.AddKey(new Keyframe(i / fps, rotations_euler[i].z));
                } else {//quaternion
                    rotateX.AddKey(new Keyframe(i / fps, rotations_quat[i].x));
                    rotateY.AddKey(new Keyframe(i / fps, rotations_quat[i].y));
                    rotateZ.AddKey(new Keyframe(i / fps, rotations_quat[i].z));
                    rotateW.AddKey(new Keyframe(i / fps, rotations_quat[i].w));
                }
            }
            Clip.SetCurve(trackingName, typeof(Transform), "localPosition.x", translateX);
            Clip.SetCurve(trackingName, typeof(Transform), "localPosition.y", translateY);
            Clip.SetCurve(trackingName, typeof(Transform), "localPosition.z", translateZ);
            Clip.SetCurve(trackingName, typeof(Transform), "localRotation.x", rotateX);
            Clip.SetCurve(trackingName, typeof(Transform), "localRotation.y", rotateY);
            Clip.SetCurve(trackingName, typeof(Transform), "localRotation.z", rotateZ);
            if (rotationType == 1)
                Clip.SetCurve(trackingName, typeof(Transform), "localRotation.w", rotateW);

            GameObject trackGO = new GameObject(trackingName);
			trackGO.transform.parent = this.transform.Find(TrackingNodeName);
			
            posHandle.Free();
            rotHandle.Free();
        }

        private void FillEventsList()
        {
            listEvents.Clear();
            _eventsToFrame.Clear();
            
            int nbEvent = Bridge4DS.GetSizeEventList(_dataSource.UUID);

            string eventName = new string(' ', 100);
            GCHandle eventNameHandle = GCHandle.Alloc(eventName, GCHandleType.Pinned);
            for (int i = 0; i < nbEvent; ++i)
            {
                int eventFrame =
                    Bridge4DS.GetEventFromList(_dataSource.UUID, i, eventNameHandle.AddrOfPinnedObject());
                eventName = Marshal.PtrToStringAnsi(eventNameHandle.AddrOfPinnedObject());

                listEvents.Add(eventFrame, eventName);

                try
                {
                    if (eventName != null)
                    {
                        _eventsToFrame.Add(eventName, eventFrame);
                    }
                }
                catch (ArgumentException)
                {
                }
            }
            eventNameHandle.Free();

            listEvents.Sort();
        }

#endif
        private bool PipelineIs(string type)
        {
            if (QualitySettings.renderPipeline != null)
            {
                return QualitySettings.renderPipeline.name.StartsWith(type);
            }

            if (GraphicsSettings.defaultRenderPipeline != null)
            {
                return GraphicsSettings.defaultRenderPipeline.name.StartsWith(type);
            }

            return false; // Should be Built-in pipeline
        }

#endregion
    }
}
