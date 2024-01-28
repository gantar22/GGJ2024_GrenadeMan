using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Colors")]
public class Colorset : ScriptableObject
{
    [SerializeField] public Color[] colors;
}
