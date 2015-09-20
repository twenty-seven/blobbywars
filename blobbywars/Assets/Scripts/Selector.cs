﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace BlobWars {
	public class Selector : MonoBehaviour {

		//to allow selection of object in editor
		public GameObject selectObject;
		// The unique ID of the tower the selector belongs to
		public string towerUID;
		// The actual Tower object, gets set automatically by UID
		private  Tower tower;
		// The NetworkIdentity of the tower, used to get the .isLocalPlayer property.
		private NetworkIdentity tNI;
		// The last Object that was selected.
		private GameObject lastSelection;
		
		private bool editorDebug = true;
		// Gets spawned in the Tower.Start() methode.
		void Start () {
			Debug.Log ("Starting Local Selector for: " + towerUID);
			transform.name = towerUID + ".select";
			tower = GameObject.Find (towerUID).GetComponent<Tower> ();
			tNI = tower.GetComponent<NetworkIdentity> ();

			Button selBtn =  GameObject.Find("SelectButton").GetComponent<Button>();
			selBtn.onClick.AddListener (() => {TriggerSelect();});
		}
		
		// 
		/// <summary>
		/// Update is called once per frame.
		/// </summary>
		void Update () {
			// If we're not on the server
			if (tNI.isLocalPlayer) {
				// Move the selector ball correspondigly 
				RaycastHit hit;
				Ray ray;
				if (editorDebug) {
					ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				} else {
					ray = Camera.main.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0)) ;
				}
				//get mouse position on plane and hit
				if (Physics.Raycast (ray, out hit)) {
					Vector3 location = new Vector3 (hit.point.x, hit.point.y, hit.point.z);

					if (hit.transform.GetComponent<Blob>() != null || 
					    hit.transform.GetComponentInParent<Blob>() != null) {
						//Debug.Log (hit.transform.gameObject.GetComponent<Blob>());
						//Debug.Log ("Found something to select");
						location.y += 10;
					}
					transform.position = location;
				}
				//trigger slecetion on click!
				if (editorDebug && Input.GetKey ("mouse 0")) {
					TriggerSelect ();
				}
			}

		}

		/// <summary>
		/// If someone clicked
		/// </summary>
		public void TriggerSelect() {
			Debug.Log("TriggerSelect");
			RaycastHit hit;
			Ray ray;
			if (editorDebug) {
				ray = Camera.main.ScreenPointToRay (Input.mousePosition);
			} else {
				ray = Camera.main.ViewportPointToRay (new Vector3 (0.5F, 0.5F, 0));
			}
			// Throw a ray
			if (Physics.Raycast (ray, out hit)) {
				Blob b = hit.transform.GetComponent<Blob> ();
				// If we hit a Blob
				if (b != null && b.tower != null) {
					// mark it
					if (lastSelection != null) {
						lastSelection.GetComponent<Blob>().setSelected(false);
					}
					lastSelection = b.gameObject;
					b.setSelected(true);
				} else {
					// If we didn't hit a blob and selected another blob already, also no obstacle
					if (null != lastSelection) {

						//move positions to a working height
						Vector3 tmpHeightSelection = lastSelection.transform.position;
						tmpHeightSelection.y = 1;
						Vector3 tmpHitPoint = hit.point;
						tmpHitPoint.y = 1;


						//check for obstacle between blobb and selection
						if(!Physics.Linecast(tmpHeightSelection, tmpHitPoint)) {
							// Send Blob on his way
							tower.CmdTransmitDestination(lastSelection.name, new Vector3(hit.point.x,lastSelection.transform.position.y,hit.point.z));
						}

					}
				}

			}
		}}
}
