using UnityEngine;
using UnityEngine.UI;

/// <summary>
///		Represents the path between two nodes,
///		and the camera transitions between.
/// </summary>
public class CinematicCameraPath : MonoBehaviour
{
	public enum CameraMode { Move, Fade, Both};

	[Header("Settings")]
	[Range(0.1f, 120)] public float MovementDurationInSeconds = 2;
	[Range(0, 120)] public float StartDelayDurationInSeconds = 0;
	[SerializeField] private CameraMode cameraMode  = CameraMode.Both;

	[Header("Path")]
	public CinematicCameraNode Origin;
	public CinematicCameraNode Target;

	[Header("Movement")]
	[SerializeField] private AnimationCurve movementCurve 
		= AnimationCurve.Linear(0, 0, 1, 1);

	[Header("Fade")]
	public Image TargetFadeImage;
	[SerializeField] private AnimationCurve fadeCurve
		= AnimationCurve.Linear(0, 0, 1, 1);

	[SerializeField] private Color32 originColor;
	[SerializeField] private Color32 targetColor = Color.black;

	private Vector3 currentPosition;
	private Vector3 currentTarget;
	private Color32 currentColor;

	/// <summary>
	///		Makes sure the provided transform matches 
	///		the rotation of the path's origin node.
	/// </summary>
	public void InitializeObject(Transform targetTransform)
	{
		targetTransform.position = Origin.Position;
		targetTransform.LookAt(Origin.Target);
	}

	/// <summary>
	///		Updates the transition relative 
	///		to the provided progress. 
	/// </summary>
	public virtual void UpdateObject(Transform targetTransform, float progress)
	{
		if (cameraMode == CameraMode.Move || cameraMode == CameraMode.Both)
			MoveCamera(targetTransform, progress);

		if (cameraMode == CameraMode.Fade || cameraMode == CameraMode.Both)
			FadeCamera(targetTransform, fadeTargetImage, progress);
	}

	/// <summary>
	///		Updates the Transform's position relative
	///		to the provided progress.
	/// </summary>
	private void MoveCamera(Transform targetTransform, float progress)
	{
		float realProgress = movementCurve.Evaluate(progress);

		currentPosition = Vector3.Lerp(Origin.Position, Target.Position, realProgress);
		currentTarget = Vector3.Lerp(Origin.Target, Target.Target, realProgress);

		targetTransform.position = currentPosition;
		targetTransform.LookAt(currentTarget);

		Debug.DrawLine(targetTransform.position, Origin.Target, Color.green);
		Debug.DrawLine(targetTransform.position, Target.Target, Color.blue);
		Debug.DrawLine(targetTransform.position, currentTarget, Color.cyan);
	}

	/// <summary>
	///		 Fades the camera relative to 
	///		 the provided progress.
	/// </summary>
	private void FadeCamera(Transform targetTransform, float progress)
	{
		float realProgress = fadeCurve.Evaluate(progress);

		currentColor = Color32.Lerp(originColor, targetColor, realProgress);

		TargetFadeImage.color = currentColor;
	}
}
