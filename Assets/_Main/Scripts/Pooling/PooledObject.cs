using UnityEngine;

namespace _Main.Scripts.Pooling
{
	
	[DisallowMultipleComponent]
	public sealed class PooledObject : MonoBehaviour
	{
		public int PoolId { get; private set; }
		public PoolManager Owner { get; private set; }

		public void Setup(PoolManager owner, int poolId)
		{
			Owner = owner;
			PoolId = poolId;
		}
	}
}