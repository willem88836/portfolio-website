using UnityEngine;
using UnityEngine.UI;

/// <summary>
///		Contains the position and rotation of one node. 
/// </summary>
[ExecuteInEditMode]
public class CinematicCameraNode : MonoBehaviour
{
	public enum UpdateType { Position, Target};

	public Vector3 Position { get { return transform.position; } }
	public Vector3 Target { get { return transform.position + transform.forward; } }


#if UNITY_EDITOR

	private void Update()
	{
		Debug.DrawLine(transform.position, Target, Color.red);
	}

#endif

	/// <summary>
	///		Updates the node's position and rotation.
	/// </summary>
	public void UpdateNode(Vector3 position, Vector3 target)
	{
		transform.position = position;
		transform.LookAt(target);
	}

	/// <summary>
	///		Updates the node's position or rotation.
	/// </summary>
	public void UpdateNode(Vector3 position, UpdateType target)
	{
		if (target == UpdateType.Position)
			transform.position = position;
		else if (target == UpdateType.Target)
			transform.LookAt(position);
	}
}
