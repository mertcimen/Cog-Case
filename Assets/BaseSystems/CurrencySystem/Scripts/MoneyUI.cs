namespace BaseSystems.CurrencySystem.Scripts
{
	public class MoneyUI : CurrencyUI
	{
		protected override void OnEnable()
		{
			Init(CurrencyManager.Money);
			
			base.OnEnable();
		}
	}
}