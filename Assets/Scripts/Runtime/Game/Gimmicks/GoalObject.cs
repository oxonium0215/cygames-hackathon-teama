using UnityEngine;
using Game.Core;

namespace Game.Gimmicks
{
	public class GoalObject : MonoBehaviour
	{
		//player enter the goal trigger
		private void OnTriggerEnter(Collider other)
		{
			if (other.gameObject.CompareTag("Player"))
			{
				// Call the Goal method directly through the Singleton instance
				ScenesGameManager.Instance.Goal();
			}
		}
	}
}
