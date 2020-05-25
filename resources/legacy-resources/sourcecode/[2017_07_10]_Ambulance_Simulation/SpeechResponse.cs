using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using FrostweepGames.SpeechRecognition.Google.Cloud;

public class SpeechResponse : MonoBehaviour {
    
    public static List<string> noWords = new List<string>();

    [SerializeField]
    AudioSource audioSource;

    [SerializeField]
    string fileIntro;

    [SerializeField]
    private List<string> notConsideredWords = new List<string>();

    [SerializeField]
    private Responses[] responses;
    
    public void Start()
    {
        noWords = notConsideredWords;
    }
    
    private List<AudioClip> GetAudioClips(string path)
    {
        List<AudioClip> clips = new List<AudioClip>();
        for (int i = 1; true; i++)
        {
            AudioClip clip = Resources.Load(path + "_" + i) as AudioClip;
            if (clip != null)
                clips.Add(clip);
            else
                break;
        }
        return clips;
    }

    private string GetRespondent(int index)
    {
        string character = "";
        switch (responses[index].person)
        {
            case Responses.Persons.Child:
                character = DataStorage.instance.childCharacter;
                break;
            case Responses.Persons.Parent:
                character = (Random.Range(0, 1) == 0) ? DataStorage.instance.dadCharacter : DataStorage.instance.momCharacter; // Every time, this is eiter one of the two.
                break;
            case Responses.Persons.Dispatcher:
                character = DataStorage.instance.dispatchCharacter;
                break;
        }
        return character;
    }
    
    private string[] TransformInput(SpeechRecognitionAlternative[] realInput)
    {
        string[] input = new string[realInput.Length];
        for (int i = 0; i < realInput.Length; i++)
        {
            input[i] = realInput[i].transcript;
        }
        return input;
    }

    private int GetInputIndex(string[] input)
    {
        int[] heat = new int[responses.Length];
        int maxIndex = -1;
        for (int x = 0; x < input.Length; x++)
        {
            string[] inputWords = input[x].Split();
            for (int i = 0; i < responses.Length; i++)
            {
                List<string[]> words = responses[i].GetOptions;
                for (int ip = 0; ip < inputWords.Length; ip++) // The amount of words gained through input
                {
                    for (int l = 0; l < words.Count; l++) // The amount of words combinations that can be recognised.
                    {
                        for (int w = 0; w < words[l].Length; w++) // The amount of words that are included inside this combination.
                        {
                            if (words[l][w] == inputWords[ip]) // if it's the same.. yay!
                            {
                                heat[i]++;
                                if (maxIndex == -1 || heat[i] > heat[maxIndex])
                                {
                                    maxIndex = i;
                                }
                            }
                        }
                    }
                }
            }
        }
        return maxIndex;
    }

    private void PlayAudio(int index)
    {
        if (index != -1)
        {
            responses[index].response.Invoke();

            string respondent = GetRespondent(index);
            bool isChild = respondent == "Child";

            string audioName = DataStorage.instance.dataPath + fileIntro;
            audioName += respondent + "_";
            audioName += responses[index].reaction;
            List<AudioClip> files = GetAudioClips(audioName);

            if (files.Count == 0)
            {
                Debug.LogError("The audio file you are referring to does not exist (" + audioName + ") Please check if you have used the correct name or if the file exists!");
            }
            else
            {
                int clipIndex = Random.Range(0, files.Count);
                AudioClip clip = files[clipIndex];
                if (!audioSource.isPlaying)
                {
                    Debug.Log("Playing: " + audioName + "_" + ++clipIndex);
                    audioSource.PlayOneShot(clip);
                }
                else
                {
                    Debug.LogWarning("The audiosource is currently playing and therefore cannot play current audio file (" + audioName + "). [SpeechResponse]");
                }
                StartCoroutine(isResponding(clip, isChild));
            }
        }
    }

    IEnumerator<WaitForSeconds> isResponding(AudioClip clip, bool isChild)
    {
        if (isChild)
        {
            DataStorage.instance.childIsSpeaking = true;
            CryScript.instance.StopCrying();
            yield return new WaitForSeconds(clip.length);
            DataStorage.instance.childIsSpeaking = false;
        }
        else
        {
            yield return new WaitForSeconds(0);
            DataStorage.instance.childIsSpeaking = false;
        }
    }

    
    public void CheckResponse(SpeechRecognitionAlternative[] realInput)
    {
        string[] input = TransformInput(realInput);
        int maxIndex = GetInputIndex(input);
        PlayAudio(maxIndex);
    }
    public string GetRecognition(SpeechRecognitionAlternative[] realInput)
    {
        string[] input = TransformInput(realInput);
        int maxIndex = GetInputIndex(input);
        string text = "";
        Debug.Log(responses.Length + " " + maxIndex);
        return input[0] + " - " + ((maxIndex != -1) ? responses[maxIndex].GetOption[0] : " ");
    }
}

[System.Serializable]
public class Responses
{
    [SerializeField]
    private string[] options;

    public enum Persons { None, Parent, Child, Dispatcher };

    //[Header("Choose one; UnityEvent or person/reaction, only one can be used at the time.")]
    public Persons person;
    public string reaction;
    
    public UnityEvent response;

    public List<string[]> GetOptions
    {
        get
        {
            List<string[]> words = new List<string[]>();

            string[] _words;

            foreach (string item in options)
            {
                _words = item.Split();
                words.Add(_words);
            }
            return words;
        }
    }
    public string[] GetOption
    {
        get { return options; }
    }
}