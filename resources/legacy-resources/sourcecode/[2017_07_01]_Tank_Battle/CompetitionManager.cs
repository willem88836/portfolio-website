using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Reflection;
using UnityEngine.Events;
using Framework;
using Random = UnityEngine.Random;

/**
 * TODO 
 *  Characters can die after winning (resulting in a level that cannot end). 
 *  prevent infinite levels.
 *  
 * BUGS:
 *  Double pool spawn
 *  Tanks having an instance outside of the map as well (no model, but with sound and can shoot).
 */

namespace Framework
{
    public class CompetitionManager : MonoBehaviour
    {

        public static System.Type winner;

        /// <summary>
        /// Class to store a spawnpoints position and angle in a single variable
        /// </summary>               
        public class SpawnPoint
        {
            public Vector3 position;
            public float angle;

            public SpawnPoint(Vector3 newPos, float newAngle)
            {
                position = newPos;
                angle = newAngle;
            }
        }

        [Header("Debugging")]

        [SerializeField]
        int testIndex = -1;                                     // A variable that is used for testing only. By adapting this, you can infinitely increase the theoretical amount of competitors.

        [Header("Objects")]

        //Base robot prefab
        [SerializeField]
        private GameObject baseRobot;                           // An empty robot which will be used to spawn the various types of robots.

        [Header("UI Objects")]

        //Reference to the UI overview
        [SerializeField]
        private GameObject playerUILeft;                        // Contains a reference to the object that is used to display the competitors' health and name (in-game)
        [SerializeField]
        private GameObject playerUIRight;                       // Contains a reference to the object that is used to display the competitors' health and name (in-game)
        [SerializeField]
        private GameObject poolUI;                              // Contains a reference to the object that is used to display the competitors in one pool (charts)
        [SerializeField]
        private GameObject poolSubUI;                           // Contains a reference to the object that is used to display the competitors' names (charts)
        [SerializeField]
        private GameObject StatObject;                          // Contains a reference to the object that is used to represents the competitors' stats (Best of X)

        [Header("UI Data")]

        [SerializeField]
        private int chartHeight;                                // Contains a value that determines the amount of space the competition chart is able to use.
        [SerializeField]
        private int                                             // Contains values that determine the placement of objects inside the competition chart.
            poolObjDynOffsetX,
            poolObjWidth,
            poolObjHeight,
            poolObjStatOffSetX,
            poolObjStatOffSetY;
        [SerializeField]
        private int                                             // Contains values that determine the placemen of UI objects of the competitors.
            tankUIOffSet,
            statOffSetY,
            statSize,
            statMaxSize;
        [Header("Competition Data")]

        [SerializeField]
        private int tankSpawnRange;                             // The area in which a tank can spawn in case all static spawnpoints are in use.

        private int poolSize = 2;                               // The number of contestants that are preferably in one pool.
        private int poolRounds = 3;                             // The number of rounds that must be played to determine a winner for a pool.
        private bool autoRun = true;                            // A reference to whether the game should continue automatically (without having to interact with the menu buttons).
        private int autoRunDelay = 5;                           // The number of seconds the autoRun function will wait before continueing.

        private System.Type[] tankListAI;                       // Contains a reference to all the Tank Behaviours that are currently in the scene.

        private SpawnPoint[] spawnPoints;                       // Contains a reference to all locations which competitors can spawn on.

        private Transform intermissionScreen;                   // A reference to the object that contains all objects for the intermission screen.
        private Transform chartScreen;                          // A reference to the object that contains all charts.
        private Transform UIScreen;                             // A reference to the object that contains all ingame UI elements.

        private int round = 0;                                  // Contains the index of the current round, is updated once all pools of this round have battled.
        private int currentPool = 0;                            // Contains the index of the pool that is currently battling.
        private int poolRound = 0;                              // Contains the how many-eth round the current game is of one specific pool (for Best out of X)
        private GameObject[] currentBots = new GameObject[0];   // Contains a reference to the GameObjects that are in the scene.
        private int[] currentBotsStats = new int[0];            // Contains the number of wins each robot currently in the scene has.

