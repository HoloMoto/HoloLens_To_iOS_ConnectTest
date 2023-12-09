using UnityEngine;

public class LogMessageListener : MonoBehaviour
{
    public Client client; // クライアントスクリプトがアタッチされたGameObjectにアタッチして、InspectorでClientを設定してください。

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Log)
        {
            client.RequestDebugLog(logString);
        }
    }
}
