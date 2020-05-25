using nuitrack;
using System;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

/// <summary>
///		Represents the data of one skeleton. 
/// </summary>
public abstract class CharacterSkeleton : MonoBehaviour
{
	/// <summary>
	///		is the number of limbs the Nuitrack SDK offers.
	/// </summary>
	protected const int JointCount = 25;

	[SerializeField] private CharacterJoint _defaultJoint;

	[NonSerialized] public CharacterJoint[] Joints;

	public Vector3 Position { get { return transform.position; } }
	public  Quaternion Rotation { get { return transform.rotation; } }

	public CharacterJoint this[JointType type]
	{
		get
		{
			CharacterJoint joint = Joints[(int)type];
			if (joint != null)
				return joint;

			Debug.LogWarningFormat("Skeleton {0} does not contain key: {1}", type, name);
			return null;
		}
	}


	protected virtual void Start()
	{
		Clear();
		// Generates a set of new joints.
		for (int i = 0; i < JointCount; i++)
		{
			var type = (JointType)i;
			Joints[i] = NewPlayerJoint(type, transform);
		}
	}


	/// <summary>
	///		Sets the skeleton's current bones to null.
	/// </summary>
	public void Clear()
	{
		if (Joints == null)
			Joints = new CharacterJoint[JointCount];

		for (var i = 0; i < JointCount; i++)
			Joints[i] = null;
	}

	/// <summary>
	///		Instantiates a new CharacterJoint GameObject at the provided parent location.
	/// </summary>
	protected CharacterJoint NewPlayerJoint(JointType type, Transform parent)
	{
		CharacterJoint joint = Instantiate(
			_defaultJoint,
			parent);

		joint.transform.position = parent.transform.position;
		joint.transform.rotation = parent.transform.rotation;

		joint.Type = type;

		return joint;
	}

	/// <summary>
	///		Adds a CharacterJointComponent to the provided GameObject.
	/// </summary>
	protected CharacterJoint NewPlayerJoint(JointType type, GameObject jointObject)
	{
		CharacterJoint joint = jointObject.AddComponent<CharacterJoint>();
		joint.Type = type;
		return joint;
	}

	/// <summary>
	///		Returns the delta between two joints.
	/// </summary>
	public Vector3 Delta(JointType from, JointType to)
	{
		CharacterJoint current = this[from];
		CharacterJoint target = this[to];
		return target.transform.position - current.transform.position;
	}
}
