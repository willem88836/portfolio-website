using System.Collections.Generic;
using UnityEngine;
using ADG;
using System.Text;
using System.IO;
using System.Threading;

/// <summary>
///		Is Responsible for the collection of all data that 
///		can be gathered within this game. 
///		This data is collected in a .json file.
/// </summary>
public class DataCollector : MonoBehaviour
{
	private const string FileName = "Data";
	private const string Extension = ".json";

	public static DataCollector Instance;

	public Json.Object JsonData;

	public bool IsSaving { get; private set; }

	[SerializeField] private bool overrideFile = true;

	private List<IData> data;

	private string path;

	private readonly StringBuilder stringBuilder
		= new StringBuilder();


	public void Awake()
	{
		Instance = this;


		path = Path.Combine(Application.persistentDataPath, FileName);
		path = Path.ChangeExtension(path, Extension);
	}

	public void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}


	/// <summary>
	///		Adds the provided Data container to the data list. 
	/// </summary>
	public void Add(IData data)
	{
		if (this.data == null)
			this.data = new List<IData>();

		this.data.Add(data);
	}

	/// <summary>
	///		Removes the provided Data container from the data list. 
	/// </summary>
	public void Remove(IData data)
	{
		if (this.data == null)
			return;

		this.data.Remove(data);
	}

	/// <summary>
	///		Saves all data. 
	/// </summary>
	public void Save()
	{
		if (IsSaving)
		{
			Debug.LogWarning("System is already saving data!");
			return;
		}
		else if (data == null || data.Count == 0)
		{
			Debug.LogWarning("Can't save data if there is none to save!");
			return;
		}

		IsSaving = true;

		// A new thread is started to prevent a potentially massive freeze. 
		new Thread(() =>
		{
			if (JsonData == null)
				JsonData = new Json.Object();
			else
				JsonData.Clear();

			for (int i = 0; i < data.Count; i++)
			{
				IData data = this.data[i];
				data.CollectData(JsonData);
			}

			StringBuilder jsonString = JsonData.ToString(stringBuilder, Json.Pretty);
			
			if (overrideFile)
				File.WriteAllText(path, jsonString.ToString());
			else
				File.AppendAllText(path, jsonString.ToString());

			Debug.Log("Saved Data to: " + path);

			IsSaving = false;
		}).Start();
	}
}
