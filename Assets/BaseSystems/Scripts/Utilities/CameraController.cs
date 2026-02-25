using Cinemachine;
using Fiber.Utilities;
using TriInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace BaseSystems.Scripts.Utilities
{
	public class CameraController : Singleton<CameraController>
	{
		public CinemachineVirtualCamera CurrentCamera { get; private set; }

		[Title("iPhone")]
		[SerializeField] private CinemachineVirtualCamera iphoneCam;
		[SerializeField] [Min(0)] private float iphoneShadowDistance = 100;

		[Title("iPad")]
		[SerializeField] private CinemachineVirtualCamera ipadCam;
		[SerializeField] [Min(0)] private float ipadShadowDistance = 120;

		

		private void Awake()
		{
			CurrentCamera = iphoneCam;
			AdjustByScreenRatio();
		}

		private void OnValidate()
		{
			ChangeShadowDistance(iphoneShadowDistance);
		}

		private void AdjustByScreenRatio()
		{
			var ratio = (float)Screen.height / Screen.width;
			if (ratio > 1.6f) // iPhone
			{
				iphoneCam.gameObject.SetActive(true);
				ipadCam?.gameObject.SetActive(false);
				CurrentCamera = iphoneCam;

				ChangeShadowDistance(iphoneShadowDistance);
			}
			else if (ipadCam) // iPad
			{
				iphoneCam.gameObject.SetActive(false);
				ipadCam.gameObject.SetActive(true);
				CurrentCamera = ipadCam;

				ChangeShadowDistance(ipadShadowDistance);
			}
		}

		private void ChangeShadowDistance(float distance)
		{
			QualitySettings.shadowDistance = distance;
			var urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
			urp.shadowDistance = distance;
		}

		
		// --------------------------------------------------------------------
		// Set Camera Size& Position with GridSize //
		// --------------------------------------------------------------------
		public void FrameGrid(int gridWidth, int gridHeight, float cellSize, float paddingWorld = 0.5f,
			float edgePaddingPercent = 0.08f)
		{
			if (CurrentCamera == null)
				return;
			CurrentCamera.m_Lens.Orthographic = true;

			
			float aspect = 16f / 9f;
			if (Camera.main != null)
				aspect = Camera.main.aspect;

			// Grid world bounds (assuming the grid is always structured around the origin)
			float halfW = (gridWidth * cellSize) * 0.5f;
			float halfH = (gridHeight * cellSize) * 1f;

			Vector3[] cornersWorld =
			{
				new Vector3(-halfW, 0f, -halfH), new Vector3(-halfW, 0f, halfH), new Vector3(halfW, 0f, -halfH),
				new Vector3(halfW, 0f, halfH),
			};

			Transform camT = CurrentCamera.transform;
			
			Vector3 c0 = camT.InverseTransformPoint(cornersWorld[0]);
			Vector3 c1 = camT.InverseTransformPoint(cornersWorld[1]);
			Vector3 c2 = camT.InverseTransformPoint(cornersWorld[2]);
			Vector3 c3 = camT.InverseTransformPoint(cornersWorld[3]);

			float minX = Mathf.Min(c0.x, c1.x, c2.x, c3.x);
			float maxX = Mathf.Max(c0.x, c1.x, c2.x, c3.x);
			float minY = Mathf.Min(c0.y, c1.y, c2.y, c3.y);
			float maxY = Mathf.Max(c0.y, c1.y, c2.y, c3.y);

			// Camera-space center
			float centerX = (minX + maxX) * 0.5f;
			float centerY = (minY + maxY) * 0.5f;

			
			Vector3 worldShift = camT.TransformVector(new Vector3(centerX, centerY, 0f));
			camT.position += worldShift;

		
			float halfWidthCam = (maxX - minX) * 0.5f;
			float halfHeightCam = (maxY - minY) * 0.5f;

			halfWidthCam += paddingWorld;
			halfHeightCam += paddingWorld;

			float orthoSize = Mathf.Max(halfHeightCam, halfWidthCam / Mathf.Max(0.0001f, aspect));

			
			orthoSize *= (1f + Mathf.Max(0f, edgePaddingPercent));  // gaps from the edges

			CurrentCamera.m_Lens.OrthographicSize = orthoSize;
		}
	}
}