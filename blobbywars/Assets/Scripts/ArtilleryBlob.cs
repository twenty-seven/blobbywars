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

		/// <summary>
		/// Attack the specified enemy.
		/// </summary>
		/// <param name="enemy">The GameObject to attack. Must contain HealthObject.</param>
		[Server]
		protected override void Attack (GameObject enemy)
		{
			// TODO: Move closer to enemy blob before attack
			nextAttackTime = Time.time + attackSpeed;
			transform.LookAt (enemy.transform.position);
			
			Rpc_DoAttack ();

			MoveTo (transform.position);

			Vector3 attackBallPosition = new Vector3 (transform.position.x,transform.position.y+10,transform.position.z);

			GameObject attackBall = (GameObject)Instantiate (AttackBallPrefab, attackBallPosition, transform.rotation);


			Vector3 attackBallDest= new Vector3 (enemy.transform.position.x,enemy.transform.position.y+10,enemy.transform.position.z);
			attackBall.GetComponent<AttackBall> ().Attack (attackBallDest, this.damage, this.DamageRadius, this.uid, this.tag);

			NetworkServer.Spawn (attackBall);
		}
	}
}