using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    [SerializeField] private Animator m_myDoor = null;

    [SerializeField] private string m_doorOpen = "DoorOpen";

    private void OnTriggerEnter (Collider other)
    {
        if (other.CompareTag("Player"))
        {
            m_myDoor.Play(m_doorOpen, 0, 0.0f);
        }
    }
}
