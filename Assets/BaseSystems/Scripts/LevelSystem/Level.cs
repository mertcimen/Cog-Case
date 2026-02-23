using UnityEngine;

namespace BaseSystems.Scripts.LevelSystem
{
	public class Level : MonoBehaviour
	{
		public virtual void Load()
		{
			gameObject.SetActive(true);
			// TimeManager.Instance.Initialize(46);
		}

		public virtual void Play()
		{
		}
	}
}