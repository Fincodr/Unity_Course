using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Simple message queue using FIFO pattern
/// </summary>
public class MessageQueue : Queue<string> {
	
	private int m_MaxMessagesInQueue;
	const int kDefaultMaxMessagesInQueue = 10;	
	
	// ctor
	public MessageQueue( int maxMessages = kDefaultMaxMessagesInQueue ) {
		m_MaxMessagesInQueue = maxMessages;
		// fill her up
		for ( int i=0; i!=m_MaxMessagesInQueue; ++i )
			base.Enqueue( "" );
	}	
	
	// Overriden Enqueue method
	public new void Enqueue( string msg ) {
		
		if ( Count >= m_MaxMessagesInQueue )
			Dequeue();
		
		base.Enqueue( msg );
		
	}
	
}

//[ExecuteInEditMode]
public class GUIScript : MonoBehaviour {
	
	enum WindowIDs {
		CONSOLE = 0,
		CHAT,
		MENU,
		CONTROL
	};
	
	/// <summary>
	/// Constant max server connections.
	/// </summary>
	const int kMaxServerConnections 	= 7;	// Max of 8 players on server
	
	/// <summary>
	/// Constant server default port.
	/// </summary>
	const int kServerDefaultPort 		= 1025;
	
	/// <summary>
	/// Constant string local host.
	/// </summary>
	const string kStrLocalHost			= "127.0.0.1";

	/// <summary>
	/// Constant string master server host name and port
	/// </summary>
	const string kStrMasterServerHost	= ""; //empty=use default server, NKUAS=172.16.0.204, Localhost=80.220.159.100;
	const int kMasterServerPort			= 55555; //not used until strMasterServerHost is set
	const string kStrGameTypeName		= "SlaughterGameByMika";
	private double fLastPollTime=0.0f;

	/// <summary>
	/// The console message queue.
	/// </summary>
	//private MessageQueue consoleQueue 	= new MessageQueue();
	
	/// <summary>
	/// The console message queue.
	/// </summary>
	private MessageQueue chatQueue 		= new MessageQueue();

	/// <summary>
	/// The server port (as string)
	/// </summary>
	private string strPort 				= "";
	
	/// <summary>
	/// The server ip (as string)
	/// </summary>
	private string strIP				= "";
	
	/// <summary>
	/// Console visible flag
	/// </summary>
	private bool bChatVisible			= false;
	
	public Texture logoTexture;
	
	private float curTime 				= 0.0f;
	
	/// Player object prefab.
	public GameObject playerModel = null;
	public GameObject enemyModel = null;
	public GameObject explosion = null;
	
	/// Current player reference.
	/// private GameObject clientPlayer = null;	

	/// Contains all player objects (including client player)
	public List<GameObject> players = new List<GameObject>();
	public Texture2D[] textures;

	private bool bLogoAnimation			= true;
	/// <summary>
	/// The logo starting y-position for animation
	/// </summary>
	private float logoYpos				= -500.0f;
	
	/// <summary>
	/// The logo starting y-speed for animation
	/// </summary>
	private float logoYspeed			= 0.0f;
	
	public GUISkin ConsoleSkin;
	
	const int kLogoWidth = 1024;
	const int kLogoHeight = 264;
	
	//public Transform playerObject		= null;
	//public ArrayList players			= new ArrayList();
	
	public string playerName = ""; ///< Name of player in field.
	public int    numberOfKills = 0;
	
	// chat window
	const int kChatWidth = 640;
	const int kChatHeight = 285;
	const int kChatWidthHidden = 150;
	const int kChatHeightHidden = 55;
	const int kChatInputFieldWidth = 400 - (30*2);
	public Rect chatRect;
	public Rect chatRectHidden;
	
	// areas for starting game.
	const int kConnectWidth = 350;
	const int kConnectHeight = 50;
	public Rect connectRect;	
	
	// Areas for in-game gui.
	public Rect controlRect = new Rect(0,0,50,25); //Screen.width-25.0f,10.0f,50.0f,25.0f);
	public Rect statsRect  = new Rect(10.0f, Screen.height - 160.0f, 150.0f, 50.0f);
	
	// high score rect
	//private Rect highScoreRect = new Rect(Screen.width-160,150,160, 250);
	
