using UnityEngine;
using System.Collections;

public class RotateY : MonoBehaviour {
	
	// Update is called once per frame
	void Update () {
		transform.RotateAroundLocal(Vector3.up, Time.deltaTime);
	}
}
