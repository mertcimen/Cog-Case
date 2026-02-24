using System;
using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.GridSystem;
using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallMovementController : MonoBehaviour
	{
		private BallController ballController;

		[Header("Movement")]
		[SerializeField] private float moveSpeed = 8f;
		[SerializeField] private float minStepDuration = 0.02f;

		private bool isMoving;
		public bool IsMoving => isMoving;

		public void Initialize(BallController controller)
		{
			ballController = controller;
		}

		public void MoveAlongPath(List<Vector2Int> path, Func<Vector2Int, GridCell> coordToCell, GridCell fromCell,
			GridCell toCell, Action onComplete)
		{
			if (isMoving) return;

			if (path == null || path.Count <= 1)
			{
				onComplete?.Invoke();
				return;
			}

			if (coordToCell == null)
			{
				Debug.LogError("BallMovementController: coordToCell is null.");
				onComplete?.Invoke();
				return;
			}

			if (moveSpeed <= 0f)
			{
				Debug.LogError("BallMovementController: moveSpeed must be > 0.");
				onComplete?.Invoke();
				return;
			}

			StartCoroutine(MoveRoutine(path, coordToCell, fromCell, toCell, onComplete));
		}

		private IEnumerator MoveRoutine(List<Vector2Int> path, Func<Vector2Int, GridCell> coordToCell,
			GridCell fromCell, GridCell toCell, Action onComplete)
		{
			isMoving = true;

			// Cleared For Start 
			ballController.DetachFromCell(); 
			{
				GridCell startCell = coordToCell(path[0]);
				if (startCell != null)
				{
					startCell.Paint();
					transform.position = startCell.transform.position;
				}
			}

			for (int i = 1; i < path.Count; i++)
			{
				GridCell cell = coordToCell(path[i]);
				if (cell == null)
					continue;

				 
				cell.Paint();
				cell.TryCollectCoin(transform.position);
				
				Vector3 start = transform.position;
				Vector3 end = cell.transform.position;

				float distance = Vector3.Distance(start, end);
				float duration = Mathf.Max(minStepDuration, distance / moveSpeed);

				float t = 0f;
				while (t < 1f)
				{
					t += Time.deltaTime / duration;
					transform.position = Vector3.Lerp(start, end, t);
					yield return null;
				}

				transform.position = end;
			}

			// Hareket bitti: hedef hücreye kendini ata
			ballController.AttachToCell(toCell);

			isMoving = false;
			onComplete?.Invoke();
		}
	}
}