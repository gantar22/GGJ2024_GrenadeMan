using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowArrow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer SR;
    
    
    
    public void SetAngle(Vector2 inDir)
    {
        float newZ = Mathf.Rad2Deg * -Mathf.Atan2(inDir.x, inDir.y);
        transform.rotation = Quaternion.Euler(0,0f, newZ);
    }

    public void SetColor(Color inColor)
    {
        SR.color = inColor;
    }

    public void SetActive(bool inActive)
    {
        SR.enabled = inActive;
    }

    public void SetAlpha(float inAlpha)
    {
        
    }
}
