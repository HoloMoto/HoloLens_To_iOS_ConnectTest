using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;

public class Server : MonoBehaviour
{
    private TcpListener _tcpListener;
    private Thread _tcpListenerThread;
    private TcpClient _connectedTcpClient;
    public TextMeshProUGUI logText;

    private readonly Queue<string> _logQueue = new Queue<string>();
    private readonly object _lockObject = new object();

    public RawImage rawImage;

    public Texture2D sendimage;

    [Tooltip("Port number")] public int _port = 9991; // Default is 9991

    private void Start()
    {
        _tcpListenerThread = new Thread(new ThreadStart(ListenForIncomingRequests));
        _tcpListenerThread.IsBackground = true;
        _tcpListenerThread.Start();

        // Start a coroutine to process logs from the main thread
        StartCoroutine(ProcessLogQueue());
    }

    private void Update()
    {
        // Process logs in the main thread
        MainThreadDispatcher.Execute();
    }

    private void ListenForIncomingRequests()
    {
        try
        {
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();

            Debug.Log($"Server is listening on port {_port}");

            while (true)
            {
                _connectedTcpClient = _tcpListener.AcceptTcpClient();

                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(_connectedTcpClient);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error: {e.Message}");
        }
    }

    private void HandleClientComm(object clientObj)
    {
        TcpClient tcpClient = (TcpClient)clientObj;
        NetworkStream clientStream = tcpClient.GetStream();

        byte[] message = new byte[4096];
        int bytesRead;

        while (true)
        {
            bytesRead = 0;

            try
            {
                bytesRead = clientStream.Read(message, 0, 4096);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error reading from client: {e.Message}");
                break;
            }

            if (bytesRead == 0)
                break;

            ProcessReceivedData(message, bytesRead);
        }

        tcpClient.Close();
    }

    private void ProcessReceivedData(byte[] data, int length)
    {
        string header = Encoding.ASCII.GetString(data, 0, 3);
        Debug.Log($"Received header: {header}");

        if (header == "LOG")
        {
            string logMessage = Encoding.ASCII.GetString(data, 3, length - 3);
            Debug.Log($"Received log message: {logMessage}");

            // Add log message to the queue for processing in the main thread
            lock (_lockObject)
            {
                _logQueue.Enqueue(logMessage);
            }
        }
        else if (header == "IMG")
        {
            byte[] imageBytes = new byte[length - 3];
            Array.Copy(data, 3, imageBytes, 0, imageBytes.Length);

            // Base64エンコードした文字列に変換
            string base64ImageData = Encoding.ASCII.GetString(data, 13, length - 13);

            // パディング文字を追加して整形
            int padding = base64ImageData.Length % 4;
            if (padding > 0)
            {
                base64ImageData += new string('=', 4 - padding);
            }

            // Convert the Base64 string to byte array
            imageBytes = Convert.FromBase64String(base64ImageData);

            // 送信
            SendImageToClient(imageBytes);



        }
        else
        {
            Debug.Log($"Received unknown data with header: {header}");
        }
    }

    public void SendTestImageToClient()
    {
        // テスト用の画像ファイルパス
        try
        {
            Texture2D texture = sendimage;
            texture.Compress(false);
            // 画像をバイト配列に変換
            byte[] imageBytes = texture.EncodeToPNG();

            // Base64エンコードした文字列に変換
            string base64ImageData = Convert.ToBase64String(imageBytes);

            // 送信
            SendImageToClient(imageBytes);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading or sending test image: {e.Message}");
        }
    }
    public void SendImageToClient(byte[] imageBytes)
    {
        try
        {
            if (_connectedTcpClient != null)
            {
                NetworkStream clientStream = _connectedTcpClient.GetStream();

                // ヘッダーと画像データを結合して送信
                byte[] dataToSend = new byte[imageBytes.Length + 3];
                Encoding.ASCII.GetBytes("IMG").CopyTo(dataToSend, 0);
                imageBytes.CopyTo(dataToSend, 3);

                clientStream.Write(dataToSend, 0, dataToSend.Length);
                Debug.Log($"Server sent image to client.");
            }
            else
            {
                Debug.LogWarning("No connected client to send image to.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending image from server: {e.Message}");
        }
    }
    public void SendCameraTransformToClient()
    {
        try
        {
            // カメラのTransform情報を取得する
            Transform cameraTransform = Camera.main.transform;

            // カメラ情報をバイト配列に変換する
            byte[] transformBytes = SerializeTransform(cameraTransform);

            // Base64エンコードする
            string base64EncodedData = Convert.ToBase64String(transformBytes);

            // ヘッダーを追加する
            string modifiedBase64EncodedData = "TRF" + base64EncodedData;

            // クライアントに送信する
            SendDataToClient(modifiedBase64EncodedData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending camera transform: {e.Message}");
        }
    }
    public static byte[] SerializeTransform(Transform transform)
    {
        // カメラのTransform情報を含むすべてのデータを取得
        Vector3 position = transform.position;
        Quaternion rotation = transform.rotation;
        Vector3 scale = transform.localScale;

        // バイトに変換
        List<byte> bytes = new List<byte>();
        bytes.AddRange(BitConverter.GetBytes(position.x));
        bytes.AddRange(BitConverter.GetBytes(position.y));
        bytes.AddRange(BitConverter.GetBytes(position.z));
        bytes.AddRange(BitConverter.GetBytes(rotation.x));
        bytes.AddRange(BitConverter.GetBytes(rotation.y));
        bytes.AddRange(BitConverter.GetBytes(rotation.z));
        bytes.AddRange(BitConverter.GetBytes(rotation.w));
        bytes.AddRange(BitConverter.GetBytes(scale.x));
        bytes.AddRange(BitConverter.GetBytes(scale.y));
        bytes.AddRange(BitConverter.GetBytes(scale.z));

        return bytes.ToArray();
    }
    public void SendDataToClient(string message)
    {
        try
        {
            if (_connectedTcpClient != null)
            {
                NetworkStream clientStream = _connectedTcpClient.GetStream();

                // データを直接送信する
                byte[] dataToSend = Encoding.ASCII.GetBytes(message);

                clientStream.Write(dataToSend, 0, dataToSend.Length);
                Debug.Log("Data Send");
            }
            else
            {
                Debug.LogWarning("No connected client to send data to.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending data from server: {e.Message}");
        }
    }

    private IEnumerator ProcessLogQueue()
    {
        while (true)
        {
            yield return null; // Wait for the next frame

            // Process logs in the main thread
            lock (_lockObject)
            {
                while (_logQueue.Count > 0)
                {
                    string logMessage = _logQueue.Dequeue();
                    AppendLogText(logMessage);
                }
            }
        }
    }

    private void AppendLogText(string message)
    {
        // Append the new log message to the existing log text using StringBuilder
        StringBuilder sb = new StringBuilder(logText.text);
        sb.AppendLine(message);
        logText.text = sb.ToString();
    }

    private void OnDestroy()
    {
        if (_tcpListener != null)
        {
            _tcpListener.Stop();
        }
    }
}



// Helper class to dispatch actions to the main thread
public static class MainThreadDispatcher
{
    private static Queue<Action> _actions = new Queue<Action>();
    private static readonly object _lockObject = new object();

    public static void Enqueue(Action action)
    {
        lock (_lockObject)
        {
            _actions.Enqueue(action);
        }
    }

    public static void Execute()
    {
        lock (_lockObject)
        {
            while (_actions.Count > 0)
            {
                Action action = _actions.Dequeue();
                action?.Invoke();
            }
        }
    }
}
