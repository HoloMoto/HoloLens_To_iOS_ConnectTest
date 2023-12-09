using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHideAndShow : MonoBehaviour
{
    public void HideAndShow()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
