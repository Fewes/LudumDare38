using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowParams : MonoBehaviour
{
	public bool r;
	public bool g;
	public bool b;
	//public bool a;
	[Range(0, 1)]
	public float intensity = 1;
}
