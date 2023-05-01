using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyDoorController : MonoBehaviour
{
   private Animator m_cupboardAnim;

   private bool m_cupboardOpen = false;

   private void Awake()
   {
       m_cupboardAnim = gameObject.GetComponent<Animator>();
   }

   public void PlayAnimation()
   {
       if(!m_doorOpen)
       {
           m_cupboardAnim.SetBool("IsOpen", true);
           m_cupboardOpen = true;
       }
       else
       {
           m_cupboardAnim.SetBool("IsOpen", false);
           m_cupboardOpen = false;
;       }
   }
}
