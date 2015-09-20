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

		/// <summary>
		/// Attack the specified enemy.
		/// </summary>
		/// <param name="enemy">The GameObject to attack. Must contain HealthObject.</param>
		[Server]
		protected override void Attack (GameObject enemy)
		{
			// TODO: Move closer to enemy blob before attack
			nextTime = Time.time + attackSpeed;
			transform.LookAt (enemy.transform.position);
			
			Rpc_DoAttack ();

			MoveTo (transform.position);
			GameObject attackBall = (GameObject)Instantiate (AttackBallPrefab, transform.position, transform.rotation);
				
			attackBall.GetComponent<AttackBall> ().Attack (enemy.transform.position, this.damage, this.uid);

			NetworkServer.Spawn (attackBall);
			
		}
	}
}