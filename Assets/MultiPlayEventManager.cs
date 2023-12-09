using UnityEngine.Events;
using UnityEngine;

public class MultiPlayEventManager : MonoBehaviour
{
    [SerializeField] UnityEvent myEvent;
    public bool isReceiveEvent;

    public GameObject obj;
    private void Update()
    {
        if (isReceiveEvent)
        {
            isReceiveEvent = false;
            obj.SetActive(!obj.activeSelf);
            //RecieveEvent();
        }
    }

    public void RecieveEvent()
    {

        myEvent.Invoke();

    }
}