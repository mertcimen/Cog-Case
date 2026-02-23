using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallMovementController : MonoBehaviour
	{
		private BallController ballController;

		public void Initialize(BallController controller)
		{
			ballController = controller;
		}
	}
}