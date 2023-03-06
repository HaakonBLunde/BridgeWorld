using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.IO;

public class SimulationScript : MonoBehaviour
{

    public int numberOfExperiments;
    public int CurrentExperimentNumber;

    public int numberOfAgents;
    public int startingEnergy;
    public int maximumEnergy;
    public float foodValue; //How much energy they get from eating food
    public int maxAge;
    public int iterations;
    public int iterationCount;
    public int currentlyLiving;
    public int startingEType; //Can be disabled for random start

    public int foodLocation;
    public int foodLocationChangeFrequency; //Lower = more often
    public int foodLocationCount;

    public float speed; //Amount of time between cycles - lower = faster
    public float mutationRate; // 0 = 0% chance, 1 = 100% chance
    public float AgeRate; // Chance of death = Age / AgeRate - 1, e.g. 10/15-1 = 0.5 = 50%

    public float fallingChance;
    public float eRate; //How much agent e-value increase/decrease given an action
    public float learningRate; //How much agent virtues increase/decrease given an action
    public bool ReproductionEnabled;
    public bool LearningEnabled;
    public bool AskForLocationEnabled;
    public bool HelpFallenEnabled;
    public bool BegForFoodEnabled;
    public bool MoralExemplarEnabled;
    

    public Text IterationText;
    public GameObject FacePrefab;
    public GameObject FoodPrefab;
    private string DataString;

    private List<int> FallenList;
    private List<int> FallenLocationList;
    private float[][] Agents;
    private float[][] SentimentValues;

    private string DeathAveragesCollected;

    public int StarvDeath;
    public int DrownDeath;

    //1. Start - initialize agents
    //2. WriteData & save to file
    //3. UpdateFoodLocation & reset agent memory of foodLocation
    //4. AskForFoodLocation
    //5. MoveToFood
    //6. HelpFallen
    //7. MoveBack
    //8. BegForFood
    //9. EatingAndStarving
    //10. MoralExemplar
    //11. Rebirth
    //12. RepeatSimulation -> repeat 2-10

    void Start()
    {

        currentlyLiving = numberOfAgents;
        iterationCount = 0;
        foodLocationCount = 0;
        StarvDeath = 0;
        DrownDeath = 0;
        CurrentExperimentNumber += 1;
        Agents = new float[numberOfAgents][]; //creates an array containing selected amount of empty arrays
        SentimentValues = new float[numberOfAgents][]; //array keeping track of relations
        FallenList = new List<int>(); //Declare list
        FallenLocationList = new List<int>(); //Declare list

        //Set initial values
        // 0 = Energy - 0 to maximumEnergy
        // 1 = V1 = Courage - -1 to +1
        // 2 = V2 = Generosity - -1 to +1
        // 3 = V3 = Honesty - -1 to +1
        // 4 = Eudaemonia type - 0 1 2 3 ... n
        // 5 = Eudaemonia value - -1 to +1
        // 6 = Location - 0 1 2 3 or 4
        // 7 = FoodLocationMemory - 0 1 2 3 or 4
        // 8 = FallenOver - 0 1 2 3 or 4 (0 if false, otherwise at location)
        // 9 = DeadOrAlive - 0 = dead / 1 = alive
        // 10 = Age of agent
        for (int i = 0; i < Agents.Length; i++)
        {
            Agents[i] = new float[11]; //make every array the size of X with floats
            
            //[i][j] - i = the one having the value & j = i's value toward j
            SentimentValues[i] = new float[numberOfAgents]; //make every array the size of X with floats

            Agents[i][0] = startingEnergy;
            Agents[i][1] = UnityEngine.Random.Range(-1.0f, 1.0f); // Random courage
            Agents[i][2] = UnityEngine.Random.Range(-1.0f, 1.0f); // Random generosity
            Agents[i][3] = UnityEngine.Random.Range(-1.0f, 1.0f); // Random honesty
            Agents[i][4] = startingEType; // UnityEngine.Random.Range(0, 3); //E-type - value to be increased - 0 or 1 or 2
            Agents[i][5] = 0; //E-value - amount of happiness (given e-type)
            Agents[i][6] = 0; //Location - Starting location
            Agents[i][7] = 0; //FoodLocationMemory, 0 if 'doesn't know', 1 2 3 4 if they know
            Agents[i][8] = 0; //Fallen over at location (0 if false)
            Agents[i][9] = 1; //Alive = 1 - Dead = 0.
            Agents[i][10] = 0; //Age


        }

        //for (int i = 0; i < Agents.Length; i++)
        //{
            //Debug agent starting info
        //    string agentInfo = string.Join(", ", Agents[i].Select(p => p.ToString()).ToArray());
        //    DataString += "\n" + agentInfo; // Save data to string
        //    Debug.Log ("Agent " + i + " = " + agentInfo);
        //}

        //VisualizeLocation();
        Invoke("WriteData", speed);
    }

