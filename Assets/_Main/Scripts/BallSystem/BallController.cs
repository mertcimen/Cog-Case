using _Main.Scripts.GridSystem;
using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallController : MonoBehaviour
	{
		public GridCell CurrentCell { get; private set; }

		[SerializeField] private BallMovementController movementController;

		public void SetCurrentCell(GridCell cell)
		{
			CurrentCell = cell;
		}

		public void Initialize( GridCell cell )
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
	}
}