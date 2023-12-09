using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogSample : MonoBehaviour
{
    void Start()
    {
        //InvokeRepeating("LogMessage", 0f, 5f);
    }


    public void SampleLog()
    {
        
        Debug.Log("Button Pressed!"+Time.deltaTime );
    }

    private void LogMessage()
    {
        // 5秒おきにログメッセージを送信
        // string logMessage = "Debug message from Unity Client";
        // client.RequestDebugLog(logMessage);
    }
}
