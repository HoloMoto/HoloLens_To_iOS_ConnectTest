using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;



public class Client : MonoBehaviour
{
    [CanBeNull] private TcpClient _client;
    private NetworkStream _stream;

    [Tooltip("Port number")] public int _port = 9991; // Default is 9991
    [Tooltip("Address of the server")] public string _ipAddress = "localhost";
    MultiPlayEventManager multiPlayEvent;
    bool _access = false;
    public RawImage rawImage;

    [SerializeField]
    GameObject iPhoneCameraPos;
    public void StartConnection()
    {
        multiPlayEvent = this.gameObject.GetComponent<MultiPlayEventManager>();
        _client = new TcpClient(_ipAddress, _port);
        _stream = _client.GetStream();

        // Start a new thread to listen for incoming messages
        new Thread(() =>
        {
            var responseBytes = new byte[536870912];
            try
            {
                while (true)
                {
                    var bytesRead = _stream.Read(responseBytes, 0, responseBytes.Length);
                    if (bytesRead == 0) break;

                    // Process received data based on header
                    ProcessReceivedData(responseBytes, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in StartConnection thread: {ex.Message}");
                // Handle the exception as needed
            }
            finally
            {
                // Clean up resources if necessary
                _client.Close();
            }
        }).Start();
    }

    private void ProcessReceivedData(byte[] data, int length)
    {

        string header = Encoding.ASCII.GetString(data, 0, 3);

        Debug.Log(Encoding.ASCII.GetString(data, 0, data.Length));
        // Only the part related to processing received data has been kept
        if (header == "LOG")
        {
            // Extract the log message from the received data
            string logMessage = Encoding.ASCII.GetString(data, 13, length - 13);
            Debug.Log($"Received log message: {logMessage}");
        }
        if(header == "FUN")
        {
            string functionMessage = Encoding.ASCII.GetString(data, 13, length - 13);

            Debug.Log("functionMessage");

            MainThreadDispatcher.Enqueue(() => _access = true);
            MainThreadDispatcher.Execute();

        }
        else if (header == "IMG")
        {

            // Extract the Base64-encoded image data from the received data
            string base64ImageData = Encoding.ASCII.GetString(data, 13, length - 13);


            // Convert the Base64 string to byte array
            byte[] imageBytes = Convert.FromBase64String(base64ImageData);
           
            // 画像をメインスレッドで処理する
            MainThreadDispatcher.Enqueue(() =>
            {
                // 画像を表示などの処理を行う
                Texture2D receivedTexture = new Texture2D(2, 2);
                receivedTexture.LoadImage(imageBytes);

                // 例：受信した画像をRawImageに表示
                rawImage.texture = receivedTexture;
                Debug.Log("Received and displayed image.");
            });
        }
        else if (header == "TRF") // 新しいヘッダー "TRF"
        {
            // Extract the Base64-encoded transform data from the received data
            Debug.Log("Received Data");
            string base64TransformData = Encoding.ASCII.GetString(data, 3, length - 3);

            // Convert the Base64 string to byte array
            byte[] transformBytes = Convert.FromBase64String(base64TransformData);
            Debug.Log(transformBytes);
            // メインスレッドで処理する
            MainThreadDispatcher.Enqueue(() =>
            {
                // カメラの Transform を適用する処理などを行う
                DeserializeAndApplyTransform(transformBytes);
                Debug.Log("Received and applied camera transform.");
            });
        }
        else
        {
            Debug.Log($"Received unknown data with header: {header}");
        }
    }
    private void DeserializeAndApplyTransform(byte[] transformBytes)
    {

        using (MemoryStream stream = new MemoryStream(transformBytes))
        {
            BinaryReader reader = new BinaryReader(stream);

            Vector3 position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Quaternion rotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

            // カメラのTransformに適用
            iPhoneCameraPos.transform.localPosition = position;
            iPhoneCameraPos.transform.localRotation = rotation;
        }
    }

    void Start()
    {
        StartConnection();
    }
    private readonly object _lockObject = new object();
    void Update()
    {
        if (_access)
        {
            Debug.Log("OKKKK");
            lock (_lockObject)
            {
                _access = false;
            }
            gameObject.SetActive(!gameObject.activeSelf);
            Debug.Log("OKKKK!!!!!!!");
            //MainThreadDispatcher.Enqueue(() => multiPlayEvent.isReceiveEvent = true);
        }
    }

    public void RequestDebugLog(string logMessage)
    {
        byte[] logBytes = Encoding.ASCII.GetBytes(logMessage);
        byte[] dataToSend = new byte[logBytes.Length + 13];
        Encoding.ASCII.GetBytes("LOG").CopyTo(dataToSend, 0);
        logBytes.CopyTo(dataToSend, 3); // 修正: 3からコピーするように変更
        _stream.Write(dataToSend, 0, dataToSend.Length);
    }

    private void OnDestroy()
    {
        // Unity エディターモードでのみ実行されるコード
#if UNITY_EDITOR
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // エディターモードのクリーンアップ処理
             EditorApplication.ExitPlaymode();
            return;
        }
#endif

        // 実行モード時のクリーンアップ処理
        _stream.Close();
        _client.Close();
    }
}