    void WriteData()
    {
        float averageVirtue1 = 0;
        float averageVirtue2 = 0;
        float averageVirtue3 = 0;
        float averageEnergy = 0;
        float averageAge = 0;
        float DeathPerIteration = 0;

        for (int i = 0; i < Agents.Length; i++)
        {
            averageVirtue1 += Agents[i][1];
            averageVirtue2 += Agents[i][2];
            averageVirtue3 += Agents[i][3];
            averageEnergy += Agents[i][0];
            averageAge += Agents[i][10];
        }

        averageVirtue1 = averageVirtue1 / numberOfAgents;
        averageVirtue2 = averageVirtue2 / numberOfAgents;
        averageVirtue3 = averageVirtue3 / numberOfAgents;
        averageEnergy = averageEnergy / numberOfAgents;
        averageAge = averageAge / numberOfAgents;

        if (iterationCount == 0)
        {
            DeathPerIteration = 0.0f;
        }
        else
        {
            DeathPerIteration = (((float)DrownDeath + (float)StarvDeath) / (float)iterationCount) / 5.0f;
        }

        float[] collectedInfo = 
        {iterationCount, averageVirtue1, averageVirtue2, averageVirtue3,
        averageEnergy, averageAge, DrownDeath, 
        StarvDeath, DrownDeath+StarvDeath, DeathPerIteration};
        //string stringInfo = string.Join(", ", collectedInfo.Select(p => p.ToString()).ToArray());
        string stringInfo = string.Join(", ", collectedInfo);
        DataString += "\n" + stringInfo; // Save data to string

        if (iterationCount == iterations - 1)
        {
            DeathAveragesCollected += "\n" + DeathPerIteration; // Save data to string
        }

        string path = "Assets/Resources/data.txt";
        File.WriteAllText(path, ""); //clear text - är det denna som tar tid? Nej
        StreamWriter writer = new StreamWriter(path, true);
        writer.Write(DataString);
        writer.Close();

        //Re-import the file to update the reference in the editor
        AssetDatabase.ImportAsset(path);

        if (iterationCount == 0)
        {
            Debug.Log("AVERAGE VIRTUE 1 - COURAGE - AT START: " + averageVirtue1);
            Debug.Log("AVERAGE VIRTUE 2 - GENEROSITY - AT START: " + averageVirtue2);
            Debug.Log("AVERAGE VIRTUE 3 - HONESTY - AT START: " + averageVirtue3);
        }

        UpdateFoodLocation();
    }

    void UpdateFoodLocation()
    {
        //Update food location//////////////////
        if (foodLocationCount == 0)
        {
            GameObject[] ls = GameObject.FindGameObjectsWithTag("Food");
            foreach (GameObject li in ls)
                GameObject.Destroy(li);

            foodLocation = UnityEngine.Random.Range(1, 5); //Food either 1 2 3 4
            Vector3 foodPos = new Vector3 (0,0,0);
            //SpawnFood at location
            if (foodLocation == 1) //At location 1 (TopRight)
            {
                foodPos = new Vector3(1.2f, 1.2f, 0);
            }
            else if (foodLocation == 2) //At location 2 (BottomRight)
            {
                foodPos = new Vector3(1.7f, -1.0f, 0);
            }
            else if (foodLocation == 3) //At location 3 (BottomLeft)
            {
                foodPos = new Vector3(-1.7f, -1.1f, 0);
            }
            else if (foodLocation == 4) //At location 4 (TopLeft)
            {
                foodPos = new Vector3(-1.3f, 1.2f, 0);
            }

            Instantiate(FoodPrefab,foodPos,Quaternion.identity);

            //RESET AGENT MEMORY OF FOODLOCATION
            for (int i = 0; i < Agents.Length; i++)
            {
                Agents[i][7] = 0;
            }
        }

        foodLocationCount += 1;

        //Reset food location//////////////////
        if (foodLocationCount >= foodLocationChangeFrequency)
        {
            foodLocationCount = 0;
        }

        //Debug.Log("Current food location: " + foodLocation);

        if(AskForLocationEnabled == true)
        {
            AskForLocation();
        }
        else
        {
            MoveToFood();
        }
    }

    void AskForLocation ()
    {
        //Agents only ask if 1. It is not a new food location 2. at least 2 is alive & V2 is not > 0.5 
        if (foodLocationCount != 1 && currentlyLiving > 1) //If 1 - then its just updated
        {
            for (int i = 0; i < Agents.Length; i++)
            {
                //If they are ALIVE and DONT KNOW the location AND are not too selfless
                if (Agents[i][9] == 1 && Agents[i][7] == 0 && Agents[i][2] < 0.5f)
                {
                    //Pick ONE agent randomly - excluding themselves
                    //Debug.Log(i + " AND " + RandomAliveOther(i));
                    //int otherAgent = -1;
                    int otherAgent = RandomAliveOther(i);
                    //Debug.Log("OTHER AGENT !!!!!! " + otherAgent);

                    //If otherAgent kNOWS the food location
                    if (Agents[otherAgent][7] == foodLocation)
                    {
                        //If they are honest - tell correct location
                        if (Agents[otherAgent][3] > 0.0f)
                        {
                            Agents[i][7] = foodLocation; //Tell correct location
                            Learning(otherAgent, 5, 3, 0.0f); //TruthTeller gets learning reward (actionType 5, virtue 3, dummy float)
                            SentimentValues[i][otherAgent] += 1.0f; //The one who asks likes the truthteller, += 1
                            //Debug.Log(otherAgent + " TOLD TRUTH TO " + i);
                        }
                        else //Lie - immediate disapproval instead of "finding out later"
                        {
                            Learning(otherAgent, 6, 3, 0.0f); //Lier gets learning reward (actionType 6, virtue 3, dummy float)
                            SentimentValues[i][otherAgent] += -1.0f; //The one who asks dislikes the lier, -= 1
                            //Debug.Log(otherAgent + " LIED TO " + i);
                        }
                    }
                    else //If otherAgent DOES NOT know food locaton - they can still lie
                    {
                        //If they are deceiftul - tell wrong location
                        if (Agents[otherAgent][3] < 0.0f)
                        {
                            Learning(otherAgent, 6, 3, 0.0f); //Lier gets learning reward (actionType 6, virtue 3, dummy float)
                            SentimentValues[i][otherAgent] += -1.0f; //The one who asks dislikes the lier, -= 1
                            //Debug.Log(otherAgent + " LIED TO " + i);
                        }
                    }
                }
            }
        }

        //Invoke("MoveToFood", speed);
        MoveToFood();
    }


