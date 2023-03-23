using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DoorRayCast : MonoBehaviour
{
    //How far the RayCast goes
    [SerializeField] private float m_rayLength = 0.7f;
    //layerMask stops interaction with other object
    [SerializeField] private LayerMask m_layerMaskInteract;
    //avoid exceptions with SerializedField
    [SerializeField] private string m_excludeLayerName = null;

    private MyDoorController m_raycastedObj;
    //key set for interaction
    [SerializeField] private KeyCode m_openDoorKey = KeyCode.Mouse0;

    [SerializeField] private Image m_crosshair = null;
    private bool m_isCrosshairActive;
    private bool m_doOnce;

    private const string m_interactableTag = "InteractiveObject";


    private void PlayAnimation()
    {
        if (m_raycastedObj == null)
            return;
        m_raycastedObj.PlayAnimation();
    }
    private void Update()
    {
        m_raycastedObj = null;
        RaycastHit hit;
        //Local reference to RayCast
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        //Looking for specified object
        int mask = 1 << LayerMask.NameToLayer(m_excludeLayerName) | m_layerMaskInteract.value;
        //Set the RayCast
        if (Physics.Raycast(transform.position, forward, out hit, m_rayLength, mask))
        {    //Looking for a tag/specified Object and if we find it
            
                // Looking for the specified game object
                m_raycastedObj = hit.collider.gameObject.GetComponent<MyDoorController>();
                if (!m_doOnce)
                {    //Script we are looking for
                    
                    //Crosshair will change color once we find the game object
                    CroshairChange(true);
                }

                m_isCrosshairActive = true;
                m_doOnce = true;

            
        }

        //Once we find the object, will change the crosshair color back to default (specified white)
        else
        {
            if (m_isCrosshairActive)
            {
                CroshairChange(false);
                m_doOnce = false;
            }
        }
    }
    // Setting the crosshair's color for true and false actions
    void CroshairChange(bool on)
    {    // cheking if crosshair check is true, if it is it changes the color
        if (on && !m_doOnce)
        {    // on true change to orange
            m_crosshair.color = Color.green;
        }
        else //Otherwise if false, change to white
        {
            m_crosshair.color = Color.white;
            //executes if crosshair is false
            m_isCrosshairActive = false;
        }
    }
}
