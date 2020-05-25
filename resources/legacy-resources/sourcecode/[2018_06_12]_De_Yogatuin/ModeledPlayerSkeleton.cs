using nuitrack;
using UnityEngine;
using Joint = nuitrack.Joint;

/// <summary>
///		Represents a pre-modeled skeleton that is animated
///		using the nuitrack SDK.
/// </summary>
public sealed class ModeledPlayerSkeleton : ModeledSkeleton
{
	private const int TargetPlayerIndex = 0;

	[SerializeField, Range(0, 0.1f)] private float smoothing = 0.08f;

	private void OnEnable()
	{
		NuitrackManager.OnSkeletonTrackerUpdate += OnSkeletonUpdate;
	}

	private void OnDisable()
	{
		NuitrackManager.OnSkeletonTrackerUpdate -= OnSkeletonUpdate;
	}

	private void Update()
	{
		if (Joints == null)
			return;

		UpdateJoints();
	}

	/// <summary>
	///		Updates the skeleton's joints smoothly to their
	///		assigned target rotation.
	/// </summary>
	private void UpdateJoints()
	{
		Quaternion velocity = Quaternion.identity;
		foreach (CharacterJoint joint in Joints)
		{
			joint.Rotation = Utilities.SmoothDamp(
				joint.Rotation,
				joint.TargetRotation,
				ref velocity,
				smoothing,
				Time.deltaTime);
		}
	}

	/// <summary>
	///		Updates the model's joints target rotation to the rotation
	///		currently tracked by the Nuitrack SDK.
	/// </summary>
	private void OnSkeletonUpdate(SkeletonData skeletonData)
	{
		if (skeletonData.NumUsers == 0)
			return;

		Skeleton skeleton = skeletonData.Skeletons[TargetPlayerIndex];
		Joint[] skeletonJoints = skeleton.Joints;

		Quaternion sensorOrientation = Quaternion.Inverse(CalibrationInfo.SensorOrientation);

		Quaternion myRotation = transform.rotation;

		for (int i = 0; i < Joints.Length; i++)
		{
			Joint joint = skeletonJoints[i];
			Quaternion rotation = joint.ToQuaternionMirrored();
			rotation = sensorOrientation * rotation;

			CharacterJoint characterJoint = Joints[i];

			rotation = myRotation * rotation;
			characterJoint.TargetRotation =  rotation;
			characterJoint.ProjectedPosition = joint.Proj.ToVector3();
		}
	}
}