    void MoveToFood()
    {
        FallenList.Clear(); //Reset list of fallen
        FallenLocationList.Clear(); //Reset list of fallen

        ////////MOVING AND FALLING/////////
        for (int i = 0; i < Agents.Length; i++)
        {
            //If they have maximum energy OR if they are dead - they don't move
           if (Agents[i][0] < maximumEnergy && Agents[i][9] == 1)
           {
            //If they DONT know where the food is
            if (Agents[i][7] != foodLocation)
            {
                //Move to a random island
                Agents[i][6] = UnityEngine.Random.Range(1, 5);
            }
            else
            {
                //If they DO know where there is energy - move to it
                Agents[i][6] = Agents[i][7];
            }
            //Falling
            if (FallingDice() == true)
            {
                Agents[i][8] = Agents[i][6]; //Fallen at location [6]
                //Debug.Log("Agent " + i + " fell into the water at " + Agents[i][6]);
                FallenList.Add(i); //Add agent to the fallenList
                FallenLocationList.Add((int)Agents[i][6]); //Store location where someone has fallen
            }
           }
        }
        ///////////////////////////////////
        //VisualizeLocation();

        if(HelpFallenEnabled == true)
        {
            Invoke("HelpFallen", speed);
        }
        else
        {
            Invoke("MoveBack", speed);
        }

    }

    void HelpFallen()
    {
        //HELPING / IGNORING

        //string fallz = string.Join(", ", FallenList.Select(p => p.ToString()).ToArray());
        //Debug.Log(fallz); //Print list of fallen agents
        //string fallz2 = string.Join(", ", FallenLocationList.Select(p => p.ToString()).ToArray());
        //Debug.Log(fallz2); //Print list of fallen agents
       // Debug.Log(FallenList.Count);
       // Debug.Log(FallenLocationList.Count);

        for (int i = 0; i < FallenList.Count; i++) //Loop every fallen agent
        {
            //Return list of possible saviors based on location
            List<int> Saviors = FindSaviors(FallenLocationList[i]);

            bool dead = true;

            //How dangerous it is
            float streamLevel = UnityEngine.Random.Range(-1.0f, 1.0f);
            //Debug.Log("Stream level: " + streamLevel);

            //If there are any saviors
            if (Saviors.Count != 0)
            {
                //For every savior for fallen agent
                for (int ii = 0; ii < Saviors.Count; ii++)
                {
                    //If courage of the savior is great enough - save agent
                    if (Agents[Saviors[ii]][1] > streamLevel)
                    {
                        float coinToss = UnityEngine.Random.Range(-1.0f, 1.0f);

                        if (coinToss > streamLevel) //Saving is successful
                        {
                            Agents[FallenList[i]][8] = 0; //Not fallen anymore
                            //Debug.Log("Agent " + Saviors[ii] + " was BRAVE and saved Agent " + FallenList[i]);
                            SentimentValues[FallenList[i]][Saviors[ii]] += 1.0f; //Fallen agents sentiment of hero += 1
                            Learning(Saviors[ii], 1, 1, streamLevel); //Action 1: Saving agent - given virtue 1 - given streamLevel
                            dead = false;
                            break;
                        }
                        else
                        {
                            //Both die
                            Agents[Saviors[ii]][9] = 0;
                            Debug.Log("Agent " + Saviors[ii] + " died trying to save Agent " + FallenList[i]);
                            DrownDeath += 1;
                            break; //ev. låta dom andra försöka rädda också
                        }
                    }
                    else
                    {
                        //Debug.Log("Agent " + Saviors[ii] + " was a COWARD and did not saved Agent " + FallenList[i]);
                        SentimentValues[FallenList[i]][Saviors[ii]] -= 1.0f; //Fallen agents sentiment of coward -= 1
                        Learning(Saviors[ii], 2, 1, streamLevel); //Action 2: Ignoring fallen agent given virtue 1
                    }
                }
            }

            if (dead == true) //No saviors or only cowards
            {
                Agents[FallenList[i]][9] = 0;
                Debug.Log("Agent " + FallenList[i] + " died");
                DrownDeath += 1;
            }

            //break; //terminates loop
        }

        //Invoke("MoveBack", speed);
        MoveBack();

    }

    void MoveBack()
    {

        for (int i = 0; i < Agents.Length; i++)
        {
            //GET ENERGY IF THEY ARE AT FOOD LOCATION
            if (Agents[i][6] == foodLocation)
            {
                Agents[i][0] += foodValue;

                //Remember food location
                Agents[i][7] = Agents[i][6];
            }

            Agents[i][6] = 0; //MOVE BACK HOME
            Agents[i][8] = 0; //RESET FALLING INTO WATER
        }

        //VisualizeLocation();
        //MoveToFood();

        //Invoke("BegForFood", speed);
        BegForFood();
    }

