using System;
using UnityEngine;

namespace Game.Core.Runtime
{
	public class GoalObject : MonoBehaviour
	{
		//player enter the goal trigger
		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.CompareTag("Player"))
			{
				ScenesGameManager.Instance.Goal();
			}
		}
	}
}
