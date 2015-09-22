//------------------------------------------------------------------------------
// <auto-generated>
//     Dieser Code wurde von einem Tool generiert.
//     Laufzeitversion:4.0.30319.18444
//
//     Änderungen an dieser Datei können falsches Verhalten verursachen und gehen verloren, wenn
//     der Code erneut generiert wird.
// </auto-generated>
//------------------------------------------------------------------------------
using System;
using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

namespace BlobWars
{
	public class AttackBall : NetworkBehaviour
	{
		[SyncVar]
		Vector3	destination;

		public float speed = 50;

		private int damage;

		string blobUid;

		string blobTag;
		int damageRadius;

		/// <summary>
		/// Attacks at the specified location with damage.
		/// </summary>
		/// <param name="location">Location.</param>
		/// <param name="damage">Damage.</param>
		[Server]
		public void Attack (Vector3 location, int damage,int damageRadius, string blobUid, string blobTag)
		{
			this.damage = damage;
			this.destination = location;
			this.blobUid = blobUid;
			this.blobTag = blobTag;
			this.damageRadius = damageRadius;
		}

		/// <summary>
		/// Attacks at current Location.
		/// </summary>
		[Server]
		private void Attack() {

			ArrayList objects = new ArrayList ();
			objects.AddRange (GameObject.FindGameObjectsWithTag ("Player"));
			objects.AddRange (GameObject.FindGameObjectsWithTag (blobTag));

			//Allen Schaden zufügen
			foreach (GameObject gameObject in objects) {
				if (Vector3.Distance (gameObject.transform.position, this.transform.position) <= damageRadius) {
					HealthObject healthObject = gameObject.GetComponent<HealthObject> ();
					healthObject.DamageObject(damage);
				}
			}

			NetworkServer.Destroy (this.gameObject);
		}

		[Server]
		private void StepMove ()
		{
			//TODO: flugkurve?
			Vector3 movement = destination - transform.position;
			// Normalise the movement vector and make it proportional to the speed per second.
			movement = movement.normalized * speed * Time.deltaTime;

			if (movement.sqrMagnitude < (destination - transform.position).sqrMagnitude) {
				transform.position = transform.position + movement;
			} else {
				transform.position = destination;
				Attack ();
			}

		}

		void Update ()
		{
			// If we're on the server, calculate movement and send it through network
			if (isServer) {
				StepMove ();
			}
		}

	}
}

