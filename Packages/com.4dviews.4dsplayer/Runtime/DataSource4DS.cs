using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace unity4dv
{
    [Serializable]
    public enum TextureFormat4DS
    {
        /// <summary>
        ///   <para>Color texture format, 8-bits per channel.</para>
        /// </summary>
        RGB24 = TextureFormat.RGB24,
        
        /// <summary>
        ///   <para>Color with alpha texture format, 8-bits per channel.</para>
        /// </summary>
        RGBA32 = TextureFormat.RGBA32,
        
        /// <summary>
        ///   <para>Compressed color texture format.</para>
        /// </summary>
        DXT1 = TextureFormat.DXT1, // 0x0000000A
        
        /// <summary>
        ///   <para>PowerVR (iOS) 2 bits/pixel compressed color texture format.</para>
        /// </summary>
        PVRTC_RGB2 = TextureFormat.PVRTC_RGB2, // 0x0000001E
        
        /// <summary>
        ///   <para>PowerVR (iOS) 4 bits/pixel compressed color texture format.</para>
        /// </summary>
        PVRTC_RGB4 = TextureFormat.PVRTC_RGB4, // 0x00000020
        
        /// <summary>
        ///   <para>ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.</para>
        /// </summary>
        ETC_RGB4 = TextureFormat.ETC_RGB4, // 0x00000022
        
        /// <summary>
        ///   <para>ASTC (8x8 pixel block in 128 bits) compressed RGB(A) texture format.</para>
        /// </summary>
        ASTC_8x8 = TextureFormat.ASTC_8x8, // 0x00000033
    }
    
    // Creates a 4D sequence from a path.
    // If the path is a directory, DataSource4DS looks for the best supported format.
    // If path is a sequence.xml file, DataSource4DS creates directly a sequence from
    // this file without checking format compatibility.
    [Serializable]
    public class DataSource4DS
    {
        public readonly int UUID;
        
#if UNITY_IOS || UNITY_VISIONOS
        [SerializeField]
        public TextureFormat4DS textureFormat = TextureFormat4DS.PVRTC_RGB4;
#elif UNITY_ANDROID
        [SerializeField]
        public TextureFormat4DS textureFormat = TextureFormat4DS.ETC_RGB4;
#else
        [SerializeField]
        public TextureFormat4DS textureFormat = TextureFormat4DS.DXT1;
#endif
        
        public int   textureSize;
        public bool  colorPerVertex;
        public int   maxVertices;
        public int   maxTriangles;
        public float frameRate;

        // Static constructor: creates a data source or returns null when no source can be created.
        public static DataSource4DS CreateDataSource(
            int Key,
            string SequenceName,
            bool DataInStreamingAssets,
            string MainPath,
            int ActiveRangeBegin,
            int ActiveRangeLastFrame,
            OutRangeMode OutRangeMode,
            string DecryptionKey)
        {
            bool success    = false;
            string rootPath;
            
            if (!(SequenceName.StartsWith("http") || SequenceName.StartsWith("holosys")) &&
                Key == 0 &&
                DataInStreamingAssets)
            {
                rootPath = Application.streamingAssetsPath + "/" + MainPath + SequenceName;

                // ANDROID STREAMING ASSETS => need to copy the data somewhere else on device to access it, because it
                // is currently in jar file.
                if (rootPath.StartsWith("jar"))
                {
                    if (!File.Exists(Application.persistentDataPath + "/" + SequenceName))
                    {
                        UnityWebRequest webRequest = UnityWebRequest.Get(rootPath);
                        webRequest.downloadHandler =
                            new DownloadHandlerFile(Application.persistentDataPath + "/" + SequenceName);
                        webRequest.SendWebRequest();
                        //yield return www; // Cannot do yield here, not really blocking because the data is local.
                        while (!webRequest.isDone)
                        {
                        }

                        if (!string.IsNullOrEmpty(webRequest.error))
                        {
                            Debug.LogError("PATH : " + rootPath);
                            Debug.LogError("Can't read data in streaming assets: " + webRequest.error);
                        }
                        else
                        {
                            if (File.Exists(Application.persistentDataPath + "/" + SequenceName))
                            {
                                rootPath = Application.persistentDataPath + "/" + SequenceName;
                                Debug.LogError(
                                    "File now exists at " + Application.persistentDataPath + "/" + SequenceName);
                            }
                            else
                            {
                                Debug.LogError(
                                    "File does not exist at " + Application.persistentDataPath + "/" + SequenceName);
                            }
                        }
                    }
                    else
                    {
                        rootPath = Application.persistentDataPath + "/" + SequenceName;
                    }
                }

                DataSource4DS instance = new DataSource4DS( Key, rootPath, ActiveRangeBegin, ActiveRangeLastFrame, OutRangeMode, ref success, DecryptionKey);
                if (success)
                {
                    return instance;
                }

                Debug.LogError("FDV Error: cannot open 4ds file at location " + rootPath);
                return null;
            }
            else
            {
                if (SequenceName.StartsWith("http") || SequenceName.StartsWith("holosys"))
                {
                    rootPath = SequenceName;
                }
                else
                {
                    rootPath = MainPath + SequenceName;
                }
                DataSource4DS instance = new DataSource4DS( Key, rootPath, ActiveRangeBegin, ActiveRangeLastFrame, OutRangeMode, ref success, DecryptionKey);
                if (success)
                {
                    return instance;
                }

                Debug.LogError("FDV Error: cannot open file at location " + rootPath);
                return null;
            }
        }

        // Private constructor.
        private DataSource4DS(
            int Key,
            string RootPath,
            int ActiveRangeBegin,
            int ActiveRangeEnd,
            OutRangeMode OutRangeMode,
            ref bool Success,
            string DecryptionKey)
        {
            UUID    = 0;
            Success = true;

            // Create sequence with native plugin.
            UUID = Bridge4DS.CreateSequence( Key, RootPath, DecryptionKey, ActiveRangeBegin, ActiveRangeEnd, OutRangeMode);
            
            if (UUID == 0)
            {
                Success = false;
            }

            // Get sequence info.
            if (!Success)
            {
                return;
            }
            
            bool systemSupportASTC = false;
            bool systemSupportDXT  = false;
                
#if UNITY_WSA // Supposed to be hololens 2.
            systemSupportDXT = true;
#else
            if (CultureInfo.InvariantCulture.CompareInfo.IndexOf(
                    SystemInfo.processorType, "ARM", CompareOptions.IgnoreCase) >= 0)
            {
                //if (Environment.Is64BitProcess)
                Bridge4DS.AddASTCSupport(UUID);
                systemSupportASTC = true;
            }
            else
            {
                // Must be in the x86 family.
                //if (Environment.Is64BitProcess)
                Bridge4DS.AddDXTSupport(UUID);
                systemSupportDXT = true;
            }
#endif

            textureSize = Bridge4DS.GetTextureSize(UUID);
            if (textureSize == 0)
            {
                // Put 1024 by default => will crash if we have 2048 texture and it's not written in xml fi.
                textureSize = 1024; 
            }

            int textureEncoding = Bridge4DS.GetTextureEncoding(UUID);

            switch (textureEncoding)
            {
                case 5:
                case 120:
                    textureFormat = TextureFormat4DS.ETC_RGB4;
                    break;
                case 6:
                case 130:
                    textureFormat = TextureFormat4DS.PVRTC_RGB4;
                    break;
                case 4:
                case 131:
                    textureFormat = TextureFormat4DS.PVRTC_RGB2;
                    break;
                case 1:
                case 100:
                    textureFormat = systemSupportDXT ? TextureFormat4DS.DXT1 : TextureFormat4DS.RGBA32;
                    break;
                case 8:
                case 164:
                    textureFormat = systemSupportASTC ? TextureFormat4DS.ASTC_8x8 : TextureFormat4DS.RGBA32; 
                    break;
                case 169:
                case 171:
                    textureFormat = TextureFormat4DS.RGBA32; 
                    break;
                case 170:
                    textureFormat = TextureFormat4DS.RGB24; 
                    break;
                case 190:
                    textureFormat  = 0;
                    colorPerVertex = true;
                    break;
                default:
#if UNITY_IOS || UNITY_VISIONOS
                    textureFormat = TextureFormat4DS.PVRTC_RGB4;
#elif UNITY_ANDROID
                    textureFormat = TextureFormat4DS.ETC_RGB4;
#else
                    textureFormat = TextureFormat4DS.DXT1;
#endif
                    break;
            }

            maxVertices = Bridge4DS.GetSequenceMaxVertices(UUID);
            if (maxVertices == 0)
            {
                maxVertices = 65535;
            }
            
            maxTriangles = Bridge4DS.GetSequenceMaxTriangles(UUID);
            if (maxTriangles == 0)
            {
                maxTriangles = 65535;
            }
            
            frameRate = Bridge4DS.GetSequenceFramerate(UUID);
        }
    }
}
