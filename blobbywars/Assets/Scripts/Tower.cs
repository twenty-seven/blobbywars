using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BlobWars
{
	public class Tower : HealthObject
	{
		// Soldier Prefabs that are used to spawn soldiers
		public GameObject FighterPrefab, RangedPrefab, ArtilleryPrefab, SelectorPrefab;
		private GameObject selector;
		public string uid;
		// Maximum Number of Soldiers
		public int maxSoldiers = 1;
		// Current Number of Soldiers
		[SyncVar]
		public int numSoldiers = 0;

		private float spawnTime;
		public float spawnDelay = 5f;

		public const int fighter = 0, ranged = 1, artillery = 2;
		public const int blobTypes = 2;
		//TODO: remove this var when done testing
		private int soldierTypeTester = 0;

		//Audio
		public AudioClip openDoorAudio;
		void Start ()
		{
			base.Start ();
			currentHealth = maxHealth;
			// Create 'unique' name for tower
			uid = "Player" + GetComponent<NetworkIdentity> ().netId;
			gameObject.transform.name = uid;
			spawnTime = Time.time + spawnDelay;
			if (isLocalPlayer) {
				Debug.Log ("Local Tower Spawned." + transform.name);
				selector = (GameObject)Instantiate (SelectorPrefab, transform.position, Quaternion.identity);
				selector.GetComponent<Selector> ().towerUID = transform.name;

				GameObject imgTarget = GameObject.Find ("ImageTarget");
				if (imgTarget != null) {
					selector.transform.parent = imgTarget.transform;
				}

				
				GameObject selectBtn = GameObject.Find ("selBtn");
				if (selectBtn != null) {
					selectBtn.GetComponent<Button> ().interactable = true;
					// add selection trigger function to the button.
					selectBtn.GetComponent<Button> ().onClick.AddListener (() => {
						selector.GetComponent<Selector> ().TriggerSelect ();
					});
				}
			}
		}
		

		// Update is called once per frame
		[ServerCallback]
		void Update ()
		{
			//TODO: change to actual spawning behaviour ... this is for testing.
			//spawn soldier when there is space ... go through types :)
			if (isServer) {
				if (numSoldiers < maxSoldiers && Time.time > spawnTime) {

					soldierTypeTester++;
					if (soldierTypeTester > blobTypes) {
						soldierTypeTester = 0;
					}

					CmdSpawnSoldier (soldierTypeTester, transform.position);
				}
			}
		}

		/// <summary>
		/// Spawns a soldier on the server
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="location">Location.</param>
		[Command]
		public void CmdSpawnSoldier (int type, Vector3 location)
		{
			string blobName = uid + "." + 0;
			for (int i = 0; i < maxSoldiers*5; i++) {
				blobName = uid + "." + i;
				if (GameObject.Find (blobName) == null) {
					break;
				}
			}

			// Create, name and spawn the object on the server
			GameObject prefab = null;
			switch (type) {
			case fighter:
				prefab = FighterPrefab;
				break;
			case ranged:
				prefab = RangedPrefab;
				break;
			case artillery: 
				prefab = ArtilleryPrefab;
				break;
			default:
				prefab = FighterPrefab;
				break;
			}

			GameObject blob = (GameObject)Instantiate (prefab, location, Quaternion.identity);

			blob.GetComponent<Blob> ().towerName = uid;
				
			GameObject imgTarget = GameObject.Find ("ImageTarget");
			if (imgTarget != null) {
				blob.transform.SetParent (imgTarget.transform);
			}

			Debug.Log ("Spawning Soldier on Server");
			Debug.Log ("Spawning " + blobName + " of tower " + uid + " at " + location);
			//blobname Unsinn, blobname!=Blob.uid

			NetworkServer.Spawn (blob);

			spawnTime = Time.time + spawnDelay;
			numSoldiers++;

			Rpc_TriggerDoor ();

		}

		/// <summary>
		/// Triggers door-opening.
		/// </summary>
		[ClientRpc]
		void Rpc_TriggerDoor ()
		{
			GetComponent<TowerAnim> ().doorsOpen = true;
		}

		/// <summary>
		/// Triggers door-opening.
		/// </summary>
		[ClientRpc]
		public void Rpc_TriggerCloseDoor ()
		{
			GetComponent<TowerAnim> ().doorsOpen = false;
		}


		// In case a blob changes it's destination, the tower is used to 
		// inform the Server about the changes
		[Client]
		public void TransmitDestination (string blobName, Vector3 destination)
		{
			CmdTransmitDestination (blobName, destination);
		}


		// Server gets changed object an set different location
		[Command]
		public void CmdTransmitDestination (string blobName, Vector3 destination)
		{
			GameObject.Find (blobName).GetComponent<Blob> ().MoveTo (destination);
		}
	}
}