        private List<int> roundPoolSize = new List<int>();      // Contains a reference to the number of pools each round has.
        private int[][] pools;                                  // Contains all the combination of pool and contestant indices.

        private bool restarting = false;                        // is true when a new round is being started.
        private int NextPromotePoolIndex = -1;                  // The index of the pool the next winner will be placed in.

        /// <summary>
        /// Shows the intermission screen.
        /// </summary>
        private void InitiateIntermission(string screenText, bool nextPool)
        {
            int uiElements = UIScreen.childCount;
            for (int i = 0; i < uiElements; i++)
            {
                Destroy(UIScreen.GetChild(i).gameObject);
            }
            intermissionScreen.gameObject.SetActive(true);
            intermissionScreen.GetChild(0).GetComponent<Text>().text = screenText;
            if (autoRun)
            {
                StartCoroutine(AutoProceed(nextPool));
            }
        }
        /// <summary>
        /// Shows the competition chart.
        /// </summary>
        public void ToChart()
        {
            chartScreen.gameObject.SetActive(true);
            // Destroys all current pools (to avoid overlap).
            int childCount = chartScreen.childCount;
            for (int i = 0; i < childCount; i++)
            {
                if (chartScreen.GetChild(i).GetComponent<Button>() == null) Destroy(chartScreen.GetChild(i).gameObject);
            }

            // Adds all updated current pools.
            for (int i = 0; i < pools.Length; i++)
            {
                int index = 0;
                int indexDeviation = 0;
                for (int o = 0; o < roundPoolSize.Count; o++)
                {
                    indexDeviation += roundPoolSize[o];
                    if (i < indexDeviation)
                    {
                        index = o;
                        break;
                    }
                }
                float xPos = index * poolObjDynOffsetX;
                float yPos = (float)(((i + 0.5d - (double)indexDeviation) / roundPoolSize[index]) * -chartHeight);
                // Generic Displaying etc.
                GameObject poolObj = (Instantiate(poolUI));
                poolObj.transform.SetParent(chartScreen);
                poolObj.name = "Pool: " + i;
                RectTransform poolObjRectTrans = poolObj.GetComponent<RectTransform>();
                poolObjRectTrans.sizeDelta = new Vector2(poolObjWidth, poolObjHeight * pools[i].Length);
                poolObjRectTrans.position = new Vector3(xPos + poolObjStatOffSetX, yPos + poolObjStatOffSetY, 0);
                for (int o = 0; o < pools[i].Length; o++)
                {
                    GameObject poolSubObj = (Instantiate(poolSubUI));
                    poolSubObj.transform.SetParent(poolObj.transform);
                    RectTransform poolSubObjRectTrans = poolSubObj.GetComponent<RectTransform>();
                    poolSubObjRectTrans.position = poolObjRectTrans.position + new Vector3(0, -poolObjHeight * o, 0);
                    poolSubObj.GetComponent<Text>().text = (pools[i][o] != -1) ? (testIndex == -1) ? tankListAI[pools[i][o]].Name : pools[i][o].ToString() : "???";

                    int pastLength = 0;
                    for (int c = 0; c < index; c++)
                    {
                        pastLength += roundPoolSize[c];
                    }
                    bool finished = false;
                    for (int c = pastLength + roundPoolSize[index]; index + 1 < roundPoolSize.Count && c < pastLength + roundPoolSize[index] + roundPoolSize[index + 1]; c++)
                    {
                        for (int b = 0; b < pools[c].Length; b++)
                        {
                            if (pools[i][o] == pools[c][b])
                            {
                                poolSubObj.GetComponent<Text>().color = Color.blue;
                                finished = true;
                                break;
                            }
                            if (pools[c][b] == -1)
                            {
                                finished = true;
                                break;
                            }
                        }
                        if (finished) break;
                    }


                    // check through all next pools' characters, if one of them is this, change text colour.
                }
            }
        }
        /// <summary>
        /// Automatically proceeds tothe next battle.
        /// </summary>
        private IEnumerator<WaitForSeconds> AutoProceed(bool poolEnd)
        {
            transform.GetChild(1).GetChild(4).gameObject.SetActive(true);
            Text countDownText = transform.GetChild(1).GetChild(4).GetComponent<Text>();
            int counter = 0;
            countDownText.text = "Proceeding in: " + (autoRunDelay - counter);
            while (counter < autoRunDelay)
            {
                yield return new WaitForSeconds(1);
                counter++;
                countDownText.text = "Proceeding in: " + (autoRunDelay - counter);
            }
            counter = 0;
            countDownText.text = "Proceeding in: " + (autoRunDelay - counter);
            intermissionScreen.gameObject.SetActive(false);
            if (poolEnd)
            {
                ToChart();
                while (counter < autoRunDelay)
                {
                    yield return new WaitForSeconds(1);
                    counter++;
                    countDownText.text = "Proceeding in: " + (autoRunDelay - counter);
                }
                chartScreen.gameObject.SetActive(false);
            }
            transform.GetChild(1).GetChild(4).gameObject.SetActive(false);
            NextBattle();
        }
        /// <summary>
        /// Starts the next battle.
        /// </summary>
        public void NextBattle()
        {
            if (currentBots.Length > 0)
            {
                Destroy(currentBots[0]);
            }
            SpawnPoolBots(currentPool);
            restarting = false;
        }
        /// <summary>
        /// Returns true if the competitors' stats are ot tied.
        /// </summary>
        private bool NotTiedStats()
        {
            int winnerIndex = -1;
            int runnerUpIndex = -1;
            for (int i = 0; i < currentBotsStats.Length; i++)
            {
                if (winnerIndex == -1 || currentBotsStats[i] > currentBotsStats[winnerIndex])
                {
                    runnerUpIndex = winnerIndex;
                    winnerIndex = i;
                }
                else if (runnerUpIndex == -1 || currentBotsStats[i] > currentBotsStats[runnerUpIndex])
                {
                    runnerUpIndex = i;
                }
            }
            
            return (currentBotsStats[winnerIndex] > Mathf.Floor((poolRounds) / 2) && currentBotsStats[runnerUpIndex] + (poolRounds - poolRound) < currentBotsStats[winnerIndex]);
        }
        /// <summary>
        /// Determines wether the round has ended or not by checking whether X rounds have been played, and comparing competitors' scores.
        /// </summary>
        private bool PoolRoundSwitch()
        {
            poolRound++;
            if (NotTiedStats())
            {
                poolRound = 0;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Adds the provided index to a pool in the next round.
        /// </summary>
        /// <param name="index"></param>
        public void PromoteCompetitor(int index)
        {
            if (round < roundPoolSize.Count - 1 && NextPromotePoolIndex < pools.Length)
            {
                for (int i = 0; i < pools[NextPromotePoolIndex].Length; i++)
                {
                    if (pools[NextPromotePoolIndex][i] == -1)
                    {
                        pools[NextPromotePoolIndex][i] = index;
                        if (i == pools[NextPromotePoolIndex].Length - 1)
                        {
                            NextPromotePoolIndex++;
                        }
                        break;
                    }
                }
            }
            else
            {
                winner = tankListAI[index];
                Debug.Log("Game won by: " + tankListAI[index]);

                UnityEngine.SceneManagement.SceneManager.LoadScene("VictoryScene");
            }
        }
        /// <summary>
        /// Continues to the next round
        /// </summary>
        public void NextRound(bool poolRoundFinished)
        {
            if (poolRoundFinished)
            {
                currentPool++;
                int roundLength = 0;
                for (int i = 0; i < roundPoolSize.Count; i++)
                {
                    if (currentPool == roundLength) round++;
                    roundLength += roundPoolSize[i];
                }
                poolRound = 0;
            }
        }
        /// <summary>
        /// Is called when a tank is destoyed. Tracks What tank is in what position, and whether the game has ended.
        /// </summary>
        private void BotDestoyed()
        {
            if (!restarting)
            {
                int living = 0;
                int livingTankIndex = -1;
                for (int i = 0; i < currentBots.Length; i++)
                {
                    if (currentBots[i] != null && currentBots[i].GetComponent<RobotMotor>().GetCurrentHealth > 0)
                    {
                        living++;
                        livingTankIndex = i;
                    }
                }
                if (living == 1)
                {
                    restarting = true;
                    currentBotsStats[livingTankIndex]++;
                    currentBots[livingTankIndex].SendMessage("OnVictory");
                    
                    bool poolRoundFinished = PoolRoundSwitch();
                    if (poolRoundFinished)
                    {
                        PromoteCompetitor(pools[currentPool][livingTankIndex]);
                    }
                    NextRound(poolRoundFinished);
                    InitiateIntermission(currentBots[livingTankIndex].name + " Won!", poolRoundFinished);
                    GameObject obj = currentBots[livingTankIndex];
                    if (obj == null) Debug.LogWarning("WINNING TANK IS NULL! [void BotDestroyed()]");
                    currentBots = new GameObject[1] { obj };
                }
                if (living == 0)
                {
                    restarting = true;
                    InitiateIntermission("TIE!", false);
                }
            }
        }

        /// <summary>
        /// Spawns the currently selected bots.
        /// </summary>
        private void SpawnPoolBots(int poolNo)
        {
            System.Type[] tempArray = (System.Type[])tankListAI.Clone();
            List<SpawnPoint> _spawnPoints = new List<SpawnPoint>();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                _spawnPoints.Add(spawnPoints[i]);
            }
            currentBots = new GameObject[pools[currentPool].Length];
            if (poolRound == 0) currentBotsStats = new int[pools[currentPool].Length];
            for (int i = 0; i < pools[currentPool].Length; i++)
            {
                //Spawn base robot prefab on the spawnpoint3
                GameObject newTank = currentBots[i] = Instantiate(baseRobot);
                newTank.GetComponent<RobotMotor>().onDestroy.AddListener(new UnityAction(BotDestoyed)); // Adds a function to the RobotMotor to call this when destoyed.

                SpawnPoint spawnPoint;
                if (pools[currentPool].Length > spawnPoints.Length)
                {
                    spawnPoint = new SpawnPoint(
                        new Vector3(Random.Range((float)-tankSpawnRange, tankSpawnRange), 0, Random.Range((float)-tankSpawnRange, tankSpawnRange)),
                        Random.Range(0, 360));
                }
                else
                {
                    int spawnIndex = Random.Range(0, _spawnPoints.Count);
                    spawnPoint = _spawnPoints[spawnIndex];
                    _spawnPoints.RemoveAt(spawnIndex);

                }
                newTank.transform.position = spawnPoint.position;
                newTank.transform.eulerAngles = new Vector3(0f, spawnPoint.angle, 0f);

                GameObject NewUI = (Instantiate((i % 2 == 0) ? playerUILeft : playerUIRight));
                NewUI.transform.SetParent(transform.GetChild(1));
                RectTransform UIRect = NewUI.GetComponent<RectTransform>();
                UIRect.offsetMin = new Vector2(0, (i / 2) * -tankUIOffSet);
                UIRect.offsetMax = new Vector2(0, (i / 2) * -tankUIOffSet);
                NewUI.transform.SetParent(UIScreen);

                int statIcons = Mathf.Max(currentBotsStats[i], poolRounds);
                for (int s = 0; s < statIcons && statIcons > 1; s++)
                {
                    GameObject NewStatUI = (Instantiate(StatObject));
                    NewStatUI.transform.SetParent(NewUI.transform);
                    RectTransform statUIRect = NewStatUI.GetComponent<RectTransform>();
                    NewStatUI.GetComponent<Image>().color = (s < currentBotsStats[i]) ? Color.red : Color.white;
                    statUIRect.offsetMin = UIRect.offsetMin;
                    statUIRect.offsetMax = UIRect.offsetMax;

                    int _statSize = statSize;
                    if (statIcons * statSize > statMaxSize) // prevents staticons from moving outside the UI
                    {
                        _statSize = (int)(statSize * ((float)statMaxSize / (statIcons * statSize)));
                    }
                    statUIRect.position = UIRect.position + new Vector3((i % 2 == 0) ? -tankUIOffSet + s * _statSize : tankUIOffSet - s * _statSize, -statOffSetY, 0);
                    statUIRect.sizeDelta = new Vector2(statSize, statSize);
                }
                newTank.GetComponent<RobotMotor>().playerUI = NewUI.transform;
                newTank.GetComponent<RobotMotor>().hasHealthUI = true; // this is needed to prevent a lot of errors connected to the UI
                newTank.AddComponent(tankListAI[pools[currentPool][i]]);
                newTank.name = tankListAI[pools[currentPool][i]].Name;
            }
        }

        /// <summary>
        /// Creates all the pools that will take part in this competition.
        /// </summary>
        private void CreatePools(int competitors)
        {
            if (competitors != tankListAI.Length) Debug.LogWarning("WARNING! - The amount of given competitors does not equal to the amount of Tank Behaviours \nDO NOT START A TANK BATTLE! \nTo resolve this, Change variable (testIndex) to -1 in the inspector.");

            List<int> _poolSize = new List<int>();
            int poolsIndex = 0;
            int _competitors = competitors;
            int leftOvers;
            string output = "Pools per Round: ";
            while (_competitors >= poolSize)
            {
                int _pools = _competitors / poolSize;
                poolsIndex += _pools;
                leftOvers = _competitors % poolSize;
                _competitors -= _pools * (poolSize - 1) + leftOvers; // The amount of competitors is filtered from all losers (poolsize - 1) + leftOvers
                for (int i = 0; i < _pools; i++)
                {
                    _poolSize.Add(((leftOvers > 0) ? poolSize + 1 : poolSize));
                    leftOvers--;
                }
                if (_competitors < poolSize && leftOvers > 0)
                {
                    _pools++;
                    _poolSize[_poolSize.Count - 1] += leftOvers;
                    leftOvers = 0;
                }
                roundPoolSize.Add(_competitors);
                output += _competitors + ", ";
            }

            if (_competitors > 1)
            {
                poolsIndex++;
                _poolSize.Add(_competitors);
                roundPoolSize.Add(1);
                output += 1 + ", ";
            }
            Debug.Log(output);
            pools = new int[poolsIndex][];
            for (int i = 0; i < poolsIndex; i++)
            {
                pools[i] = new int[_poolSize[i]];
            }
            NextPromotePoolIndex = roundPoolSize[0];
        }
        /// <summary>
        /// Fills the pools for the firts round.
        /// </summary>
        private void RefillPools(int competitors)
        {
            List<int> competitorIndices = new List<int>();
            for (int c = 0; c < competitors; c++)
            {
                competitorIndices.Add(c);
            }
            int firstRoundLength = competitors / poolSize;
            for (int p = 0; p < pools.Length; p++)
            {
                for (int c = 0; c < pools[p].Length; c++)
                {
                    if (p < firstRoundLength)
                    {
                        pools[p][c] = competitorIndices[Random.Range(0, competitorIndices.Count)];
                        competitorIndices.Remove(pools[p][c]);
                    }
                    else
                    {
                        pools[p][c] = -1;
                    }
                }
            }
        }
        /// <summary>
        /// Creates a Completely new pool.
        /// </summary>
        private void CreateNewPool(int competitors)
        {
            CreatePools(competitors);
            RefillPools(competitors);
        }
        /// <summary>
        /// Applies Settings filled in the introductionScreen.
        /// </summary>
        public void ApplyNewSettings()
        {
            int inputChildIndex = 2;
            Transform uiScreen = transform.GetChild(1).GetChild(3);
            int ps = (uiScreen.GetChild(2).GetChild(inputChildIndex).GetComponent<Text>().text == "") ? poolSize : int.Parse(uiScreen.GetChild(2).GetChild(inputChildIndex).GetComponent<Text>().text);
            poolSize = ps;
            int pr = (uiScreen.GetChild(3).GetChild(inputChildIndex).GetComponent<Text>().text == "") ? poolRounds : int.Parse(uiScreen.GetChild(3).GetChild(inputChildIndex).GetComponent<Text>().text);
            poolRounds = pr;
            autoRun = uiScreen.GetChild(4).GetComponent<Toggle>().isOn;
            int ard = (uiScreen.GetChild(5).GetChild(inputChildIndex).GetComponent<Text>().text == "") ? autoRunDelay : int.Parse(uiScreen.GetChild(5).GetChild(inputChildIndex).GetComponent<Text>().text);
            autoRunDelay = ard;
        }
        /// <summary>
        /// Reboots the CompetitionManager;
        /// </summary>
        public void Reboot()
        {
            intermissionScreen = transform.GetChild(1).GetChild(0);
            chartScreen = transform.GetChild(1).GetChild(1);
            UIScreen = transform.GetChild(1).GetChild(2);
            FindRobots();
            if (tankListAI.Length < poolSize)
            {
                poolSize = tankListAI.Length;
                Debug.LogWarning("Poolsize is larger than total amount of Tank Behaviours! \nThe poolsize has been adapted to this amount of Behaviours. [" + poolSize + "]");
            } // Avoids poolsize being larger than number tankAIs.
            if (poolRounds <= 0)
            {
                poolRounds = 1;
                Debug.LogWarning("PoolRounds must be 1 or higher  \nThe poolRounds have been set to 1");
            } // Avoids the poolRounds being less or equal to 0.
            FindSpawnLocations();
            CreateNewPool((testIndex != -1) ? testIndex : tankListAI.Length);
            if (testIndex != -1)
            {
                foreach (int[] comp in pools)
                {
                    PromoteCompetitor(comp[Random.Range(0, comp.Length)]);
                }
            }
        }

        /// <summary>
        /// Finds all RobotAIScripts in the project.
        /// </summary>
        private void FindRobots()
        {
            //Find all AI scripts
            tankListAI = FindDerivedTypes(typeof(RobotControl)).ToArray();
            string debugMessage = "Found " + tankListAI.Length + " AI scripts: ";
            foreach (System.Type type in tankListAI)
            {
                debugMessage += type.Name + ", ";
            }
            Debug.Log(debugMessage);
        }
        /// <summary>
        /// Finds all the preset Spawn Locations.
        /// </summary>
        private void FindSpawnLocations()
        {
            //Find the spawnpoint locations
            spawnPoints = new SpawnPoint[transform.GetChild(0).childCount];
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Transform child = transform.GetChild(0).GetChild(i);
                spawnPoints[i] = new SpawnPoint(child.position, child.eulerAngles.y);
            }
        }
        /// <summary>
        /// Gathers all derived types from a given baseType
        /// </summary>
        private IEnumerable<System.Type> FindDerivedTypes(System.Type baseType)
        {
            Assembly assembly = Assembly.GetAssembly(baseType);
            return assembly.GetTypes().Where(t => t != baseType && baseType.IsAssignableFrom(t));
        }

        void Start()
        {
            transform.GetChild(1).GetChild(3).gameObject.SetActive(true);
        }
    }
}