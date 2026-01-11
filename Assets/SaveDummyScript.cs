using System;
using UnityEngine;

public class SaveDummyScript : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        SaveManager.Instance.SaveCurrentRoomAsExperience("TestRoom1");
    }
}
