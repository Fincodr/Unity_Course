using UnityEngine;
using System.Collections;

public class IntroRotator : MonoBehaviour {
	public float speed = 0.5f;
	// Update is called once per frame
	void Update () 
	{
		
		//transform.RotateAroundLocal( Vector3.up, Time.deltaTime);
		transform.RotateAroundLocal( Vector3.forward, Time.deltaTime*speed);
	}
}
