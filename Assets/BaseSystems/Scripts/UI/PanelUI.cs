using Sirenix.OdinInspector;

namespace BaseSystems.Scripts.UI
{
	public abstract class PanelUI : SerializedMonoBehaviour
	{
		public virtual void Open()
		{
			gameObject.SetActive(true);
		}

		public virtual void Close()
		{
			gameObject.SetActive(false);
		}
	}
}