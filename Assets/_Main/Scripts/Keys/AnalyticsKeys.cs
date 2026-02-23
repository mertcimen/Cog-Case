using System.Collections.Generic;
using _Main.Scripts.Analytics;

namespace _Main.Scripts.Analytics
{
	public enum ELevelTutorial
	{
		GameModesIntro,
	}

	public enum ECurrencyType
	{
		Money
	}
	// public enum ELevelType
	// {
	//     AI,
	//     Local,
	//     Online
	// }

	
	public enum EAnalyticsEvent
	{
		// Booster_Bought, // Name, Total
		Game_Start, // 
		Game_End, // Index, Time
		Level_Start, // Type, Index 
		Level_Win, // Time, 
		Level_Fail, // LevelNo,
		Tutorial_Start, // Name, State, Context
		Tutorial_End, // Name, State, Context
		Currency_Change, // Name, Used, Total
	}
	
}