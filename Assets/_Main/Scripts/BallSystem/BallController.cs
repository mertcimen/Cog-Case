using _Main.Scripts.GridSystem;
using DG.Tweening;
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

		private Tween bounceTween;

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

		private Tween squishTween;

		public void HitBounceEffect()
		{
			KillBounceEffect();

			// Bounce (position)
			bounceTween = transform.DOJump(transform.position, 0.5f, 1, 0.2f).SetEase(Ease.OutBounce)
				.OnComplete(() => { bounceTween = null; });

			Vector3 originalScale = Vector3.one;

			squishTween = DOTween.Sequence().Append(transform.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.08f))
				.Append(transform.DOScale(new Vector3(0.9f, 1.1f, 1f), 0.08f))
				.Append(transform.DOScale(originalScale, 0.1f)).SetEase(Ease.OutQuad)
				.OnComplete(() => { squishTween = null; });
		}

		public void KillBounceEffect()
		{
			if (bounceTween != null)
			{
				bounceTween.Kill();
				bounceTween = null;
			}

			if (squishTween != null)
			{
				squishTween.Kill();
				squishTween = null;
			}

			transform.localScale = Vector3.one;
		}
	}
}