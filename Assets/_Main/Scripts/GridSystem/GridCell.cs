using _Main.Scripts.BallSystem;
using _Main.Scripts.Datas;
using _Main.Scripts.LevelEditor;
using DG.Tweening;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridCell : MonoBehaviour
	{
		public Vector2Int Coordinate { get; private set; }
		public bool IsWall { get; private set; }
		public bool HasBall { get; private set; }
		public BallController CurrentBall => currentBall;

		[SerializeField] private SpriteRenderer paintSprite;

		private BallController currentBall;
		private GridManager gridManager;

		private bool isPainted;

		public void Initialize(CellData cellData, GridManager owner)
		{
			gridManager = owner;

			Coordinate = cellData.coord;
			IsWall = cellData.isWall;
			HasBall = cellData.hasBall;

			if (IsWall && HasBall)
				HasBall = false;

			isPainted = false;

			SpawnContent();
		}

		private void SpawnContent()
		{
			currentBall = null;
			if (HasBall)
			{
				var ballPrefab = ReferenceManagerSO.Instance.BallPrefab;
				var ball = Instantiate(ballPrefab, transform);
				ball.transform.localPosition = Vector3.zero;
				ball.transform.localRotation = Quaternion.identity;

				currentBall = ball;
				currentBall.Initialize(this);
			}
			else if (IsWall)
			{
				var wallPrefab = ReferenceManagerSO.Instance.WallPrefab;
				var wall = Instantiate(wallPrefab, transform);
				wall.transform.localPosition = Vector3.zero;
				wall.transform.localRotation = Quaternion.identity;
			}
		}

		public void Paint()
		{
			if (IsWall) return;
			if (isPainted) return;

			isPainted = true;

			if (paintSprite != null)
			{
				paintSprite.enabled = true;
				paintSprite.DOColor(Color.red, 0.4f);
			}

			gridManager?.IncreaseCurrentPaintedCount();
		}

		public void ClearBallReference()
		{
			currentBall = null;
			HasBall = false;
		}

		public void SetBallReference(BallController ball)
		{
			currentBall = ball;
			HasBall = ball != null;
		}
	}
}