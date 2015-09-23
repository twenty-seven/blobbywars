using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace BlobWars
{
	public class ArtilleryBlob : Blob
	{
		/// <summary>
		/// The attack ball prefab.
		/// </summary>
		public GameObject AttackBallPrefab;

		public int DamageRadius = 20;

		private GameObject lastAttacked;

		/// <summary>
		/// Attack the specified enemy.
		/// </summary>
		/// <param name="enemy">The GameObject to attack. Must contain HealthObject.</param>
		[Server]
		protected override void Attack (GameObject enemy)
		{
			lastAttacked = enemy;

			// TODO: Move closer to enemy blob before attack
			nextAttackTime = Time.time + attackSpeed;
			transform.LookAt (enemy.transform.position);
			
			Rpc_DoAttack ();

			MoveTo (transform.position);

			Vector3 attackBallPosition = new Vector3 (transform.position.x,transform.position.y+10,transform.position.z);

			GameObject attackBall = (GameObject)Instantiate (AttackBallPrefab, attackBallPosition, transform.rotation);


			Vector3 attackBallDest= new Vector3 (enemy.transform.position.x,enemy.transform.position.y+10,enemy.transform.position.z);
			attackBall.GetComponent<AttackBall> ().Attack (attackBallDest, this.damage, this.DamageRadius, this.tag);

			NetworkServer.Spawn (attackBall);
		}

		/// <summary>
		/// Selects the enemy to attack.
		/// Remembers last-attacked Enemy.
		/// </summary>
		/// <returns>The enemy to attack.</returns>
		[Server]
		protected override GameObject SelectEnemyToAttack() {
			if (lastAttacked != null 
			    && lastAttacked.GetComponent<HealthObject>().isAlive() 
			    && Vector3.Distance (lastAttacked.transform.position, this.transform.position) <= range) {
				return lastAttacked;
			}

			GameObject[] Blobs = GameObject.FindGameObjectsWithTag (this.tag);
			
			// For each Blob on the field
			for (var d = 0; d  < Blobs.Length; d++) {
				Blob blob = Blobs [d].GetComponent<Blob> ();
				// Skip my own blobs
				if (blob == null || blob.towerUID == null) {
					Debug.Log ("Error");
					continue;
				}
				if (blob.towerUID == towerUID) {
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

	}
}