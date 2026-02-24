using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Main.Scripts.Datas
{
	[CreateAssetMenu(fileName = "GameParameters", menuName = "GameParametersSO", order = 0)]
	public class GameParametersSO : ScriptableObject
	{
		[SerializeField] private float ballMoveDuration = 0.2f;
		[SerializeField] private int rotatePerStep = 90;

		public List<FillColors> fillColors = new List<FillColors>();

		public float GetBallMoveDuration() => ballMoveDuration;
		public float GetRotatePerStep() => rotatePerStep;
	}

	[Serializable]
	public class FillColors
	{
		public ColorType colorType;
		public Color color;
	}

	public enum ColorType
	{
		Red,
		Orange,
		Yellow,
		Green,
		Blue,
		Purple,
		Pink,
		White,
		Black,
	}
}