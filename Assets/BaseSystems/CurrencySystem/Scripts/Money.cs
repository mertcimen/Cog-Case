using BaseSystems.Scripts.Utilities;
using UnityEngine;

namespace BaseSystems.CurrencySystem.Scripts
{
	/// <summary>
	/// Soft currency
	/// </summary>
	public class Money : Currency
	{
		public override long Amount
		{
			get => PlayerPrefs.GetInt(PlayerPrefsNames.MONEY, 0);
			set => PlayerPrefs.SetInt(PlayerPrefsNames.MONEY, (int)value);
		}
	}
}