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

		/// <summary>
		/// minimal distance for linear movement. if distance between current pos and destination is lower, blob 'jumps'.
		/// </summary>
		private double minDistance=0.1;
		/// <summary>
		/// Distancd for blob to move after spawning in tower.
		/// </summary>
		private Vector3 stepOut = Vector3.zero;
		/// <summary>
		/// Time for next attacking.
		/// </summary>
		protected float nextAttackTime;
		/// <summary>
		/// Synchronization smoothing.
		/// </summary>
		private int lerpRate = 5;
		/// <summary>
		/// Damage the blob does to other blobs and tower
		/// </summary>
		public int damage;
		/// <summary>
		/// Range over which the blob can attack other blobs and tower.
		/// </summary>
		public int range;
		/// <summary>
		/// Cooldown after attack.
		/// </summary>
		public float attackSpeed;
		// Blob speed
		/// <summary>
		/// Movement speed.
		/// </summary>
		public int speed;

		/// <summary>
		/// Time to wait to close doors. 
		/// </summary>
		public float doorOpenSeconds = 2.5;

		/// <summary>
		/// Differentiate between a selected Blob and an unselected Blob (navigation).
		/// </summary>
		public bool isSelected = false;
		/// <summary>
		/// Selection sphere.
		/// </summary>
		private GameObject selectSphere;
		/// <summary>
		/// The slime animation.
		/// </summary>
		public SlimeAnim slAnim;
		/// <summary>
		/// Unique Blob Id.
		/// </summary>
		public string uid;

		/// <summary>
		/// Blob Rotation.
		/// </summary>
		[SyncVar]
		public Quaternion
			syncRot;
		/// <summary>
		/// The Location the Blob is currently traveling towards.
		/// </summary>
		[SyncVar]
		private Vector3
			syncDestination;
		/// <summary>
		/// The Location the Blob is currently at.
		/// </summary>
		[SyncVar]
		private Vector3
			syncPos;

		/// <summary>
		/// Tower UID.
		/// </summary>
		[SyncVar]
		public string
			towerUID;
		/// <summary>
		/// own tower.
		/// </summary>
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

		/// <summary>
		/// Used for debugging, if true, every blobs, not only enemys, are attacked.
		/// </summary>
		private bool debug = false;

		void Awake ()
		{

		}

		/// <summary>
		/// Use this for initialization
		/// </summary>
		public void Start ()
		{
			base.Start ();
			// set attackTime
			nextAttackTime = Time.time + attackSpeed;
			// set Tower and UID
			tower = GameObject.Find (towerUID);
			uid = towerUID + "." + GetComponent<NetworkIdentity> ().netId.ToString ();
			transform.name = uid;

			//get animator
			slAnim = GetComponent<SlimeAnim> ();

			// Keep them from jumping to 0,0,0
			syncDestination = transform.position;
			syncPos = transform.position;
			syncRot = transform.rotation;

			// The Object steps out of the tower
			if (Vector3.Equals (stepOut, Vector3.zero)) {
				if (tower.transform.position.z < 0) {

					stepOut = Vector3.forward * 100;
				} else {
					stepOut = Vector3.back * 100;
				}
			}

			// Make the object move out of the tower
			MoveTo (transform.position + stepOut);

			//Init selector on clients
			Rpc_initSelector ();
			Rpc_TriggerCloseDoors ();
		}


		/// <summary>
		/// Update is called once per frame
		/// </summary>
		protected void Update ()
		{
			// If we're on the server, calculate movement and send it through network
			if (isServer) {
				FindEnemyTower();
				StepMove ();
				CheckForEnemies ();
			} 
			if(isClient){
				// Else simulate movement of remote objects
				lerpPosition ();
			} 
			
		}

		/// <summary>
		/// checks for enemies.
		/// </summary>
		[Server]
		private void CheckForEnemies ()
		{
			if (nextAttackTime < Time.time) { 

				GameObject enemy = SelectEnemyToAttack();
				if(enemy!=null){
					Attack(enemy);
				}
			}
			
		}
		  
		protected virtual GameObject SelectEnemyToAttack() {
			GameObject[] Blobs = GameObject.FindGameObjectsWithTag (this.tag);
			
			// For each Blob on the field
			for (var d = 0; d  < Blobs.Length; d++) {
				Blob blob = Blobs [d].GetComponent<Blob> ();
				// Skip my own blobs
				if (blob == null || blob.towerUID == null) {
					Debug.Log ("Error");
					continue;
				}
				if (blob.towerUID == towerUID && !debug) {
					continue;
				}
				if (blob.uid == this.uid) {
					continue;
				}
				
				// If the blob is in range, attack blob.
				if (Vector3.Distance (blob.gameObject.transform.position, this.transform.position) <= range) {
					return blob.gameObject;						
				}
				
			}
			
			//Attack Enemy Tower if possible
			if (enemyTower != null) {
				if (Vector3.Distance (enemyTower.transform.position, transform.position) <= range) {
					return enemyTower;
				}
			}
			return null;
		}
		
		
		/// <summary>
		/// Attack the specified enemy.
		/// </summary>
		/// <param name="enemy">The GameObject to attack. Must contain HealthObject.</param>
		[Server]
		protected virtual void Attack (GameObject enemy)
		{
			// TODO: Move closer to enemy blob before attack
			nextAttackTime = Time.time + attackSpeed;
			transform.LookAt (enemy.transform.position);
			
			Rpc_DoAttack ();

			enemy.GetComponent<HealthObject> ().DamageObject (damage);
		}

		/// <summary>
		/// Finds the enemy tower.
		/// </summary>
		private void FindEnemyTower() {
			if (enemyTower != null) {
				return;
			}
				
			GameObject[] players = GameObject.FindGameObjectsWithTag ("Player");
			for (int i = 0; i < players.Length; i++) {
				if (players [i].name != this.towerUID) {
					enemyTower = players [i].GetComponent<Tower> ().gameObject;
				}
			} 
			
		}
		
		// Checks whether it's the movmenet out of the tower and handles the animation.
		// Turns on Walking ANimation and sets new destination, which triggers movement from the update function.
		[Server]
		public void MoveTo (Vector3 location)
		{
			syncDestination = location;
		}

		[Server]
		protected void TurnTo (Vector3 point)
		{

			var targetRotation = Quaternion.LookRotation (point - transform.position, Vector3.up);
			//targetRotation.y = offset.y;
			transform.rotation = Quaternion.Slerp (transform.rotation, targetRotation, Time.deltaTime * 2.0f);
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
				slAnim.isWalking = true;
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


				slAnim.isWalking =false;
				transform.position = syncDestination;			//anim.rootPosition = syncDestination;
			}
			// Synchronize Movement
			syncPos = transform.position;
			syncRot = transform.rotation;
		}


		/// <summary>
		/// Smooth out movement.
		/// </summary>
		[Client]
		protected void lerpPosition ()
		{
			Vector3 currentPos;

			Animator anim = GetComponent<Animator> ();

			if (anim != null) {
				currentPos = anim.rootPosition;
			} else {
				currentPos = transform.position;
			}

			if (Vector3.Distance (currentPos, syncPos) > minDistance) {
				TriggerWalking(true);
				transform.position = Vector3.Lerp (transform.position, syncPos, Time.deltaTime * lerpRate);
				transform.rotation = Quaternion.Lerp (transform.rotation, syncRot, Time.deltaTime * lerpRate);
			} else {
				transform.position = syncPos;
				TriggerWalking(false);
			}
		}

		
		/// <summary>
		/// Plays walkingAnimation and sound on client.
		/// </summary>
		/// <param name="isWalking">If set to <c>true</c> is walking.</param>
		[Client]
		protected void TriggerWalking(bool isWalking) {
			slAnim.isWalking = isWalking;
			if (isWalking) {
				aSource.PlayOneShot (moveAudio);
			}
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

		protected void AnimationPositionFix ()
		{
			//apply movement done in animation to actual position.
			//transform.position += transform.forward * GetComponent<Animator> ().deltaPosition.magnitude;
			//transform.position += (syncDestination - transform.position).normalized * speed * Mathf.Abs(GetComponent<Animator>().deltaPosition.magnitude);
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
			isSelected = false;
		}


		[ClientRpc]
		private void Rpc_TriggerCloseDoors() {

			StartCoroutine (WaitWithDoorAnimation ());
		
		}

		[Client]
			IEnumerator WaitWithDoorAnimation() {
					yield return new WaitForSeconds(doorOpenSeconds);
					tower.GetComponent<TowerAnim> ().closeDoors ();
			}



	}
}