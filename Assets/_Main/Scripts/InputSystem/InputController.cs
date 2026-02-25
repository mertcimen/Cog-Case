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
		[SerializeField] private float minSwipeDistance = 60f; // classic swipe threshold (tap->swipe)
		[SerializeField] private float maxSwipeTime = 0.5f;
		[SerializeField] private bool allowMouseInEditor = true;

		[Header("Continuous Swipe (Hold & Drag)")]
		[SerializeField] private bool continuousWhileHolding = true;
		[SerializeField] private float continuousStepDistance = 60f;
		[SerializeField] private bool lockAxisAfterFirstStep = true;

		private Vector2 _startPos;
		private float _startTime;
		private bool _tracking;

		// continuous state
		private Vector2 _lastStepOrigin;
		private bool _axisLocked;
		private bool _lockedHorizontal;

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
				HandleMouse();
#endif
			HandleTouch();
		}

		// ---------------- TOUCH ----------------

		private void HandleTouch()
		{
			if (Input.touchCount <= 0) return;

			var t = Input.GetTouch(0);

			if (t.phase == TouchPhase.Began)
			{
				BeginTrack(t.position);
			}
			else if (_tracking && (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
			{
				if (continuousWhileHolding)
					ProcessContinuous(t.position);
			}
			else if (_tracking && (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled))
			{
				EndTrack(t.position);
			}
		}

		// ---------------- MOUSE ----------------

		private void HandleMouse()
		{
			if (Input.GetMouseButtonDown(0))
			{
				BeginTrack(Input.mousePosition);
			}
			else if (_tracking && Input.GetMouseButton(0))
			{
				if (continuousWhileHolding)
					ProcessContinuous(Input.mousePosition);
			}
			else if (_tracking && Input.GetMouseButtonUp(0))
			{
				EndTrack(Input.mousePosition);
			}
		}

		// ---------------- CORE ----------------

		private void BeginTrack(Vector2 pos)
		{
			_tracking = true;
			_startPos = pos;
			_startTime = Time.time;

			_lastStepOrigin = pos;
			_axisLocked = false;
		}

		private void EndTrack(Vector2 endPos)
		{
			_tracking = false;

			if (!continuousWhileHolding)
			{
				TryEmitClassicSwipe(_startPos, endPos, Time.time - _startTime);
			}
			else
			{
			}
		}

		private void ProcessContinuous(Vector2 currentPos)
		{
			Vector2 delta = currentPos - _lastStepOrigin;

			if (delta.magnitude < continuousStepDistance)
				return;

			bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);

			// Eğer axis lock istiyorsan: sadece bu step için uygula
			if (lockAxisAfterFirstStep)
			{
				if (!_axisLocked)
				{
					_axisLocked = true;
					_lockedHorizontal = horizontal;
				}

				horizontal = _lockedHorizontal;
			}

			// Seçilen axis üzerinde yeterli mesafe var mı?
			float axisDistance = horizontal ? Mathf.Abs(delta.x) : Mathf.Abs(delta.y);
			if (axisDistance < continuousStepDistance)
				return;

			SwipeDirection dir;

			if (horizontal)
				dir = delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
			else
				dir = delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;

			OnSwipe?.Invoke(dir);

			// ✅ KRİTİK: event sonrası başlangıcı “son dokunduğum nokta” yap
			_lastStepOrigin = currentPos;

			// ✅ KRİTİK: axis kilidini de resetle ki eventten sonra yön değiştirebilsin
			_axisLocked = false;
		}

		private void TryEmitClassicSwipe(Vector2 start, Vector2 end, float duration)
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