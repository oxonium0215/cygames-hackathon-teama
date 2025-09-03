using System;
using UnityEngine;

namespace Game.Gimmicks
{
	public class GoalObject : MonoBehaviour
	{
		public ScenesGameManager MoveScenes;
		//player enter the goal trigger
		private void Start()
		{
			Canvas canvas = GameObject.Find("GameUI").GetComponent<Canvas>();
			MoveScenes = canvas.GetComponent<ScenesGameManager>();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.CompareTag("Player"))
			{
				MoveScenes.Goal();
			}
		}
	}
}
