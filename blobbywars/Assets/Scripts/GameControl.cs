using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BlobWars {
	public class GameControl : NetworkBehaviour {

		public GameObject winUI, controls;
		private Text winText;
		private bool gameStarted = false;
		
		// Use this for initialization
		void Start () {
			winText = winUI.transform.Find ("WinText").GetComponent<Text>();
		}
		// Update is called once per frame
		void Update () {

			if (CheckWinner ()) {
				WinGame ();
			}
		}

		private bool CheckWinner() {
			GameObject[] winners = GameObject.FindGameObjectsWithTag ("Player");
			// avoid triggering winner when only one player is connected at the beginning
			if (!gameStarted && winners.Length > 1) {
				gameStarted = true;
			} else if (!gameStarted) {
				return false;
			}
			return (winners.Length == 1);
		}


		public void WinGame(){
			GameObject winner = GameObject.FindGameObjectWithTag ("Player");
			Tower tWinner = winner.GetComponent<Tower> ();
			Debug.Log (tWinner.isLocalPlayer);
			if (tWinner.isLocalPlayer) {
					winText.text = "VICTORY!";
					winText.color = Color.green;
			} else {
					winText.text = "DEFEAT!";
					winText.color = Color.red;
			}
			
			winUI.SetActive (true);
			controls.SetActive (false);
		}

		public void QuitGame() {
			NetworkManager nm = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
			if (nm.isNetworkActive) {
				if(Network.isClient) {
					nm.StopClient();
				} else {
					nm.StopHost();
				}
			} else {
				Application.Quit ();
			}
		}
	}
}