    void BegForFood()
    {
        //Beg for food function

        if (currentlyLiving > 1 && BegForFoodEnabled == true) //Has to be at least 2 alive
        {
            for (int i = 0; i < Agents.Length; i++)
            {
                float selfishness = 0; //reset
                float hunger = 0; //reset
                selfishness = 1 - ((Agents[i][2] + 1) / 2);
                hunger = 1 - (Agents[i][0] / (float)maximumEnergy);
                float begFactor = hunger * selfishness; //The more hungry and selfish -> more likely to ask
                
                if (Agents[i][9] == 1 && begFactor > 0.2f)
                {

                    //Debug.Log("Agent " + i + " hunger " + hunger + " X selfishness " + selfishness + " = " + begFactor + " GREEDY AND HUNGRY ENOUGH TO ASK");
                    int otherAgent = RandomAliveOther(i);

                    float otherAgentHunger = 1 - (Agents[otherAgent][0] / (float)maximumEnergy);
                    float otherAgentSelfishness = 1 - ((Agents[otherAgent][2] + 1) / 2);
                    float otherAgentBegFactor = otherAgentHunger * otherAgentSelfishness;

                    //OtherAgentHunger - OwnHunger = how selfless or selfish you (otherAgent) are
                    //For instance, if Your Hunger = 0.9, other is 0.2 = you are 0.7 more full -> too selfless
                    //If Your Hunger = 0.2, other is 0.7 = -0.5 less full -> too selfish

                    //Debug.Log("Agent " + otherAgent + " hunger " + otherAgentHunger + " X selfishness " + otherAgentSelfishness + " = " + otherAgentBegFactor + " - Is it over 0.15?");
                    
                    //if (otherAgentBegFactor < 0.15f) //If they are non-selfish and non-full enough
                    if (Agents[otherAgent][2] > 0.0f) //If they are generous enough
                    {
                        Debug.Log("Agent " + otherAgent + " was NICE and GAVE FOOD to " + i);
                        Agents[i][0] += foodValue;
                        Agents[otherAgent][0] -= foodValue;
                        Learning(otherAgent, 3, 2, (otherAgentHunger - hunger)); //Action 3 - give to hungry
                        SentimentValues[i][otherAgent] += 1.0f; //The one who begs likes the giver, += 1
                    }
                    else
                    {
                        //Debug.Log("Agent " + otherAgent + " was MEAN and DID NOT GIVE food to " + i);
                        Learning(otherAgent, 4, 2, (otherAgentHunger - hunger)); //Action 4 - ignore hungry
                        SentimentValues[i][otherAgent] -= 1.0f; //The one who begs dislikes the giver, -= 1
                    }
                }
            }
        }
        //Invoke("DeathRebirth", speed);
        EatingAndStarving();
    }

    void EatingAndStarving()
    {

        //DEATH BY STARVATION   
        for (int i = 0; i < Agents.Length; i++)
        {
            Agents[i][0] -=1; //CONSUME 1 FOOD
            Agents[i][10] +=1; //AGE ONE ITERATION

            //Check (if energy is below 1 AND agent is not already dead
            if (Agents[i][0] < 1 && Agents[i][9] == 1)
            {
                Agents[i][9] = 0;
                Debug.Log("Agent " + i + " died of STARVATION");
                StarvDeath += 1;
            }


            //Death by aging here?
        }

        MoralExemplar();
    }


    void MoralExemplar()
    {
        if (MoralExemplarEnabled == true)
        {            
            for (int i = 0; i < Agents.Length; i++)
            {
                if (Agents[i][9] == 1) //Check if it is alive
                {
                    int otherAgent = RandomAliveOther(i); //Pick a random other

                    //Moral exemplar conditions
                    //1. Same e-type (4) //Currently unnecessary since we only do one e-type at a time
                    //2. Higher e-value (5)
                    //3. Older or same age (10)
                    //X. Agent like them (sentimental value)
                    //if (Agents[otherAgent][5] > Agents[i][5] && Agents[otherAgent][10] >= Agents[i][10])
                    if (Agents[otherAgent][5] > Agents[i][5])
                    {
                        Agents[i][1] = Agents[otherAgent][1];
                        Agents[i][2] = Agents[otherAgent][2];
                        Agents[i][3] = Agents[otherAgent][3];
                        Debug.Log("Agent " + i + " copied the virtues of agent " +  otherAgent + " ------------ MORAL EXEMPLAR!!!");
                    }
                }
            }
        }


        if (ReproductionEnabled == true)
        {
            Rebirth();
        }
        else
        {
            RepeatSimulation(); //Skip to repeat simulation
        }
    }

