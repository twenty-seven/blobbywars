using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace BlobWars
{
	/**
	 * This class contains the Blob and 
	 * their specific functionalities. 
	 * Most synchronizing and selection, as well as 
	 * combat is defined here.
	 */
	public class Blob : HealthObject
	{
		// TODO: Should be remove, just used to rotate the blob correctly
		//private Quaternion offset = new Quaternion ();
		// The minimal distance between the current position and destination, to trigger movement.
		private double minDistance;
		// Distance the Blob walks after he is spawned (walking out of the tower)
		private Vector3 stepOut = Vector3.zero;
		// Private placeholder for speed treshold
		protected float nextTime;
		// Synchronization smoothing
		private int lerpRate = 5;


		// Differentiate between a selected Blob and an unselected Blob (navigation)
		public bool isSelected = false;
		// Damage the blob does to other blobs
		public int damage;
		// Range over which the blob can attack other blobs
		public int range;
		// Speed with which it attacks in seconds
		public float attackSpeed;
		// Blob speed
		public int speed;

		//Selection sphere
		private GameObject selectSphere;
		public SlimeAnim slAnim;
		// Unique Blob Id
		public string uid;
		// The rotation of the Object.
		[SyncVar]
		public Quaternion
			syncRot;
		// The Location the Blob is currently traveling towards
		[SyncVar]
		private Vector3
			syncDestination;
		// The Location the Blob is currently at.
		[SyncVar]
		private Vector3
			syncPos;

		// Only sync the name, the object can be fetched later
		[SyncVar]
		public string
			towerName;
		public GameObject tower;

		/// <summary>
		/// The enemy tower.
		/// </summary>
		private GameObject enemyTower;

		/// <summary>
		/// The attack audio.
		/// </summary>
		public AudioClip atkAudio;
		/// <summary>
		/// The move audio.
		/// </summary>
		public AudioClip moveAudio;

		//public GameObject AttackBallPrefab;

		void Awake ()
		{

		}

		/// <summary>
		/// Use this for initialization
		/// </summary>
		public void Start ()
		{
			base.Start ();
			// Set up unique ID, current Healthpoints and the attack speed
			currentHealth = maxHealth;
			nextTime = Time.time + attackSpeed;
			uid = towerName + "." + GetComponent<NetworkIdentity> ().netId.ToString ();
			transform.name = uid;
			//offset.y = .785f;
			slAnim = GetComponent<SlimeAnim> ();
			// Keep them from jumping to 0,0,0
			syncDestination = transform.position;
			// My commanding tower
			tower = GameObject.Find (towerName);
			// The Object steps out of the tower
			if (Vector3.Equals (stepOut, Vector3.zero)) {
				if (tower.transform.position.z < 0) {

					stepOut = Vector3.forward * 100;
				} else {
					stepOut = Vector3.back * 100;
				}
			}


			syncPos = transform.position;
			syncRot = transform.rotation;

			Rpc_initSelector ();

			// Make the object move out of the tower
			MoveTo (transform.position + stepOut);

			Debug.Log ("Spawned Blob");
		}

		/// <summary>
		/// checks for enemies.
		/// </summary>
		[Server]
		protected void CheckForEnemies ()
		{
			if (nextTime < Time.time) { 
				GameObject[] Blobs = GameObject.FindGameObjectsWithTag (tag);
				
				// For each Blob on the field
				for (var d = 0; d  < Blobs.Length; d++) {
					Blob blob = Blobs [d].GetComponent<Blob> ();
					// Skip my own blobs
					if (blob == null || blob.towerName == null) {
						Debug.Log ("Error");
						continue;
					}
					if (blob.towerName == towerName) {
						//continue;
					}
					if (blob.uid == this.uid) {
						continue;
					}
						
					//Debug.Log (this.uid+" - Checking enemy position from " + this.uid + " to " + blob.uid + " " + Vector3.Distance (blob.gameObject.transform.position, this.transform.position));

					// If the blob is in range, attack blob.
					if (Vector3.Distance (blob.gameObject.transform.position, this.transform.position) <= range) {
						Attack (blob.gameObject);
						return;						
					}

				}
				
				//Find enemy Tower
				//NOT TESTED
				if (enemyTower == null) {
					GameObject[] players = GameObject.FindGameObjectsWithTag ("Player");
					for (int i = 0; i < players.Length; i++) {
						if (players [i].name != this.towerName) {
							enemyTower = players [i].GetComponent<Tower> ().gameObject;
						}
					} 
				}

				//Attack Enemy Tower if possible
				if (enemyTower != null) {
					if (Vector3.Distance (enemyTower.transform.position, transform.position) <= range) {
						Attack (enemyTower);
						return;
					}
				}


			}
			
		}
		  

		/// <summary>
		/// Attack the specified enemy.
		/// </summary>
		/// <param name="enemy">The GameObject to attack. Must contain HealthObject.</param>
		[Server]
		protected virtual void Attack (GameObject enemy)
		{
			// TODO: Move closer to enemy blob before attack
			nextTime = Time.time + attackSpeed;
			transform.LookAt (enemy.transform.position);
			
			Rpc_DoAttack ();

			enemy.GetComponent<HealthObject> ().DamageObject (damage);

		}

		/// <summary>
		/// Plays Animation and Sound on Client.
		/// </summary>
		[ClientRpc]
		protected void Rpc_DoAttack()
		{
			slAnim.doAttack = true;	
			aSource.PlayOneShot (atkAudio);
		}


		/// <summary>
		/// Update is called once per frame
		/// </summary>
		protected void Update ()
		{
			// If we're on the server, calculate movement and send it through network
			if (isServer) {
				StepMove ();
				CheckForEnemies ();
			} 
			if(isClient){
				// Else simulate movement of remote objects
				lerpPosition ();
			} 

		}

		// Checks whether it's the movmenet out of the tower and handles the animation.
		// Turns on Walking ANimation and sets new destination, which triggers movement from the update function.
		[Server]
		public void MoveTo (Vector3 location)
		{
			//if (transform.position == tower.transform.position && location == (tower.transform.position + stepOut)) {
			if (Vector3.Distance(transform.position, tower.transform.position)>20){
			    tower.GetComponent<Tower>().Rpc_TriggerCloseDoor();
			}
			syncDestination = location;
		}

		/// <summary>
		/// Make a single step towards the target location.
		/// </summary>
		[Server]
		protected  void StepMove ()
		{
			Vector3 currentPos;
			Animator anim = GetComponent<Animator> ();
			// Use rootPosition of the animator to get a position unbound to the animation
			if (anim != null) {
				currentPos = GetComponent<Animator> ().rootPosition;
				
			} else {
				currentPos = transform.position;
			}

			// If the distance is bigger than range, or I just spawned and have to leave the tower.
			if (Vector3.Distance (transform.position, syncDestination) > minDistance) {
				TriggerWalking(true);
				// TODO: PATHFINDING 
				// Do your own pathfinding here


				Vector3 movement = syncDestination - currentPos;
				// Normalise the movement vector and make it proportional to the speed per second.
				movement = movement.normalized * speed * Time.deltaTime;

				// Move the player to it's current position plus the movement.
				transform.position = currentPos + movement;


				TurnTo (syncDestination);
			} else {			
				// Turn off animation after you finished walking


				TriggerWalking(false);
				transform.position = syncDestination;			//anim.rootPosition = syncDestination;
			}
			// Synchronize Movement
			syncPos = transform.position;
			syncRot = transform.rotation;
		}


		[Server]
		protected void TurnTo (Vector3 point)
		{
			var targetRotation = Quaternion.LookRotation (point - transform.position, Vector3.up);
			//targetRotation.y = offset.y;
			transform.rotation = Quaternion.Slerp (transform.rotation, targetRotation, Time.deltaTime * 2.0f);
		}

		/// <summary>
		/// Triggers Walking
		/// </summary>
		/// <param name="isWalking">If set to <c>true</c> is walking.</param>
		[Server]
		protected void TriggerWalking(bool isWalking) {
			//Play on server for movementCalculation
			slAnim.isWalking = isWalking;
			//Trigger anim on clients
			Rpc_TriggerWalking (isWalking);
		}

		/// <summary>
		/// Plays walkingAnimation and sound on client.
		/// </summary>
		/// <param name="isWalking">If set to <c>true</c> is walking.</param>
		[ClientRpc]
		protected void Rpc_TriggerWalking(bool isWalking) {
			slAnim.isWalking = isWalking;
			if (isWalking) {
				aSource.PlayOneShot (moveAudio);
			}
		}


		/// <summary>
		/// Smooth out movement.
		/// </summary>
		[Client]
		protected void lerpPosition ()
		{
			Animator anim = GetComponent<Animator> ();
			Vector3 currentPos;
			if (anim != null) {
				currentPos = anim.rootPosition;
				
			} else {
				currentPos = transform.position;
			}
			if (Vector3.Distance (currentPos, syncPos) > minDistance) {

				//Debug.Log ("Moving towards " + synPos);
				transform.position = Vector3.Lerp (transform.position, syncPos, Time.deltaTime * lerpRate);
				transform.rotation = Quaternion.Lerp (transform.rotation, syncRot, Time.deltaTime * lerpRate);
			} else {
				transform.position = syncPos;
				//slAnim.isWalking = false;
			}
		}

		protected void AnimationPositionFix ()
		{
			//apply movement done in animation to actual position.
			//transform.position += transform.forward * GetComponent<Animator> ().deltaPosition.magnitude;
			//transform.position += (syncDestination - transform.position).normalized * speed * Mathf.Abs(GetComponent<Animator>().deltaPosition.magnitude);
		}

		/// <summary>
		/// Plays the move audio. trigger this via animation event.
		/// </summary>
		protected void PlayMoveAudio ()
		{
			//play audio for movement once
			aSource.PlayOneShot (moveAudio);
		}

		/// <summary>
		/// Sets selected.
		/// </summary>
		/// <param name="b">If set to <c>true</c> b.</param>
		[Client]
		public void setSelected (bool b)
		{
			Debug.Log (this.uid + "selected");
			isSelected = b;
			selectSphere.GetComponent<Renderer> ().enabled = b;
		}
		
		/// <summary>
		/// Inits the selector.
		/// </summary>
		[ClientRpc]
		private void Rpc_initSelector ()
		{
			//GameObject selectSphere;
			selectSphere = GameObject.CreatePrimitive (PrimitiveType.Plane);
			selectSphere.transform.parent = transform;
			selectSphere.transform.position = transform.position;
			selectSphere.transform.localScale = new Vector3 (0.3f, 0.1f, 0.3f);
			//selectSphere.transform.localScale = new Vector3(50,50,50);
			
			Shader shader = Shader.Find ("Transparent/Diffuse");
			selectSphere.GetComponent<Renderer> ().material.shader = shader;
			selectSphere.GetComponent<Renderer> ().material.color = new Color (1, 100, 1, 0.5f);
			selectSphere.GetComponent<Renderer> ().enabled = false;
		}

	}
}