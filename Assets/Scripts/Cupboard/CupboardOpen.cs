using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupboardOpen : MonoBehaviour
{
    [SerializeField] private Animator m_cupboard = null;

    [SerializeField] private string m_cupboardOpen = "C.Open";

    private void OnTriggerEnter (Collider other)
    {
        if (other.CompareTag("Player"))
        {
            m_cupboard.Play(m_cupboardOpen, 0, 0.0f);
        }
    }
}