	private bool gameIsOn = false;
	private string strChatMessage = "";
	private HighScoreEntry[] highscores;
	private HighScore highscore;
	private bool bHighScoresAvailable = true;
	//private bool showHighscores = false;
	
	//private NetworkViewID playerID;
	
	/// <summary>
	/// Awake this instance.
	/// </summary>
	void Awake()
	{
		playerName = "Unnamed";
		fLastPollTime = 0.0f;
		
		strIP = kStrLocalHost;
		strPort = "" + kServerDefaultPort;
		
		// we remain until explicitly destroyed.
		GameObject.DontDestroyOnLoad(transform.gameObject);
		
		chatQueue.Enqueue( "Slaughter Game v0.0.1 (c) Mika Luoma-aho" );
		chatQueue.Enqueue( "");
		chatQueue.Enqueue( "Available commands:" );
		chatQueue.Enqueue( "/help, /quit             Need help?");
		chatQueue.Enqueue( "/connect, /disconnect    Connect or disconnect" );
		chatQueue.Enqueue( "/port <num>  Set listening or connecting port (default = 1025)" );
		chatQueue.Enqueue( "/ip <ip>     Set connecting ip (default 127.0.0.1)" );
		chatQueue.Enqueue( "/create <o>  Create object o (player, enemy, rock, ...)");
		chatQueue.Enqueue( "" );
		
		if (Network.HavePublicAddress())		
		    Debug.Log("This machine has a public IP address");		
		else		
		    Debug.Log("This machine has a private IP address");		
		
		if ( kStrMasterServerHost.Length != 0 )
		{
			MasterServer.ipAddress = kStrMasterServerHost;
	        MasterServer.port = kMasterServerPort;
			Network.natFacilitatorIP = kStrMasterServerHost;
			Network.natFacilitatorPort = kMasterServerPort;
		}
	
		highscores = new HighScoreEntry[100];
		highscore = new HighScore();
	}
	
	public void GetHighScoreListing()
	{
		/*
		// Save one highscore
		try
		{
			highscore.SetScore( kStrGameTypeName, playerName, 1);
			highscore.Timeout = 100;
		}
		catch (Exception ex)
		{
			Debug.Log ( "SetScore exception: " + ex.ToString() );
		}
		*/

		// Get highscores
		try
		{
			/*
			HighScoreEntry he = highscore.GetHighScore( kStrGameTypeName, playerName );
			Debug.Log ( he.Score );
			*/
			highscore.GetHighScores( kStrGameTypeName, 0, 1, out highscores );
			bHighScoresAvailable = true;
			//highscore.GetHighScoresAsync( kStrGameTypeName, 0, 10 );
		}
		catch (Exception ex)
		{
			Debug.Log ( "GetHighScores exception: " + ex.ToString() );
			bHighScoresAvailable = false;
		}
	}

	/// <summary>
	/// Spawns the player.
	/// </summary>
	/// <param name='pos'>
	/// Position.
	/// </param>
	public void SpawnPlayer( Vector3 pos ) 
	{
		GameObject player = (GameObject)Instantiate(playerModel, pos, Quaternion.AngleAxis(-90.0f, Vector3.right));	
		
		players.Add(player);
		
		// Load random texture for player.
		int texId = new System.Random().Next(0,textures.Length);
		player.transform.GetChild(0).renderer.materials[0].mainTexture = textures[texId];	
		
	}
	
	/// <summary>
	/// Spawns the dummy.
	/// </summary>
	/// <param name='pos'>
	/// Position.
	/// </param>
	public void SpawnDummy( Vector3 pos ) 
	{
		GameObject player = (GameObject)Instantiate(playerModel, pos, Quaternion.AngleAxis(-90.0f, Vector3.right));	
		
		// Load random texture for player.
		int texId = new System.Random().Next(0,textures.Length);
		player.transform.GetChild(0).renderer.materials[0].mainTexture = textures[texId];	
		player.GetComponent<ShipController>().isControlledLocally = false;
	}
	
	/// <summary>
	/// Spawns the explosion.
	/// </summary>
	/// <param name='pos'>
	/// Position.
	/// </param>
	public void SpawnExplosion( Vector3 pos)
	{
		Instantiate(explosion, pos, Quaternion.AngleAxis( -90, Vector3.right));	
	}	
	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start()
	{
	}
	