    void Rebirth()
    {
        //Everyone who is dead will be reborn as someone new
        for (int i = 0; i < Agents.Length; i++)
        {
            if (Agents[i][9] == 0) //If they are dead
            {

                //Do the parents here and check if index is out of range

            
                if (UnityEngine.Random.value > mutationRate) //Random.value returns number between 0.0 and 1.0 (inclusive)
                {
                //SIMPLE - Take two random other agents as parents
                //ADVANCED - Parents have same e-type & "like" eachother (no negative sentiment-values)

                //1. Pick two random agents that are 1. alive, 2. not the same
                int parentOne = RandomAliveOther(i); //i based on dead agent
                int parentTwo = RandomAliveOther(parentOne); //based on first parent

                string parent1Info = string.Join(", ", Agents[parentOne].Select(p => p.ToString()).ToArray());
                string parent2Info = string.Join(", ", Agents[parentTwo].Select(p => p.ToString()).ToArray());

                Debug.Log("Agent " + i + " will be reborn - having " + parentOne + " = " + parent1Info + " and " + parentTwo + " = " + parent2Info + " as parents");

                //2. Add and divide Energy V1 V2 V3 Etype Evalue to get child values
                float childEnergy = (Agents[parentOne][0] + Agents[parentTwo][0]) / 2;
                float childV1 = (Agents[parentOne][1] + Agents[parentTwo][1]) / 2;
                float childV2 = (Agents[parentOne][2] + Agents[parentTwo][2]) / 2;
                float childV3 = (Agents[parentOne][3] + Agents[parentTwo][3]) / 2;
                float childEType = Agents[parentOne][4]; //Simply take etype of one of them
                float childEValue = (Agents[parentOne][5] + Agents[parentTwo][5]) / 2;

                //3. Set new child values - reset the rest of the values - location, foodmemory, fallen, alive: Agents[i][9] = 1)
                Agents[i][0] = (int)Math.Round(childEnergy, 0); //Energy
                Agents[i][1] = childV1; //V1 = Courage -1 to +1
                Agents[i][2] = childV2; //V2 = Generosity -1 to +1
                Agents[i][3] = childV3; //V3 = Honesty -1 to +1
                Agents[i][4] = childEType; //E-type
                Agents[i][5] = childEValue; //E-value
                Agents[i][6] = 0; //Location - Starting location
                Agents[i][7] = 0; //FoodLocationMemory, 0 if 'doesn't know', 1 2 3 4 if they know
                Agents[i][8] = 0; //Fallen over at location (0 if false)
                Agents[i][9] = 1; //Alive = 1 - Dead = 0.
                Agents[i][10] = 0; //Age - reset to 0

                string newChildInfo = string.Join(", ", Agents[i].Select(p => p.ToString()).ToArray());
                Debug.Log("Agent " + i + " is reborn with " + newChildInfo);
                }
                else //If mutation happens
                {
                    Agents[i][0] = startingEnergy; //Energy
                    Agents[i][1] = UnityEngine.Random.Range(-1.0f, 1.0f); // Random courage
                    Agents[i][2] = UnityEngine.Random.Range(-1.0f, 1.0f); // Random generosity
                    Agents[i][3] = UnityEngine.Random.Range(-1.0f, 1.0f); // Random honesty
                    Agents[i][4] = startingEType; //UnityEngine.Random.Range(0, 2); //E-type 0 or 1
                    Agents[i][5] = 0; //E-value
                    Agents[i][6] = 0; //Location - Starting location
                    Agents[i][7] = 0; //FoodLocationMemory, 0 if 'doesn't know', 1 2 3 4 if they know
                    Agents[i][8] = 0; //Fallen over at location (0 if false)
                    Agents[i][9] = 1; //Alive = 1 - Dead = 0.
                    Agents[i][10] = 0; //Age - reset to 0

                    string newChildInfo = string.Join(", ", Agents[i].Select(p => p.ToString()).ToArray());
                    Debug.Log("Agent " + i + " MUTATED MUTATED MUTATED into " + newChildInfo);
                }

                //Reset SentimentValues of the dead so that EVERY agent sets their SentimentValue for Agents[i] to 0
                //EVERY AGENT should reset their sentiment value for the dead one
                for (int k = 0; k < Agents.Length; k++)
                {
                    SentimentValues[k][i] = 0; //Every agent (k) reset sentiment values of the dead one (i)
                    SentimentValues[i][k] = 0; //Every dead agent (i) reset their sentiment values of every other agent (k)
                }
            }
        }

        RepeatSimulation();
    }

    void RepeatSimulation()
    {

        float averageVirtue1 = 0;
        float averageVirtue2 = 0;
        float averageVirtue3 = 0;
        currentlyLiving = 0;

        //DEBUG INFO & Save currently living
        for (int i = 0; i < Agents.Length; i++)
        {
            string agentInfo = string.Join(", ", Agents[i].Select(p => p.ToString()).ToArray());
            //DataString += "\n" + agentInfo; // Save data to string
            Debug.Log ("Agent " + i + " = " + agentInfo);
            averageVirtue1 += Agents[i][1];
            averageVirtue2 += Agents[i][2];
            averageVirtue3 += Agents[i][3];

            if (Agents[i][9] == 1)
            {
                currentlyLiving += 1;
            }
        }

        //DEBUG SENTIMENT VALUES
        //for (int i = 0; i < SentimentValues.Length; i++)
        //{
        //    string agentInfo = string.Join(", ", SentimentValues[i].Select(p => p.ToString()).ToArray());
        //    //DataString += "\n" + agentInfo; // Save data to string
        //    Debug.Log ("Sentiment values for Agent " + i + " = " + agentInfo);
        //}

        Debug.Log ("ITERATION: " + iterationCount + " // CURRENTLY LIVING: " + currentlyLiving);
        averageVirtue1 = averageVirtue1 / numberOfAgents;
        averageVirtue2 = averageVirtue2 / numberOfAgents;
        averageVirtue3 = averageVirtue3 / numberOfAgents;
        Debug.Log("AVERAGE VIRTUE 1 - COURAGE: " + averageVirtue1);
        Debug.Log("AVERAGE VIRTUE 2 - GENEROSITY: " + averageVirtue2);
        Debug.Log("AVERAGE VIRTUE 3 - HONESTY: " + averageVirtue3);

        iterationCount += 1;
        IterationText.text = iterationCount.ToString("0");
        if (iterationCount < iterations && currentlyLiving > 0)
        {
            //Debug.Log ("NUMBER OF AGENTS " + Agents.Length);
            Invoke("WriteData", speed);
        }
        else
        {
            //DEBUG SENTIMENT VALUES
            for (int i = 0; i < SentimentValues.Length; i++)
            {
                string agentInfo = string.Join(", ", SentimentValues[i].Select(p => p.ToString()).ToArray());
                //DataString += "\n" + agentInfo; // Save data to string
                Debug.Log ("Sentiment values for Agent " + i + " = " + agentInfo);
            }

            Debug.Log("EXPERIMENT NUMBER: " + CurrentExperimentNumber + " ENDED AT ITERATION " + iterationCount  + " // CURRENTLY LIVING: " + currentlyLiving);
            //WriteString ();

            if (CurrentExperimentNumber < numberOfExperiments)
            {
                Start();
            }
            else
            {
                Debug.Log (DeathAveragesCollected);
            }


        }
    }


