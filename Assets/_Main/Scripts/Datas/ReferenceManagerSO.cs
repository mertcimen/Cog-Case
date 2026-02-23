using _Main.Scripts.BallSystem;
using _Main.Scripts.WallSystem;
using UnityEngine;

namespace _Main.Scripts.Datas
{
	[CreateAssetMenu(fileName = "ReferenceManagerSO", menuName = "Data/Reference ManagerSO")]
	public class ReferenceManagerSO : ScriptableObject
	{
		private static ReferenceManagerSO _instance;

		[SerializeField] private BallController ballPrefab;
		[SerializeField] private WallController wallPrefab;

		public BallController BallPrefab => ballPrefab;
		public WallController WallPrefab => wallPrefab;

		public static ReferenceManagerSO Instance
		{
			get
			{
				if (_instance != null) return _instance;

				_instance = Resources.Load<ReferenceManagerSO>("ReferenceManagerSO");
				if (_instance == null)
					Debug.LogError(
						"ReferenceManagerSO not found in Resources. Put ReferenceManagerSO.asset under a Resources folder.");

				return _instance;
			}
		}
	}
}