	/// <summary>
	/// Launchs the server.
	/// </summary>
	void LaunchServer()
	{
		chatQueue.Enqueue( "[INFO] LaunchServer -> Starting server (Listetning at port " + strPort + ")" );
		
		try {
			NetworkConnectionError err = Network.InitializeServer ( kMaxServerConnections, int.Parse(strPort), false /* TODO: Enable: !Network.HavePublicAddress() */ );
			switch ( err ) {
				case NetworkConnectionError.NoError:
					break;
				default:
					chatQueue.Enqueue( "[ERROR] LaunchServer -> Could not start server!" );
					break;
			}
		}
		catch ( FormatException )
		{
			chatQueue.Enqueue( "[ERROR] LaunchServer -> Invalid Port number \"" + strPort + "\"" );			
		}
		catch ( Exception ex )
		{
			chatQueue.Enqueue ( "[ERROR] LaunchServer -> Exception: " + ex.ToString () );
		}
	}
	
	/// <summary>
	/// On successful server launch
	/// </summary>
	void OnServerInitialized()
	{
		chatQueue.Enqueue( "[INFO] Server started at port " + strPort );
		Application.LoadLevel(1);
		gameIsOn = true;
		// NetworkInstantiate();
		chatQueue.Enqueue( "[INFO] Server started at port " + strPort );
		chatQueue.Enqueue( "[INFO] Registering Server to Master server..." );
		MasterServer.RegisterHost( kStrGameTypeName, playerName + "'s game", "Slaughter Game free for all!" );
	}
	
	void OnFailedToConnectToMasterServer( NetworkConnectionError info )
	{
		chatQueue.Enqueue("Failed to connect to the master server! (info=" + info+")");
	}
	
 	void OnMasterServerEvent(MasterServerEvent msEvent) {
        if (msEvent == MasterServerEvent.RegistrationSucceeded)
            chatQueue.Enqueue("Server has been registered on master server");
        
    }	
	
	/// <summary>
	/// Raises the disconnected from server event.
	/// </summary>
	/// <param name='info'>
	/// Info.
	/// </param>
	void OnDisconnectedFromServer( NetworkDisconnection info )
	{
		if ( Network.isServer ) {
			chatQueue.Enqueue( "[INFO] You have been disconnected from the server (Server shutting down)" );
			MasterServer.UnregisterHost();
		}
		else
		{
			if ( info == NetworkDisconnection.LostConnection )
			{
				chatQueue.Enqueue( "[INFO] You have been disconnected from the server (Lost connection to the server)" );
			}
			else
			{
				chatQueue.Enqueue( "[INFO] Successfully disconnected from the server." );
			}
		}
		foreach ( GameObject obj in players ) {
			GameObject.Destroy( obj );
		}
		players.Clear();
		gameIsOn = false;
		logoYpos = -500.0f;
		logoYspeed = 0.0f;
		bLogoAnimation = true;
	}
	
	/// <summary>
	/// Raises the player disconnected event.
	/// </summary>
	void OnPlayerDisconnected( NetworkPlayer player ) {
		chatQueue.Enqueue( "[INFO] OnPlayerDisconnected -> Clean up after a player disconnected" );
		Network.RemoveRPCs( player );
		List<GameObject> dead = new List<GameObject>();
		foreach ( GameObject obj in players ) {
			if ( obj.networkView.owner == player ) {
				dead.Add( obj );
			}
		}
		foreach ( GameObject obj in dead ) {
			players.Remove( obj );
		}
		dead.Clear();
		Network.DestroyPlayerObjects( player );
	}
	
	/// <summary>
	/// Raised when client is successfully connected to server
	/// </summary>
	void OnConnectedToServer() {
		chatQueue.Enqueue( "[INFO] OnConnectedToServer -> Connected to server" );
		Application.LoadLevel(1);
		gameIsOn = true;
		//NetworkInstantiate();
	}
	
	/// <summary>
	/// Raised when client fails to connect to the server
	/// </summary>
	void OnFailedToConnect( NetworkConnectionError info ) {
		chatQueue.Enqueue( "[ERROR] OnFailedToConnect -> Could not connect to the server: " + info );
	}
	
