using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUpController : MonoBehaviour
{
   [Header("Pickup Settings")]
   [SerializeField] Transform m_holdArea;
   private GameObject m_heldObj;
   private Rigidbody m_heldObjRB;

    [Header("Physics Parameters")]
    [SerializeField] private float m_pickupRange = 5.0f;
    [SerializeField] private float m_pickupForce = 150.0f;
    private void Update()
    {
       
        if(m_heldObj != null)
        {
            MoveObject();
        }
    }

    void PickUpInput()
    {
        if(m_heldObj == null) 
            {
                RaycastHit hit;
                if(Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, m_pickupRange))
                {
                    PickupObject(hit.transform.gameObject);
                }
            }
            else
            {
                DropObject();
            }
    }

    void PickupObject(GameObject pickObj)
    {
        if(pickObj.GetComponent<Rigidbody>())
        {
            m_heldObjRB = pickObj.GetComponent<Rigidbody>();
            m_heldObjRB.useGravity = false;
            m_heldObjRB.drag = 10;
            m_heldObjRB.constraints = RigidbodyConstraints.FreezeRotation;

            m_heldObjRB.transform.parent = m_holdArea;
            m_heldObj = pickObj;
        }
    }

    void MoveObject()
    {
        if(Vector3.Distance(m_heldObj.transform.position, m_holdArea.position) > 0.1f)
        {
            Vector3 m_moveDirection = (m_holdArea.position - m_heldObj.transform.position);
            m_heldObjRB.AddForce(m_moveDirection * m_pickupForce);
        }
    }

    void DropObject()
    {
        
            m_heldObjRB.useGravity = true;
            m_heldObjRB.drag = 1;
            m_heldObjRB.constraints = RigidbodyConstraints.None;

            m_heldObjRB.transform.parent = null;
            m_heldObj = null;
        
    }
}
