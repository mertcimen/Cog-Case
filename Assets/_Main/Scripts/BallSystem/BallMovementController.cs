using System;
using System.Collections;
using System.Collections.Generic;
using _Main.Scripts.Datas;
using _Main.Scripts.GridSystem;
using BaseSystems.AudioSystem.Scripts;
using _Main.Scripts.InputSystem;
using UnityEngine;

namespace _Main.Scripts.BallSystem
{
	public class BallMovementController : MonoBehaviour
	{
		private BallController ballController;

		[Header("Movement")]
		[SerializeField] private float moveDuration = 0.35f;

		private bool isMoving;
		public bool IsMoving => isMoving;

		public void Initialize(BallController controller)
		{
			ballController = controller;
			moveDuration = ReferenceManagerSO.Instance.GameParameters.GetBallMoveDuration();
		}

		public void MoveAlongPath(List<Vector2Int> path, Func<Vector2Int, GridCell> coordToCell, GridCell fromCell,
			GridCell toCell, SwipeDirection swipeDirection, Action onComplete)
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

			if (moveDuration <= 0f)
			{
				Debug.LogError("BallMovementController: moveDuration must be > 0.");
				onComplete?.Invoke();
				return;
			}

			StartCoroutine(MoveRoutine(path, coordToCell, toCell, swipeDirection, onComplete));
		}

		private IEnumerator MoveRoutine(List<Vector2Int> path, Func<Vector2Int, GridCell> coordToCell, GridCell toCell,
			SwipeDirection swipeDirection, Action onComplete)
		{
			isMoving = true;
			ballController.KillBounceEffect();
			ballController.DetachFromCell();

			// Start cell paint + snap
			GridCell startCell = coordToCell(path[0]);
			if (startCell != null)
			{
				startCell.Paint();
				startCell.TryCollectCoin(transform.position);
				transform.position = startCell.transform.position;
			}

			int steps = path.Count - 1;

			if (ballController != null && ballController.BallRotateController != null && steps > 0)
			{
				ballController.BallRotateController.StartRotate(swipeDirection, moveDuration, steps);
			}

			if (steps > 0)
			{
				float stepDuration = moveDuration / steps;

				for (int i = 1; i < path.Count; i++)
				{
					GridCell cell = coordToCell(path[i]);
					if (cell == null) continue;

					cell.Paint();
					cell.TryCollectCoin(transform.position);

					Vector3 start = transform.position;
					Vector3 end = cell.transform.position;

					float t = 0f;
					while (t < 1f)
					{
						t += Time.deltaTime / Mathf.Max(0.0001f, stepDuration);
						transform.position = Vector3.Lerp(start, end, t);
						yield return null;
					}

					transform.position = end;
				}
			}

			Finish(toCell, onComplete);
		}

		private void Finish(GridCell toCell, Action onComplete)
		{
			AudioManager.Instance.PlayAudio(AudioName.BallHit);
			ballController.HitBounceEffect();
			// Stop rotate
			if (ballController != null && ballController.BallRotateController != null)
				ballController.BallRotateController.StopRotate();

			ballController.AttachToCell(toCell);

			isMoving = false;
			onComplete?.Invoke();
		}
	}
}