    //Visualize location of all agents
    void VisualizeLocation()
    {

        //Destroy all faces
        GameObject[] ls = GameObject.FindGameObjectsWithTag("Face");
        foreach (GameObject li in ls)
            GameObject.Destroy(li);

        //For each agent - chech their location and spawn a smiley face at some location near one of the 9 spots
        //Spread out a bit so they don't stack the same position
        for (int i = 0; i < Agents.Length; i++)
        {
            float randVal1 = UnityEngine.Random.Range(-0.25f, 0.25f);
            float randVal2 = UnityEngine.Random.Range(-0.25f, 0.25f);
            Vector3 facePos = new Vector3 (0,0,0);
            
            if (Agents[i][6] == 0) //If they are home
            {
                facePos = new Vector3(randVal1, randVal2, 0);

            }
            else if (Agents[i][6] == 1) //At location 1 (TopRight)
            {
                facePos = new Vector3(1.2f + randVal1, 1.2f + randVal2, 0);
                if (Agents[i][8] == 1) //Fallen over
                {
                    facePos = new Vector3(1.0f + randVal1, 0.5f + randVal2, 0);
                }
            }
            else if (Agents[i][6] == 2) //At location 2 (BottomRight)
            {
                facePos = new Vector3(1.7f + randVal1, -1.0f + randVal2, 0);
                if (Agents[i][8] == 2) //Fallen over
                {
                    facePos = new Vector3(1.3f + randVal1, -0.6f + randVal2, 0);
                }
            }
            else if (Agents[i][6] == 3) //At location 3 (BottomLeft)
            {
                facePos = new Vector3(-1.7f + randVal1, -1.1f + randVal2, 0);
                if (Agents[i][8] == 3) //Fallen over
                {
                    facePos = new Vector3(-0.7f + randVal1, -0.8f + randVal2, 0);
                }
            }
            else if (Agents[i][6] == 4) //At location 4 (TopLeft)
            {
                facePos = new Vector3(-1.3f + randVal1, 1.2f + randVal2, 0);
                if (Agents[i][8] == 4) //Fallen over
                {
                    facePos = new Vector3(-1.2f + randVal1, 0.5f + randVal2, 0);
                }
            }
            
            //if (Agents[i][9] == 0)
            //{Spawn death-face}

            //GameObject go = Instantiate(FacePrefab, facePos, Quaternion.identity);
            Instantiate(FacePrefab,facePos, Quaternion.identity);

        }

    }

    //Roll dice - if true - fallen over
    bool FallingDice()
    {
            if (UnityEngine.Random.value < fallingChance)
            {
                return true;
            }
        return false;
    }

    int RandomAliveOther(int i)
    {

        List<int> ToAskList = new List<int>(); //Create list
        //Conditions:
        //1. They are not dead
        //2. They are not the agent

        for (int j = 0; j < Agents.Length; j++)
        {
            if (Agents[j][9] == 1 && j != i)
            {
                ToAskList.Add(j);
            }
        }

        //Select and return random from list
        int ranNum = UnityEngine.Random.Range(0, ToAskList.Count);
        return ToAskList[ranNum];
    }

    List<int> FindSaviors(int i)
    {

        List<int> Saviors = new List<int>(); //Create list

        //Conditions:
        //1. Is on the same location (i)
        //2. Has not fallen themselves Agents[][8] = 0;
        //3. Is not dead Agents[][9] = 1;

        for (int j = 0; j < Agents.Length; j++)
        {

            if (Agents[j][6] == i && Agents[j][9] == 1 && Agents[j][8] == 0) //Three conditions
            {
                Saviors.Add(j);
                //Debug.Log(j + " is an alive agent at same location that hasn't fallen in");
            }
        }

        string relevantSaviors = string.Join(", ", Saviors.Select(p => p.ToString()).ToArray());
        //Debug.Log("POSSIBLE saviors for = " + relevantSaviors); //Print list of possible NBs

        //Shuffle the list
        List<int> shuffledNumbers = new List<int>(); //List with shuffled order
        
        for (int k = 0; k < Saviors.Count; k++)
        {
            int ranNum = Saviors[UnityEngine.Random.Range(0, Saviors.Count)]; //generates random number
            shuffledNumbers.Add(ranNum); //adds the random number to shuffledNumbers
            Saviors.Remove(ranNum); //removes the used ones
        }

        string shuff = string.Join(", ", shuffledNumbers.Select(p => p.ToString()).ToArray());
        //Debug.Log("SHUFFLED ORDER = " + shuff); //Print list of possible NBs

        //Cut the list - depending on NumberOfAgents - 1/10? So 10 if 100, 1 if 10
        //Do this in the save/ignore action instead
        
        return shuffledNumbers;
    }

