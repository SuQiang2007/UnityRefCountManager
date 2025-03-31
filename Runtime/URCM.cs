using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class URCM : MonoBehaviour
{
    private static bool _showLog = true;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void Log(string message)
    {
        if(!_showLog) return;
        Debug.Log($"RUCM:{message}");
    }

    public static void LogWarning(string message)
    {
        if(!_showLog) return;
        Debug.LogWarning($"RUCM:{message}");
    }
}
