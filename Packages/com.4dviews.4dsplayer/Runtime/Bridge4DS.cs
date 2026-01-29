using UnityEngine;
using System.Runtime.InteropServices;

//-----------------Bridge4DS-----------------//
namespace unity4dv
{
    // Imports the native plugin functions.
    public class Bridge4DS
    {

#if (UNITY_IPHONE || UNITY_VISIONOS) && !UNITY_EDITOR
        private const string ImportName = "__Internal";  
#else // Android & Desktop
        private const string ImportName = "BridgeCodec4DS";
#endif

        //callback to call unity log function from C#
        public delegate void DebugCallback(string message);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true), AOT.MonoPInvokeCallback(typeof(DebugCallback))]
        public static extern void RegisterDebugCallback(DebugCallback callback);

        // Inits the plugin (Sequence manager, etc.).
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern unsafe int CreateSequence(
                int Key,
                [MarshalAs(UnmanagedType.LPStr)] string DataPath,
                [MarshalAs(UnmanagedType.LPStr)] string DecryptionKey,
                int RangeBegin,
                int RangeEnd,
                OutRangeMode OutRangeMode);

        // Stops the plugin and releases memory (Sequence manager, etc.).
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void DestroySequence(int Key);

        // Starts or stops the playback.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void Play4D(int Key, bool On);
		
        // Stops the playback.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void Stop(int Key);

        // Starts loading and decoding.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void StartBuffering(int Key);
        
        // Stops loading and decoding.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void StopBuffering(int Key);
        
        // Gets the new model from plugin.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int UpdateModel(
                int Key,
                System.IntPtr PtrVertices,
                System.IntPtr PtrUVs,
                System.IntPtr PtrTriangles,
                System.IntPtr Texture,
                System.IntPtr Normals,
                System.IntPtr Velocities,
                System.IntPtr Bbox,
                int LastModelId,
                ref int NbVertices,
                ref int NbTriangles,
                bool EnableLookAt,
                Vector3 LookAtTarget,
                int LookAtMaxAngle);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int UpdateModelLive(
                int Key,
                System.IntPtr PtrVertices,
                System.IntPtr PtrTriangles,
                System.IntPtr Bbox,
                System.IntPtr Colors,
                int LastModelId,
                ref int NbVertices,
                ref int NbTriangles,
                bool EnableLookAt,
                Vector3 LookAtTarget,
                int LookAtMaxAngle);

        // Gets the new model from plugin.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern unsafe int UpdateModelNative(
                int Key,
                void* PtrVertices,
                void* PtrUVs,
                void* PtrTriangles,
                void* Texture,
                void* Normals,
                void* Velocities,
                void* Bbox,
                int LastModelId,
                ref int NbVertices,
                ref int NbTriangles,
                bool EnableLookAt,
                float LookAtTarget_x,
                float LookAtTarget_y,
                float LookAtTarget_z,
                int LookAtMaxAngle);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern unsafe int UpdateModelLiveNative(
                int Key,
                void* PtrVertices,
                void* PtrTriangles,
                void* BBox,
                void* Colors,
                int LastModelID,
                ref int NbVertices,
                ref int NbTriangles,
                bool EnableLookAt,
                float LookAtTarget_x,
                float LookAtTarget_y,
                float LookAtTarget_z,
                int LookAtMaxAngle);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool LookAtData(int Key, ref Vector3 Pivot, ref Vector3 Axis, ref float Angle);

        // Pull the new events that occured.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int PullNewEvents(int Key);

        // Get one event. Must be called after PullNewEvents.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetEvent(int Key, int Idx, System.IntPtr Name, ref int Type);

        // Get the number of events in file.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSizeEventList(int Key);

        // Get one event in the complete list of events in the file.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetEventFromList(int Key, int Idx, System.IntPtr Name);

        //[DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        //public static extern bool OutOfRangeEvent(int key);

        // Gets the 4DR texture image size.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetTextureSize(int Key);

        // Gets the 4DR texture encoding.
        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetTextureEncoding(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceMaxVertices(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceMaxTriangles(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern float GetSequenceFramerate(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceNbFrames(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetSequenceCurrentFrame(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GotoFrame(int Key, int Frame);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetSpeed(int Key, float SpeedRatio);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetChunkBufferSize(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetMeshBufferSize(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetChunkBufferMaxSize(int Key, int Size);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetMeshBufferMaxSize(int Key, int Size);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetHTTPDownloadSize(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetHTTPDownloadSize(int Key, int Size);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool GetHTTPKeepInCache(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetHTTPKeepInCache(int Key, bool Val);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern long GetHTTPCacheSize(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void SetHTTPCacheSize(int Key, long Size);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetAudioBuffer(int Key, System.IntPtr Samples);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioBufferSize(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioNbSamples(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioNbChannels(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetAudioSampleRate(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void AddDXTSupport(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void AddASTCSupport(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern bool HasLookAt(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern int GetNbTrackings(int Key);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetTrackingInfos(
                int Key,
                int Index,
                ref int FirstFrame,
                ref int LastFrame,
                ref int RotationType,
                System.IntPtr Name);

        [DllImport(ImportName, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        public static extern void GetTrackingBuffers(
                int Key,
                int Index,
                System.IntPtr PositionBuffer,
                System.IntPtr RotationBuffer);
    } // class Bridge4DS
} // namespace unity4DV
