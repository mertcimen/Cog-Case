using _Main.Scripts.GridSystem;
using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallController : MonoBehaviour
	{
		public GridCell CurrentCell { get; private set; }

		[SerializeField] private BallMovementController movementController;
		public BallMovementController MovementController => movementController;

		public void Initialize(GridCell cell)
		{
			if (movementController == null)
				movementController = GetComponent<BallMovementController>();

			SetCurrentCell(cell);

			if (movementController == null)
			{
				Debug.LogError("BallController: BallMovementController missing on ball prefab.");
				return;
			}

			movementController.Initialize(this);
		}

		public void SetCurrentCell(GridCell cell)
		{
			CurrentCell = cell;
		}

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

			// güvenlik: tam hücre merkezine snap
			transform.position = targetCell.transform.position;
		}
	}
}