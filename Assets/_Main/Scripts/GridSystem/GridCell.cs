using _Main.Scripts.BallSystem;
using _Main.Scripts.Datas;
using _Main.Scripts.LevelEditor;
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

		public void Initialize(CellData cellData)
		{
			Coordinate = cellData.coord;
			IsWall = cellData.isWall;
			HasBall = cellData.hasBall;
			
			if (IsWall && HasBall)
				HasBall = false;

			SpawnContent();
			ApplyVisuals();
		}

		private void SpawnContent()
		{
			// // Önce varsa eskileri temizle (re-init senaryosunda önemli)
			// for (int i = transform.childCount - 1; i >= 0; i--)
			// 	Destroy(transform.GetChild(i).gameObject);

			currentBall = null;

			if (HasBall)
			{
				var ballPrefab = ReferenceManagerSO.Instance.BallPrefab;
				var ball = Instantiate(ballPrefab, transform);
				ball.transform.localPosition = Vector3.zero;
				ball.transform.localRotation = Quaternion.identity;

				currentBall = ball;

				// Ball’a currentCell verelim
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

		private void ApplyVisuals()
		{
			
		}

		public void SetPainted(bool painted)
		{
			if (paintSprite != null)
				paintSprite.enabled = painted;
		}
	}
}