	/// <summary>
	/// Called when client is connecting to the server
	/// </summary>
	void ConnectToServer( HostData element = null ) {
		
		chatQueue.Enqueue( "[INFO] ConnectToServer -> Connecting client to a server" );
		
		try {
			if ( element != null )
				Network.Connect (element);
			else
				Network.Connect( strIP, int.Parse (strPort) );
		}
		catch ( FormatException )
		{
			chatQueue.Enqueue( "[ERROR] ConnectToServer -> Exception: Invalid Port number \"" + strPort + "\"" );
		}
	}	
	
	/// <summary>
	/// Networks the instantiate.
	/// </summary>
	void NetworkInstantiate() {
		chatQueue.Enqueue( "Adding using NetworkInstantiate()" );
		NetworkViewID viewID = Network.AllocateViewID();
		networkView.RPC( "CreatePlayer", RPCMode.AllBuffered, viewID );
	}

	/// <summary>
	/// Chat the specified strMessage.
	/// </summary>
	[RPC] void Chat( string strMessage ) {
		chatQueue.Enqueue( "[Message] " + strMessage );
	}
	
	/// <summary>
	/// Creates the player.
	/// </summary>
	[RPC] void CreatePlayer( NetworkViewID viewID ) {		
		chatQueue.Enqueue( "[RPC] CreatePlayer" );
		GameObject player = (GameObject)Instantiate(playerModel, new Vector3(2.0f*players.Count,0.0f,0.0f), Quaternion.AngleAxis(-90.0f, Vector3.right));
		NetworkView view = player.GetComponent<NetworkView>();
		view.viewID = viewID;		
		int texId = players.Count % textures.Length;
		player.transform.GetChild(0).renderer.materials[0].mainTexture = textures[texId];			
		// Solution #1
		if ( view.isMine ) player.GetComponent<ShipController>().isControlledLocally = true;
		// Solution #2
		// if ( view.viewID == playerID ) player.GetComponent<ShipController>().isControlledLocally = true;			
		//if ( !Network.isServer ) player.collider.enabled = false;
		players.Add( player.gameObject );
	}	
	
	/// <summary>
	/// Kills the player.
	/// </summary>
	[RPC] void KillPlayer( NetworkViewID viewID ) {		
		chatQueue.Enqueue( "[RPC] KillPlayer" );
		// "Kill" player
		foreach ( GameObject obj in players ) {
			if ( obj.networkView.viewID == viewID ) {
				obj.GetComponent<ShipController>().Kill();
				break;
			}
		}
	}	

