using _Main.Scripts.LevelEditor;
using UnityEngine;

namespace _Main.Scripts.GridSystem
{
	public class GridCell : MonoBehaviour
	{
		public Vector2Int Coordinate { get; private set; }
		public bool IsWall { get; private set; }
		public bool HasBall { get; private set; }

		public void Initialize(CellData cellData)
		{
			Coordinate = cellData.coord;
			IsWall = cellData.isWall;
			HasBall = cellData.hasBall;

			
			ApplyVisuals();
		}

		private void ApplyVisuals()
		{
			// örnek: duvarsa farklı görünüm
			// (renderer/material logic senin projeye göre)
		}
	}
}