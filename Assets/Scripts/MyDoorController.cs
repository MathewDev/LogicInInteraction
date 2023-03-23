using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyDoorController : MonoBehaviour
{
   private Animator m_doorAnim;

   private bool m_doorOpen = false;

   private void Awake()
   {
       m_doorAnim = gameObject.GetComponent<Animator>();
   }

   public void PlayAnimation()
   {
       if(!m_doorOpen)
       {
           m_doorAnim.SetBool("IsOpen", true);
           m_doorOpen = true;
       }
       else
       {
           m_doorAnim.SetBool("IsOpen", false);
           m_doorOpen = false;
;       }
   }
}