	void OnGUI()
	{
		if ( Network.time - fLastPollTime > 15 ) {
			fLastPollTime = Network.time;
			MasterServer.ClearHostList();
			MasterServer.RequestHostList( kStrGameTypeName );
			if ( bHighScoresAvailable )
			{
				bHighScoresAvailable = false;
				try
				{
					/*
					HighScoreEntry he = highscore.GetHighScore( kStrGameTypeName, playerName );
					Debug.Log ( he.Score );
					*/
					highscore.GetHighScores( kStrGameTypeName, 0, 1, out highscores );
					bHighScoresAvailable = true;
					//highscore.GetHighScoresAsync( kStrGameTypeName, 0, 10 );
				}
				catch (Exception ex)
				{
					Debug.Log ( "GetHighScores exception: " + ex.ToString() );
				}		
			}
		}
		
		GUI.skin = ConsoleSkin;
		GUI.skin.label.normal.textColor = Color.cyan;
		
		// Allow quit anywhere
		if (Input.GetKey( KeyCode.Escape ) ) {
			Network.Disconnect();
			Application.Quit();
		}
		
		// Create player when space is pressed
		if ( Input.GetKeyDown( KeyCode.Space ) ) {
			bool bFound = false;
			foreach ( GameObject obj in players ) {
				if ( obj.networkView.isMine ) {
					if ( obj.GetComponent<ShipController>().isDead ) {
						obj.GetComponent<ShipController>().Spawn();
					}
					bFound = true;
					break;
				}
			}
			// Do we have player object already?
			if ( !bFound ) {
				NetworkInstantiate();
			}
		}
		
		if ( !gameIsOn ) {
		}

		// Need to set the values here when screen width etc is known!
		int tmpXS = (int)((float)Screen.width) > kLogoWidth ? kLogoWidth : (int)((float)Screen.width);
		int tmpYS = (int)((float)((float)kLogoHeight/(float)kLogoWidth)*tmpXS);
		float logoYmax = (float)(Screen.height/2-kConnectHeight/2 - (tmpYS));

		// Draw chat/command window
		if ( bChatVisible ) {
			chatRect = new Rect( 0, Screen.height - kChatHeight, Screen.width, kChatHeight );
			chatRect = GUILayout.Window( (int)WindowIDs.CHAT, chatRect, HandleControls, "Chat" ); 
		}
		else
		{
			chatRectHidden = new Rect( 0, Screen.height - kChatHeightHidden, kChatWidthHidden, kChatHeightHidden );
			chatRectHidden = GUILayout.Window( (int)WindowIDs.CHAT, chatRectHidden, HandleControls, "Chat" ); 
		}
		
		if ( !gameIsOn )
		{
			// Draw logo
			if ( !logoTexture ) {
				Debug.LogError ( "Assign a logoTexture in inspector!" );
			}
			else
			{
				curTime += Time.deltaTime;
				
				// accelerate logo y-position until acceleration speed is too low
				if ( bLogoAnimation )
				{
					logoYspeed += 9.81f * Time.deltaTime; // :-)
					logoYpos += logoYspeed;
					if ( logoYpos > logoYmax ) {
						// invert y-speed and halve
						logoYspeed /= 1.2f;
						if ( logoYspeed < 1.0f ) 
							bLogoAnimation = false;
						logoYspeed = -logoYspeed;
						logoYpos = logoYmax;
					}
				}
				else
					logoYpos = logoYmax;
				
				GUI.DrawTexture(new Rect(Screen.width/2 - tmpXS/2, (int)logoYpos, tmpXS, tmpYS), logoTexture, ScaleMode.ScaleToFit, true, kLogoWidth/kLogoHeight);
			}

			// Draw menu
			connectRect = new Rect( Screen.width/2-kConnectWidth/2, Screen.height/2+kConnectHeight/2, kConnectWidth, kConnectHeight );
			connectRect = GUILayout.Window( (int)WindowIDs.MENU, connectRect, HandleControls, "Menu" );
		}
		else
		{
			// Draw play controls
			controlRect = new Rect( 0, 0, 50, 25 );
			controlRect = GUILayout.Window( (int)WindowIDs.CONTROL, controlRect, HandleControls, "Controls" );
		}
			
	}
	
