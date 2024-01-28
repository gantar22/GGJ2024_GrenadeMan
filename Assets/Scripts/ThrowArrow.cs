using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowArrow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] SRs;
    [SerializeField] private Transform MaskStart;
    [SerializeField] private Transform MaskEnd;
    [SerializeField] private Transform Mask;
    
    
    
    public void SetAngle(Vector2 inDir)
    {
        float newZ = Mathf.Rad2Deg * -Mathf.Atan2(inDir.x, inDir.y);
        transform.rotation = Quaternion.Euler(0,0f, newZ);
    }

    public void SetColor(Color inColor)
    {
        foreach(var sr in SRs)
        {
            sr.color = inColor;
        }
    }

    public void SetActive(bool inActive)
    {
        foreach (var sr in SRs)
        {
            sr.enabled = inActive;
        }
    }

    public void SetAlpha(float inAlpha)
    {
        Mask.transform.position = Vector3.Lerp(MaskStart.position, MaskEnd.position, inAlpha);
    }
}
