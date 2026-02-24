using _Main.Scripts.Datas;
using _Main.Scripts.InputSystem;
using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallRotateController : MonoBehaviour
	{
		[SerializeField] private Transform ballModel;

		private BallController ballController;
		private float rotatePerStep;
		private bool isRotating;
		private Vector3 worldAxis;
		private float remainingAngle;     
		private float angularSpeed;      

		public void Initialize(BallController ballController)
		{
			this.ballController = ballController;
			rotatePerStep = ReferenceManagerSO.Instance.GameParameters.GetRotatePerStep();
		}

		private void Update()
		{
			if (!isRotating || ballModel == null)
				return;

			float delta = angularSpeed * Time.deltaTime;

			if (delta > remainingAngle)
				delta = remainingAngle;

			ballModel.Rotate(worldAxis, delta, Space.World);

			remainingAngle -= delta;

			if (remainingAngle <= 0f)
				isRotating = false;
		}

		public void StartRotate(SwipeDirection swipeDirection, float totalDuration, int steps)
		{
			if (ballModel == null || steps <= 0)
				return;

			// Direction -> Axis
			switch (swipeDirection)
			{
				case SwipeDirection.Right:
					worldAxis = -Vector3.forward;
					break;

				case SwipeDirection.Left:
					worldAxis = Vector3.forward;
					break;

				case SwipeDirection.Up:
					worldAxis = Vector3.right;
					break;

				case SwipeDirection.Down:
					worldAxis = -Vector3.right;
					break;
			}

			//  90 degree per step
			float totalAngle = steps * rotatePerStep;

			remainingAngle = totalAngle;

			// moveDuration 
			angularSpeed = totalAngle / Mathf.Max(0.0001f, totalDuration);

			isRotating = true;
		}

		public void StopRotate()
		{
			isRotating = false;
			remainingAngle = 0f;
		}
	}
}