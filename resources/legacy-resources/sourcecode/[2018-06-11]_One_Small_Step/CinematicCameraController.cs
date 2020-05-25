using System.Collections;
using UnityEngine.UI;
using UnityEngine;

/// <summary>
///		Controls the the cinematic camera. 
/// </summary>
[ExecuteInEditMode]
public class CinematicCameraController : MonoBehaviour
{
	public enum CameraMode { Enabled, Disabled, Paused};

	[Header("Settings")]
	[SerializeField] private bool loopRoute = true;
	public CameraMode Mode = CameraMode.Disabled;
	[SerializeField] private CinematicCameraPath[] path;

	[Header("Testing")]
	[SerializeField] private bool testingMode = false;
	[SerializeField] private int pathStartIndex; 
	[SerializeField] private int pathEndIndex;
	
	[Header("References")]
	[SerializeField] private CinematicCameraPath tempPath;
	[SerializeField] private Transform camera;
	[SerializeField] private Image image;

	public float RouteProgress { get { return currentPathIndex / (path.Length - 1); } }
	public float PathProgress { get; private set; }
	public bool FinishedRoute { get; private set; }
	public bool FinishedPath { get; private set; }

	private CinematicCameraPath currentPath;
	private int currentPathIndex = 0;


	private void Start()
	{
		foreach (CinematicCameraPath path in path)
			path.TargetFadeImage = image;

		#if UNITY_EDITOR

			if (!testingMode)
				return;

			Debug.LogWarning("Testing mode is turned on");

			_currentPathIndex = _pathStartIndex;

		#endif
	}

	private void Update()
	{
		if (path == null)
			return;

		#if UNITY_EDITOR

			// Debugging lines
			for (int i = 0; i < _path.Length; i++)
				if (_path[i] != null)
					Debug.DrawLine(_path[i].Origin.Position, _path[i].Target.Position, Color.red);
		
		#endif

		if (Mode != CameraMode.Enabled || (Application.isEditor && !Application.isPlaying))
			return;

		UpdateTransition();
	}
	

	/// <summary>
	///		Updates the current transition
	/// </summary>
	private void UpdateTransition()
	{
		// Makes sure a path is selected.
		if (currentPath == null)
		{
			StartCoroutine(MoveNext(false));
			return;
		}

		// Updates the path.
		PathProgress += Time.deltaTime / currentPath.MovementDurationInSeconds;

		PathProgress = Mathf.Clamp01(PathProgress);

		currentPath.UpdateObject(camera, PathProgress);

		// Makes sure a path is ended.
		if (PathProgress < 1)
			return;

		StartCoroutine(MoveNext(true));
	}

	/// <summary>
	///		Updates the Controller to the next path.
	/// </summary>
	private IEnumerator MoveNext(bool hasDelay)
	{
		PathProgress = 1;
		Mode = CameraMode.Paused;
		FinishedPath = true;

		if (
			currentPathIndex == path.Length 
			|| (testingMode && currentPathIndex == pathEndIndex))
		{
			currentPathIndex = testingMode 
				? pathStartIndex 
				: 0;

			FinishedRoute = true;

			if (!loopRoute)
			{
				Mode = CameraMode.Disabled;
				StopAllCoroutines();
			}
		}

		currentPath = path[currentPathIndex];

		// A frame is waited to make sure other classes can recognize the finish.
		yield return new WaitForEndOfFrame(); 
		yield return new WaitForSeconds(currentPath.StartDelayDurationInSeconds);

		currentPath.InitializeObject(camera);

		PathProgress = 0;
		currentPathIndex++;
		Mode = CameraMode.Enabled;
	}

	/// <summary>
	///		Returns the origin node of the current path.
	/// </summary>
	public CinematicCameraNode NodeOriginAt(int index)
	{
		if (index < 0 || index >= path.Length)
		{
			Debug.LogError(string.Format("Index {0} out of range; value is clamped", index));
			index = Mathf.Clamp(index, 0, path.Length - 1);
		}

		return path[index].Origin;
	}

	/// <summary>
	///		Returns the target node of the current path.
	/// </summary>
	public CinematicCameraNode NodeTargetAt(int index)
	{
		if (index < 0 || index >= path.Length)
		{
			Debug.LogError(string.Format("Index {0} out of range; value is clamped", index));
			index = Mathf.Clamp(index, 0, path.Length - 1);
		}

		return path[index].Target;
	}
}