	void HandleControls( int windowId )
	{
		switch ( windowId ) {
			
			case (int)WindowIDs.CHAT:
				// Draw the console window
				GUILayout.BeginVertical( );
				{
					if ( bChatVisible ) {
						Color tmp = GUI.color;
						foreach ( string msg in chatQueue ) {
							if ( msg.Contains("[INFO]") )
								GUI.skin.label.normal.textColor = Color.green;
							else if ( msg.Contains ("[ERROR]" ) )
								GUI.skin.label.normal.textColor = Color.red;
							else if ( msg.Contains ("[CMD]" ) )
								GUI.skin.label.normal.textColor = Color.grey;
							else if ( msg.Contains ("[RPC]" ) )
								GUI.skin.label.normal.textColor = Color.yellow;
							else if ( msg.Contains ("[Message]" ) )
								GUI.skin.label.normal.textColor = Color.magenta;
							else
								GUI.skin.label.normal.textColor = Color.cyan;
							GUILayout.Label( msg );
						}
						GUI.color = tmp;
						GUILayout.BeginHorizontal();
						{
							strChatMessage = GUILayout.TextField( strChatMessage, GUILayout.Width( kChatInputFieldWidth ) );
							if ( GUILayout.Button( "Submit" ) || (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return) )
							{
								// Send msg
								switch ( strChatMessage.ToLower() ) {
									case "/quit":
										chatQueue.Enqueue( "[CMD] " + strChatMessage );
										Application.Quit();
										break;
									case "/disconnect":
										Network.Disconnect();
										break;
									case "/create player":
										chatQueue.Enqueue( "[CMD] " + strChatMessage );
										NetworkInstantiate();
										//BroadcastMessage( "SpawnPlayer", Vector3.zero );
										break;
									default:
										if ( strChatMessage.ToLower().StartsWith("/ip ") )
										{
											// 01234567
											// /ip 12345
											strIP = strChatMessage.Substring(4).Trim();
										}
										else if ( strChatMessage.ToLower ().StartsWith("/port ") )
										{
											// 01234567
											// /port 12345
											strPort = strChatMessage.Substring(6).Trim();
										}
										else
											networkView.RPC( "Chat", RPCMode.All, strChatMessage );
										break;
								}
								// Clear chat field
								strChatMessage = "";
							}
							if ( GUILayout.Button( "Hide chat" ) )
							{
								bChatVisible = false;
							}
						}
						GUILayout.EndHorizontal();
					} else {
						if ( GUILayout.Button( "Show chat" ) )
						{
							bChatVisible = true;
						}
					}
				}
				GUILayout.EndVertical();
				break;
				
			case (int)WindowIDs.MENU:
				GUILayout.BeginVertical();
				{
					GUILayout.BeginHorizontal();
					{
						GUILayout.Label ( "Player name: " );
						playerName = GUILayout.TextField( playerName, GUILayout.Width( kChatInputFieldWidth/2 ) );
					}
					GUILayout.EndHorizontal();
				
					GUILayout.Space ( 10 );
					if ( bHighScoresAvailable )
					{
						if ( GUILayout.Button( "Show highscores" ) )
						{
							int nNum = 1;
							foreach ( HighScoreEntry he in highscores )
							{
								GUILayout.Button( nNum + ". " + he.Name + ", Score: " + he.Score );
								nNum++;
							}
							GUILayout.Space ( 10 );
						}
					}
					else
					{
						GUILayout.Button( "No highscore server available, Sorry." );
					}

					if ( GUILayout.Button( "Start a new game" ) ) {
						LaunchServer();
						//BroadcastMessage( "SpawnPlayer", Vector3.zero );
						//BroadcastMessage( "SpawnDummy", new Vector3(10.0f, 0.0f, 0.0f) );
						//BroadcastMessage( "SpawnPlayer", new Vector3(-10.0f, 0.0f, 0.0f) );
					}
					HostData[] data = MasterServer.PollHostList();
					int nGamesAvailable = 0;
					foreach (HostData element in data)
					{
						GUILayout.BeginVertical( );
						if (GUILayout.Button(nGamesAvailable+1 + ". " + element.gameName.ToUpper() + " available (ip: " + element.ip[0] + "). Click to join!" ) )
						{
							// Connect to HostData struct, internally the correct method is used (GUID when using NAT).
							ConnectToServer(element);
						}
						GUILayout.EndVertical( );
						nGamesAvailable++;
					}		
					string sSeconds = ((int)(16 - (Network.time - fLastPollTime))).ToString();
					if ( nGamesAvailable == 0 )
					{
						//GUILayout.Label( "Refreshing servers list automatically in " + sSeconds.PadLeft(2,'0') + " second(s)" );
						if ( GUILayout.Button ( "Sorry, no joinable games found.\nClick to refresh list now\n(polling automatically in " + sSeconds.PadLeft(2,'0') + "s)" ) ) {
							fLastPollTime = Network.time;
							MasterServer.ClearHostList();
							MasterServer.RequestHostList( kStrGameTypeName );						
						}
					}
					else
					{
						if ( GUILayout.Button ( "Click to refresh list now\n(polling automatically in " + sSeconds.PadLeft(2,'0') + "s)" ) ) {
							fLastPollTime = Network.time;
							MasterServer.ClearHostList();
							MasterServer.RequestHostList( kStrGameTypeName );						
						}
					}
					if ( GUILayout.Button( "Quit" ) ) {
						// Won't work from editor! only from standalone.
						Application.Quit();
					}
				}
				GUILayout.EndVertical();
				break;
				
			case (int)WindowIDs.CONTROL:
				GUILayout.BeginVertical();
				{
					if ( GUILayout.Button("End game") )
					{
						Network.Disconnect();
						// LoadLevel doesn't work here because it would load a new instance of the GUI
						// TODO: Fix loadlevel
						//Application.LoadLevel(0);
					}
	
					/*
					if ( GUILayout.Button("Spawn Dummy") )
					{
						BroadcastMessage("SpawnDummy", new Vector3(10.0f, 0.0f, 0.0f));	
					}
					*/
				}
				GUILayout.EndVertical();
				break;
				
			}
	}
	
		
}
