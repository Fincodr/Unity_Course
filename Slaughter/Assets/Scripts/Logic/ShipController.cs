using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class ShipController : MonoBehaviour {
	
	public float				maxVelocity = 40.0f;
	public float 				turningSpeedFactor = 5.0f;
	public float 				accelerationSpeedFactor = 10.0f;
	public GameObject 			plasmaModel = null;
	public Weapon  				weapon = new Weapon();
	public bool 				isControlledLocally = false; ///< Whether object is controlled locally or not.
	public float 				turningSpeed;
	
	public bool					isDead = false;	

	/*
 	*   _____                 __ 
 	*  |     \.-----.---.-.--|  |
 	*  |  --  |  -__|  _  |  _  |
 	*  |_____/|_____|___._|_____|
 	*                            
 	*   ______              __                 __              
 	*  |   __ \.-----.----.|  |--.-----.-----.|__|.-----.-----.
 	*  |      <|  -__|  __||    <|  _  |     ||  ||     |  _  |
 	*  |___|__||_____|____||__|__|_____|__|__||__||__|__|___  |
 	*                                                   |_____|
 	* 
 	*  By Mika Luoma-aho aka <Fincodr@mxl.fi> (c) 2012 	
 	*/
	
	public Vector3 				pastFollowerPosition, pastTargetPosition, newFollowerPosition;
	public Quaternion 			pastFollowerRotation, pastTargetRotation, newFollowerRotation;
	
	public double lastUpdateTime = 0.0f;		// When was the last update (timestamp)
	public double lastTransitTime = 0.0f;		// How long it took to update the last state to the client

	public double kInterpolationMaxTime = 0.1f; // How many seconds old state information we accept to be used in interpolation
	public double kExtrapolationMaxTime = 1.2f; // How many seconds we want to try to extrapolate into future
	
	// States contain the timestamp, position, rotation and velocity information
	internal struct  State
	{
		internal double timestamp;
		internal Vector3 pos;
		internal Quaternion rot;
		internal Vector3 vel;
	}
	
	State[] m_State = new State[20];	// States are stored in a buffer with 20 slots
	int m_nStatesInBuffer;				// Keep track of what slots are used
	
	public Vector3				targetPosition;

	//-----------------------------------------------------------------------------------------------------------------------
	// SaveState function is used to store new states in the buffer
	// Note: Only new states are stored, old ones are dropped (because they are sent with UDP they could come out-of-order)
	//-----------------------------------------------------------------------------------------------------------------------
	public void SaveState( ref Vector3 pos, ref Quaternion rot, ref Vector3 vel, ref NetworkMessageInfo info )
	{
		// Store state only if its newer than the previous
		if ( m_nStatesInBuffer == 0 || info.timestamp > m_State[0].timestamp )
		{
			lastUpdateTime = Network.time;
			lastTransitTime = lastUpdateTime - info.timestamp;
			
			// Shift buffer contents, oldest data erased, 18 becomes 19, ... , 0 becomes 1
			for (int i=m_State.Length-1;i>=1;i--)
			{
				m_State[i] = m_State[i-1];
			}			
			
			// Save currect received state as 0 in the buffer, safe to overwrite after shifting
			State state = new State();
			state.timestamp = info.timestamp;
			state.pos = pos;
			state.rot = rot;
			state.vel = vel;
			m_State[0] = state;
			
			// Increment state count but never exceed buffer size
			m_nStatesInBuffer = Mathf.Min(m_nStatesInBuffer + 1, m_State.Length);
			
			// Detect jump
			float distance = Vector3.Distance( m_State[0].pos, m_State[1].pos );
			if ( distance > 100.0f )
			{
				rigidbody.position = m_State[0].pos;
				rigidbody.rotation = m_State[0].rot;
				newFollowerPosition = rigidbody.position;
				pastFollowerPosition = newFollowerPosition;
				pastTargetPosition = rigidbody.position;
			}
		}
		else
		{
			// Log warning message
			Debug.LogWarning("Received old State: #" + info.timestamp + " (ignored)");
		}		
	}
	
	//-----------------------------------------------------------------------------------------------------------------------
	// Help function to smooth approach to vector position
	//-----------------------------------------------------------------------------------------------------------------------
	Vector3 SmoothApproach( Vector3 pastPosition, Vector3 pastTargetPosition, Vector3 targetPosition, float speed )
    {
        float t = Time.deltaTime * speed;
        Vector3 v = ( targetPosition - pastTargetPosition ) / t;
        Vector3 f = pastPosition - pastTargetPosition + v;
        return targetPosition - v + f * Mathf.Exp( -t );
    }
	
	//-- End of Dead Reckoning --
		
	void Awake()
	{
		// we remain until explicitly destroyed (lasts even when new level is loaded!)
		GameObject.DontDestroyOnLoad(transform.gameObject);
		isControlledLocally = false;
		turningSpeed = 0.0f;
		maxVelocity = 40.0f;
		turningSpeedFactor = 5.0f;
		accelerationSpeedFactor = 10.0f;
		targetPosition = Vector3.zero;
		Spawn();		
	}	
		
	// Firing method.  
	[RPC] void Fire()
	{		
		weapon.Fire( plasmaModel, transform.position-transform.forward*3.0f, transform.rotation);
		audio.Play();		
	}

	// Kill player
	public void Kill()
	{
		Debug.LogError ( "Kill() called on SHIP #" + GetInstanceID() );
		isDead = true;
		GetComponentInChildren<Renderer>().enabled = false;
		
		// Hide all
	    var renderers = gameObject.GetComponentsInChildren<Renderer>();
	    foreach (var renderer in renderers) {
	        renderer.enabled = false;
	    }		
		
		GameObject.Find("NetworkGUI").GetComponent<GUIScript>().SpawnExplosion(transform.position);
		collider.enabled = false;
	}
	
	// Spawn player
	public void Spawn()
	{
		Debug.LogError ( "Spawn() called on SHIP #" + GetInstanceID() );
		isDead = false;

		// Show all
	    var renderers = gameObject.GetComponentsInChildren<Renderer>();
	    foreach (var renderer in renderers) {
	        renderer.enabled = true;
	    }
		
		rigidbody.velocity = Vector3.zero;
		rigidbody.position = Vector3.zero;
		rigidbody.rotation = Quaternion.AngleAxis(-90.0f, Vector3.right);

		GetComponentInChildren<Renderer>().enabled = true;
		weapon.UnCharge();	// disable firing just after respawn
		collider.enabled = true;		
	}
	
	// Update is called once per game cycle (not every frame, use FixedUpdate for that)
	void Update()
	{
		if ( !isDead ) {			
			if ( isControlledLocally )
			{	
				if ( Input.GetKeyDown(KeyCode.Space) && weapon.IsCharged )
				{
					// Fire(); // fire local weapon
					networkView.RPC( "Fire", RPCMode.AllBuffered );
	
				}
			}						
			weapon.Update(Time.deltaTime);			
		}
	}	
	
	// FixedUpdate is called before rendering each frame (so rate can be as high as 60hz)
	void FixedUpdate()
	{
		// If we are not dead, update our position, velocity and rotation
		if ( !isDead )
		{
			
			// if we are controlled locally we can set the rigidbody directly.
			if ( isControlledLocally )
			{
				// model is reversed, thus is our forward axis. There is no negative acceleration (See Project Settings->Input), 
				// ship must be turned and accelerated in order to change movement direction.
				rigidbody.AddRelativeForce( Input.GetAxis("Acceleration")*-accelerationSpeedFactor * Vector3.forward );				
				rigidbody.velocity = Vector3.ClampMagnitude(rigidbody.velocity, maxVelocity);				
				
				// Rotation will be applied discretely.
				// Smoother turning could be accomplished by using Torque. 				
				//turningSpeed = Input.GetAxis("Rotation");
				if ( Input.GetKey(KeyCode.LeftArrow))
					turningSpeed = -1.0f;
				else if ( Input.GetKey(KeyCode.RightArrow))
					turningSpeed = 1.0f;
				else
					turningSpeed = 0.0f;
				
				rigidbody.MoveRotation( rigidbody.rotation * Quaternion.AngleAxis( turningSpeedFactor * turningSpeed, Vector3.up) ); 				
			}
			else
			{	
				
				// Dead reckoning? Check module
				
				double currentTime = Network.time;
				
				// We have a window of interpolationBackTime where we basically play 
				// By having interpolationBackTime the average ping, you will usually use interpolation.
				// And only if no more data arrives we will use extrapolation
				
				//-- Dead Reckoning: INTERPOLATION
				// Check if latest state exceeds interpolation time, if this is the case then
				// it is too old and extrapolation should be used
				/* DISABLED INTERPOLATION FOR DEBUGGING
				double interpolationTime = currentTime - kInterpolationMaxTime;
				if (m_State[0].timestamp > interpolationTime )
				{
					Debug.Log ("### Dead Reckoning 'Interpolation' started because last state is not too old (last timestamp accepted is " + interpolationTime + ") ###");
					//Debug.Log( "interpolating, time = " + interpolationTime );
					for (int i=0;i<m_nStatesInBuffer;i++)
					{
						// Find the state which matches the interpolation time (time+0.1) or use last state
						//Debug.Log ("-- State[" + i + "].timestamp = " + m_State[i].timestamp + " --");
						if (m_State[i].timestamp <= interpolationTime || i == m_nStatesInBuffer-1)
						{
							// The state one slot newer (<100ms) than the best playback state
							int j = Mathf.Max(i-1, 0);
							State rhs = m_State[j];
							// The best playback state (closest to 100 ms old (default time))
							State lhs = m_State[i];
							
							// Use the time between the two slots to determine if interpolation is necessary
							double length = rhs.timestamp - lhs.timestamp;
							
							//Debug.Log ("-- Using States #" + i + " (best) and #" + j + " which have delta of " + length);
							
							float t = 0.0f;
							// As the time difference gets closer to 100 ms t gets closer to 1 in 
							// which case rhs is only used
							if (length > 0.0001f)
								t = (float)((interpolationTime - lhs.timestamp) / length);
							
							// if t=0 => lhs is used directly
							rigidbody.position = Vector3.Lerp(lhs.pos, rhs.pos, t);
							rigidbody.rotation = Quaternion.Slerp(lhs.rot, rhs.rot, t);
							break;
						}
					}
				}
				//-- Dead Reckoning: EXTRAPOLATION
				// Extrapolation calculates where player would be after a determined time
				// based on the 2 last positions.
				else
				*/
				{
					//Debug.Log ("### Dead Reckoning 'Extrapolation' started because last state was too old ###");
					
					// How far to the future we need to extrapolate?
					float extrapolationLength = Convert.ToSingle(currentTime - m_State[0].timestamp); // + lastTransitTime);					
					Debug.Log ("-- extrapolationLength = " + extrapolationLength + " --");
					
					//float extrapolationLength = Convert.ToSingle(currentTime - m_BufferedState[0].timestamp)/1000.0f;					
					// If the last state difference to realtime is under the time we can extrapolate and we 
					// have atleast 2 states in buffer we should extrapolate					
					if ( extrapolationLength<kExtrapolationMaxTime && m_nStatesInBuffer>1 )
					{	
						Vector3 expectedPosition;
						//expectedPosition = m_State[0].pos + (((m_State[0].pos - m_State[1].pos) / (float)(m_State[0].timestamp - m_State[1].timestamp)) * extrapolationLength);
						expectedPosition = m_State[0].pos + m_State[0].vel * (extrapolationLength + 0.1f);
						
						newFollowerPosition = SmoothApproach( pastFollowerPosition, pastTargetPosition, expectedPosition, 20.0f ); //distance*20.0f ); //distance/10.0f );
						pastFollowerPosition = newFollowerPosition;
						pastTargetPosition = expectedPosition;
						rigidbody.position = newFollowerPosition;						
						rigidbody.rotation = Quaternion.Slerp ( rigidbody.rotation, m_State[0].rot, 0.5f );
					}
					else
					{
						// Can't extrapolate :(, so the position directly.
						rigidbody.position = m_State[0].pos;
						rigidbody.rotation = m_State[0].rot;
					}					
				}
			}
		
			float aspectRatio = (float)Screen.width / (float)Screen.height;
			Vector3 pos = rigidbody.position;
			
			// change ship position if moved off-screen to match "wrapping" of edges.
			if ( pos.x >  100*aspectRatio )	pos.x -= 200*aspectRatio; 
			if ( pos.x < -100*aspectRatio ) pos.x += 200*aspectRatio;
			if ( pos.y >  100 ) pos.y -= 200;
			if ( pos.y < -100 ) pos.y += 200;
			
			// Finally, make it so on screen.
			rigidbody.position = pos;
		}
	}

	
}