    //Learning feedback based on eudaemonia type and action
    //Input AGENT who did what ACTION given what VIRTUE
    void Learning(int agentIndex, int actionType, int virtueType, float factor)
    {

        if (LearningEnabled == true)
        {

        //Action types:
        //1 - Save fallen agent (Virtue 1 - Courage)
        //2 - Ignore fallen agent (Virtue 1 - Courage)
        //3 - Give to begging agent (Virtue 2 - Generosity)
        //4 - Ignore begging agent (Virtue 2 - Generosity)
        //5 - Answer honestly to asking agent (Virtue 3 - Honesty)
        //6 - Lie to asking agent (Virtue 3 - Honesty)
        //7 ...

        //Virtue types:
        //1 = Courage
        //2 = Generosity
        //3 = Honesty

        //Eudaemonia types:
        //0 - Increase moral praise (assume that other agent has a reaction)
        //1 - Increase energy-level of self
        //2 - Balance between moral selfish and selfless

        float currentEValue = Agents[agentIndex][5];

        
        if (actionType == 1) //1 - Save fallen agent (Virtue 1 - Courage)
        {
            if (Agents[agentIndex][4] == 0) //If E-type = 0
            {
                Agents[agentIndex][5] += eRate; //"Approval"
                Debug.Log("Agent " + agentIndex + " got an approval for SAVING FALLEN, eudaemonia INCREASED");
            }
            else if (Agents[agentIndex][4] == 1) //If E-type = 1
            {
                Agents[agentIndex][5] -= eRate; //"BAD - risk for self"
                Debug.Log("Agent " + agentIndex + "did something bad for self-survival, e-value DECREASED");
            }
            else if (Agents[agentIndex][4] == 2) //If E-type = 2
            {
                Debug.Log(factor);
                if(factor < 0.0f) //If the stream was less than half of maximum - good job
                {
                    Agents[agentIndex][5] += eRate; //"GOOD"
                    Debug.Log("Agent " + agentIndex + " saved fallen when the risk was LOW, eudaemonia INCREASED");
                }
                else //If the stream was more than half of maximum - bad job
                {
                    Agents[agentIndex][5] -= eRate; //"BAD"
                    Debug.Log("Agent " + agentIndex + " saved fallen when the risk was TOO HIGH, eudaemonia DECREASED");
                }
            }
        }
        else if (actionType == 2) //2 - Ignore fallen agent (Virtue 1 - Courage)
        {
            if (Agents[agentIndex][4] == 0) //If E-type = 0
            {
                Agents[agentIndex][5] -= eRate; //"Disapproval"
                Debug.Log("Agent " + agentIndex + " got a disapproval for IGNORING FALLEN, eudaemonia DECREASED");
            }
            else if (Agents[agentIndex][4] == 1) //If E-type = 1
            {
                Agents[agentIndex][5] += eRate; //"GOOD - no unecessary risk for self"
                Debug.Log("Agent " + agentIndex + "did something good for self-survival, e-value INCREASED");
            }
            else if (Agents[agentIndex][4] == 2) //If E-type = 2
            {
                Debug.Log(factor);
                if(factor < 0.0f) //If the stream was less than half of maximum - bad job
                {
                    Agents[agentIndex][5] -= eRate; //"BAD"
                    Debug.Log("Agent " + agentIndex + " ignored fallen when the risk was LOW, eudaemonia DECREASED");
                }
                else //If the stream was more than half of maximum - bad job
                {
                    Agents[agentIndex][5] += eRate; //"GOOD"
                    Debug.Log("Agent " + agentIndex + " ignored fallen when the risk was TOO HIGH, eudaemonia INCREASED");
                }
            }
        }
        else if (actionType == 3) //3 - Give to hungry agent (Virtue 2 - Generosity)
        {
            if (Agents[agentIndex][4] == 0) //If E-type = 0
            {
                Agents[agentIndex][5] += eRate; //"Approval"
                Debug.Log("Agent " + agentIndex + " got an approval for GIVING FOOD, eudaemonia INCREASED");
            }
            else if (Agents[agentIndex][4] == 1) //If E-type = 1
            {
                Agents[agentIndex][5] -= eRate; //"BAD - giving is bad for self-energy, e-value DECREASED"
                Debug.Log("Agent " + agentIndex + "did something bad for self-survival, e-value DECREASED");
            }
            else if (Agents[agentIndex][4] == 2) //If E-type = 2
            {
                Debug.Log(factor);
                //GIVING AGENTS HUNGER -MINUS- BEGGING AGENTS HUNGER, E.g.:
                //0.8 (you) - 0.6 (other) = POSIIVE - you gave even if you were more hungry (0.2)
                //0.2 (you) - 0.5 (other) = NEGATIVE - you gave when you had more (0.3)
                if(factor > 0.0f) //If you were hungrier than the other agent - BAD
                {
                    Agents[agentIndex][5] -= eRate; //"BAD"
                    Debug.Log("Agent " + agentIndex + " GAVE food to other agent even if she had less energy, eudaemonia DECREASED");
                }
                else //The other agent were hungrier than you - GOOD JOB
                {
                    Agents[agentIndex][5] += eRate; //"GOOD"
                    Debug.Log("Agent " + agentIndex + " GAVE food to other agent because she had more energy, eudaemonia INCREASED");
                }
            }
        }
        else if (actionType == 4) //4 - Ignore hungry agent (Virtue 2 - Generosity)
        {
            if (Agents[agentIndex][4] == 0) //If E-type = 0
            {
                Agents[agentIndex][5] -= eRate; //"Disapproval"
                Debug.Log("Agent " + agentIndex + " got a disapproval for NOT GIVING FOOD, eudaemonia DECREASED");
            }
            else if (Agents[agentIndex][4] == 1) //If E-type = 1
            {
                Agents[agentIndex][5] += eRate; //"Good - giving is bad for self-energy, e-value INCREASED"
                Debug.Log("Agent " + agentIndex + "did something bad for self-survival, e-value DECREASED");
            }
            else if (Agents[agentIndex][4] == 2) //If E-type = 2
            {
                Debug.Log(factor);
                //GIVING AGENTS HUNGER -MINUS- BEGGING AGENTS HUNGER, E.g.:
                //0.8 (you) - 0.6 (other) = POSIIVE - you did not give when you were more hungry (0.2)
                //0.2 (you) - 0.5 (other) = NEGATIVE - you did not give even if you had more (0.3)
                if(factor > 0.0f) //If you were hungrier than the other agent - GOOD
                {
                    Agents[agentIndex][5] += eRate; //"GOOD"
                    Debug.Log("Agent " + agentIndex + " did NOT give food to other agent because she had less energy, eudaemonia INCREASED");
                }
                else //The other agent were hungrier than you - GOOD JOB
                {
                    Agents[agentIndex][5] -= eRate; //"BAD"
                    Debug.Log("Agent " + agentIndex + " did NOT give food to other agent evem if she had more energy, eudaemonia DECREASED");
                }
            }
        }
        else if (actionType == 5) //5 - Tell TRUTH to asking agent (Virtue 3 - Honesty)
        {
            if (Agents[agentIndex][4] == 0) //If E-type = 0
            {
                Agents[agentIndex][5] += eRate; //"Approval"
                Debug.Log("Agent " + agentIndex + " got an approval for TELLING TRUTH, eudaemonia INCREASED");
            }
            else if (Agents[agentIndex][4] == 1) //If E-type = 1
            {
                //Agents[agentIndex][5] -= eRate; //NEUTRAL
                Debug.Log("Agent " + agentIndex + "telling the truth is neither good nor bad for self-survival");
            }
            else if (Agents[agentIndex][4] == 2) //If E-type = 1
            {
                Agents[agentIndex][5] += eRate; //"GOOD FOR OTHERS"
                Debug.Log("Agent " + agentIndex + "telling TRUTH is good for others, neutral for self-survival, eudaemonia INCREASED");
            }
        }
        else if (actionType == 6) //6 - Tell LIE to asking agent (Virtue 3 - Honesty)
        {
            if (Agents[agentIndex][4] == 0) //If E-type = 0
            {
                Agents[agentIndex][5] -= eRate; //"Disapproval"
                Debug.Log("Agent " + agentIndex + " got a disapproval for TELLING LIE, eudaemonia DECREASED");
            }
            else if (Agents[agentIndex][4] == 1) //If E-type = 1
            {
                //Agents[agentIndex][5] -= eRate; //NEUTRAL
                Debug.Log("Agent " + agentIndex + "telling the truth is neither good nor bad for self-survival");
            }
            else if (Agents[agentIndex][4] == 2) //If E-type = 1
            {
                Agents[agentIndex][5] -= eRate; //"BAD FOR OTHERS"
                Debug.Log("Agent " + agentIndex + "telling lie is bad for others, neutral for self-survival, eudaemonia DECREASED");
            }
        }

        ///////////////////////////////////////////////////////////////
        ///////////Change relevant virtue based on virtueType//////////
        if (Agents[agentIndex][5] > currentEValue) // = it was a 'good' action
        {
            if (!(Agents[agentIndex][4] == 2 && (virtueType == 1 || virtueType == 2))) //Exlude e-type 2 - they only learn through negative rienforcement
            {
                if (Agents[agentIndex][virtueType] > 0) //If it is positive - make it even more positive
                {
                    Agents[agentIndex][virtueType] += learningRate;
                    Debug.Log("Agent " + agentIndex + " +++reinforced+++ positive virtue " + virtueType + " because it led to +++e-value");
                }
                else //If it is negative - make it more negative
                {
                    Agents[agentIndex][virtueType] -= learningRate;
                    Debug.Log("Agent " + agentIndex + " ---reinforced--- negative virtue " + virtueType + " because it led to +++e-value");
                }
            }
        }
        ///Exluding "equal to" as then virtuous should be unchanged
        else if (Agents[agentIndex][5] < currentEValue) // = it was a 'bad' action
        {
            if (Agents[agentIndex][virtueType] > 0) //If it is positive - make it less positive
            {
                Agents[agentIndex][virtueType] -= learningRate;
                Debug.Log("Agent " + agentIndex + " ---reinforced--- positive virtue " + virtueType + " because it led to ---e-value");
            }
            else //If it is negative - make it more positive
            {
                Agents[agentIndex][virtueType] += learningRate;
                Debug.Log("Agent " + agentIndex + " +++reinforced+++ negative virtue " + virtueType + " because it led to ---e-value");
            }
        }
        ///////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////

        //Make sure that virtue stays within range -1.0 to +1.0
        if (Agents[agentIndex][virtueType] > 1.0f)
        {
            Agents[agentIndex][virtueType] = 1.0f;
        }
        else if (Agents[agentIndex][virtueType] < -1.0f) //Make sure that it remains within range -1 to +1
        {
            Agents[agentIndex][virtueType] = -1.0f;
        }


        }
    }
}