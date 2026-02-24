using System;
using Fiber.Utilities;
using UnityEngine;

namespace _Main.Scripts.InputSystem
{
	public enum SwipeDirection
	{
		Left,
		Right,
		Up,
		Down
	}

	public class InputController : Singleton<InputController>
	{
		public event Action<SwipeDirection> OnSwipe;

		[Header("Swipe Settings")]
		[SerializeField] private float minSwipeDistance = 60f;
		[SerializeField] private float maxSwipeTime = 0.5f;   
		[SerializeField] private bool allowMouseInEditor = true;

		private Vector2 _startPos;
		private float _startTime;
		private bool _tracking;

		private bool _inputEnabled = true;

		public void SetInputEnabled(bool enabled)
		{
			_inputEnabled = enabled;
		}

		private void Update()
		{
			if (!_inputEnabled) return;

#if UNITY_EDITOR
			if (allowMouseInEditor)
				HandleMouseSwipe();
#endif
			HandleTouchSwipe();
		}

		private void HandleTouchSwipe()
		{
			if (Input.touchCount <= 0) return;

			var t = Input.GetTouch(0);

			if (t.phase == TouchPhase.Began)
			{
				_tracking = true;
				_startPos = t.position;
				_startTime = Time.time;
			}
			else if (_tracking && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
			{
				_tracking = false;
				TryEmitSwipe(_startPos, t.position, Time.time - _startTime);
			}
		}

		private void HandleMouseSwipe()
		{
			if (Input.GetMouseButtonDown(0))
			{
				_tracking = true;
				_startPos = Input.mousePosition;
				_startTime = Time.time;
			}
			else if (_tracking && Input.GetMouseButtonUp(0))
			{
				_tracking = false;
				TryEmitSwipe(_startPos, (Vector2)Input.mousePosition, Time.time - _startTime);
			}
		}

		private void TryEmitSwipe(Vector2 start, Vector2 end, float duration)
		{
			Vector2 delta = end - start;
			if (delta.magnitude < minSwipeDistance) return;
			if (duration > maxSwipeTime) return;

			SwipeDirection dir = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
				? (delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left)
				: (delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down);

			OnSwipe?.Invoke(dir);
		}
	}
}
