using _Main.Scripts.GridSystem;
using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallController : MonoBehaviour
	{
		public GridCell CurrentCell { get; private set; }

		[SerializeField] private BallMovementController movementController;
		[SerializeField] private BallRotateController ballRotateController;

		public BallMovementController MovementController => movementController;
		public BallRotateController BallRotateController => ballRotateController;

		public void Initialize(GridCell cell)
		{
			if (movementController == null)
				movementController = GetComponent<BallMovementController>();

			if (ballRotateController == null)
				ballRotateController = GetComponent<BallRotateController>();

			SetCurrentCell(cell);

			if (movementController == null)
			{
				Debug.LogError("BallController: BallMovementController missing on ball prefab.");
				return;
			}

			movementController.Initialize(this);

			// NEW
			if (ballRotateController != null)
				ballRotateController.Initialize(this);
		}

		public void SetCurrentCell(GridCell cell) => CurrentCell = cell;

		public void DetachFromCell()
		{
			if (CurrentCell == null) return;
			CurrentCell.ClearBallReference();
			CurrentCell = null;
		}

		public void AttachToCell(GridCell targetCell)
		{
			if (targetCell == null) return;

			targetCell.SetBallReference(this);
			CurrentCell = targetCell;
			transform.position = targetCell.transform.position;
		}
	}
}