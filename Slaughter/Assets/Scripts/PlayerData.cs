using UnityEngine;
using System.Collections;
/* OnSerializeNetwork*/
public class PlayerData : MonoBehaviour 
{
	
	void OnSerializeNetworkView( BitStream stream, NetworkMessageInfo info ) {
		// Hmm, I wonder what we could do with this...	
		// Check module 4?
		if ( stream.isWriting ) {
			// Serializing to stream
			Vector3 position = new Vector3();
			Quaternion rotation = new Quaternion();
			Vector3 velocity = new Vector3();
			position = gameObject.transform.position;
			rotation = gameObject.transform.rotation;
			velocity = gameObject.rigidbody.velocity;
			stream.Serialize( ref position );
			stream.Serialize( ref rotation );
			stream.Serialize( ref velocity );
		}
		else if ( stream.isReading ) {
			// Deserializing from stream
			//gameObject.GetComponentInChildren<ShipController>().timeDiff = Time.time - gameObject.GetComponentInChildren<ShipController>().lastUpdateTime;
			//gameObject.GetComponentInChildren<ShipController>().lastUpdateTime = Time.time;
			Vector3 pos = Vector3.zero;
			Quaternion rot = Quaternion.identity;
			Vector3 vel = Vector3.zero;
			stream.Serialize( ref pos );
			stream.Serialize( ref rot );
			stream.Serialize( ref vel );			
			gameObject.GetComponentInChildren<ShipController>().SaveState( ref pos, ref rot, ref vel, ref info );
		
			/*				
			gameObject.GetComponentInChildren<ShipController>().serverPosition = position;
			gameObject.GetComponentInChildren<ShipController>().serverRotation = rotation;
			gameObject.GetComponentInChildren<ShipController>().serverVelocity = velocity;
			*/
		}
	}

	/// <summary>
	/// Raises the network instantiate event.
	/// </summary>
	/// <param name='info'>
	/// Info.
	/// </param>
	void OnNetworkInstantiate(NetworkMessageInfo info) {
		GameObject.Find("NetworkGUI").GetComponent<GUIScript>().players.Add( this.gameObject );
	}
}
