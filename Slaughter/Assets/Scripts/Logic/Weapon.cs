using UnityEngine;
using System.Collections;

public class Weapon
{
	private float 		charge 		= 1.0f;
	const float   		chargeTime 	= 0.3f;
	public GameObject 	plasmaModel = null;
	
	public void Fire( GameObject model, Vector3 pos, Quaternion orientation )
	{
		GameObject.Instantiate( model, pos, orientation);
		charge = 0.0f;
	}
	
	public void UnCharge()
	{
		charge = 0.0f;
	}
	
	public void Update(float time)
	{
		charge+=time*(1.0f/0.5f);
	}
	
	public bool IsCharged
	{
		get {
			return (charge > 1.0f);
		}
	}
	
}
