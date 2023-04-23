using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System;
using System.IO;
using UnityEditorInternal;

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
    public int numberOfThefts;

    public int foodLocation;
    public int foodLocationChangeFrequency; //Lower = more often
    public int foodLocationCount;

    public float speed; //Amount of time between cycles - lower = faster
    public float mutationRate; // 0 = 0% chance, 1 = 100% chance
    public float AgeRate; // Chance of death = Age / AgeRate - 1, e.g. 10/15-1 = 0.5 = 50%

    public float fallingChance;
    public static float eRate; //How much agent e-value increase/decrease given an action
    public  float learningRate; //How much agent virtues increase/decrease given an action
    public bool ReproductionEnabled;
    public static bool LearningEnabled;
    public bool AskForLocationEnabled;
    public bool HelpFallenEnabled;
    public bool BegForFoodEnabled;
    public bool MoralExemplarEnabled;
    public bool StealingEnabled;

    public float courageShiftThroughDeathAndRebirth;
    public float generosityShiftThroughDeathAndRebirth;
    public float honestyShiftThroughDeathAndRebirth;
    

    public Text IterationText;
    public GameObject FacePrefab;
    public GameObject FoodPrefab;
    //private string DataString;

    private List<Agent> FallenList;
    //private List<int> FallenLocationList;
    //private float[][] Agents;
    private Agent[] Agents;
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
        LearningEnabled = true;
        StealingEnabled = true;
        numberOfExperiments = 1;
        iterations = 200;
        numberOfAgents = 50;
        maximumEnergy = 10;
        eRate = 0.01f;
        startingEnergy = 5;
        foodValue = 1.25f;
        learningRate = 0.1f;
        MoralExemplarEnabled = false;
        Debug.Log("maximum food is " + maximumEnergy);
        Debug.Log("Starting food is " + startingEnergy);
        currentlyLiving = numberOfAgents;
        iterationCount = 0;
        foodLocationCount = 0;
        StarvDeath = 0;
        DrownDeath = 0;
        CurrentExperimentNumber += 1;
        numberOfThefts = 0;

//        Agents = new float[numberOfAgents][]; //creates an array containing selected amount of empty arrays
        Agents = new Agent[numberOfAgents];
        SentimentValues = new float[numberOfAgents][]; //array keeping track of relations
        FallenList = new List<Agent>(); //Declare list
        //FallenLocationList = new List<int>(); //Declare list
        startingEType = 0;
        Array.Clear(Agents, 0, Agents.Length);
        if (CurrentExperimentNumber == 2)
        {
            startingEType = 1;
        }else if (CurrentExperimentNumber == 3)
        {
            startingEType = 2;
        }

        if (CurrentExperimentNumber==4)
        {
            LearningEnabled = false;
        }
/*
        if (CurrentExperimentNumber == 4) //here all agents have random e-types
        {
            for (int i = 0; i < Agents.Length; i++)
            {
                Agents[i] = new Agent(startingEnergy,maximumEnergy, i, learningRate);
            }
        }else
        {*/

            Debug.Log("Start of experiment number " + CurrentExperimentNumber + " starting E-Type is " + startingEType);
            for (int i = 0; i < Agents.Length; i++)
            {
                Agents[i] = new Agent(startingEnergy, startingEType, maximumEnergy, i, learningRate);
            }
        //}
        Invoke("WriteData", speed);
    }

    void WriteData()
    {
        string dataString;
        float averageCourage = 0;
        float averageHonesty = 0;
        float averageGenerosity = 0;
        float averageEnergy = 0;
        float averageAge = 0;
        float DeathPerIteration = 0;
        float AverageEType = 0; 
        //get avg virtues
        int numberOfSelfless = 0;
        int numberOfSelfish = 0;
        int numberOfbalanced = 0;
        for (int i = 0; i < Agents.Length; i++)
        {
            averageCourage += Agents[i].courage;
            averageHonesty += Agents[i].honesty;
            averageGenerosity += Agents[i].generosity;
            averageEnergy += Agents[i].energy;
            averageAge += Agents[i].age;
            AverageEType += Agents[i].eType;
            switch (Agents[i].eType)
            {
                case 0:
                    numberOfSelfless += 1;
                    break;
                case 1:
                    numberOfSelfish += 1;
                    break;
                case 2:
                    numberOfbalanced += 1;
                    break;
            }
        }

        averageCourage = averageCourage / numberOfAgents;
        averageHonesty = averageHonesty / numberOfAgents;
        averageGenerosity = averageGenerosity / numberOfAgents;
        averageEnergy = averageEnergy / numberOfAgents;
        averageAge = averageAge / numberOfAgents;
        AverageEType = AverageEType / numberOfAgents;
        //track deaths
        if (iterationCount == 0)
        {
            DeathPerIteration = 0.0f;
        }
        else
        {
            DeathPerIteration = (((float)DrownDeath + (float)StarvDeath) / (float)iterationCount) / 5.0f;
        }

        float[] collectedInfo = 
        {iterationCount, averageCourage, averageGenerosity, averageHonesty,
        averageEnergy, averageAge, DrownDeath, 
        StarvDeath, DrownDeath+StarvDeath, DeathPerIteration, numberOfThefts,  AverageEType,  numberOfSelfless, numberOfSelfish, numberOfbalanced};
        string stringInfo = string.Join(" ", collectedInfo);
        dataString = stringInfo + "\n"; // Save data to string

        if (iterationCount == iterations - 1)
        {
            DeathAveragesCollected += "\n" + DeathPerIteration; // Save data to string
        }

        string experiment = "exp" + CurrentExperimentNumber;
        string path = "Assets/Resources/" + experiment + "h.txt";
        using (StreamWriter sw = File.AppendText(path))
        {
                sw.Write(dataString);
                sw.Close();
        }
        AssetDatabase.ImportAsset(path);

        if (iterationCount == 0)
        {
            Debug.Log("AVERAGE VIRTUE 1 - COURAGE - AT START: " + averageCourage);
            Debug.Log("AVERAGE VIRTUE 2 - GENEROSITY - AT START: " + averageGenerosity);
            Debug.Log("AVERAGE VIRTUE 3 - HONESTY - AT START: " + averageHonesty);
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
                Agents[i].foodLocationMemory = 0;
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
    {   //some kind of god of war switch case here?
        //Agents only ask if 1. It is not a new food location 2. at least 2 is alive & V2 is not > 0.5 
        if (foodLocationCount != 1 && currentlyLiving > 1) //If 1 - then its just updated
        {
            for (int i = 0; i < Agents.Length; i++)
            {
                Agents[i].askFoodLocation(Agents[RandomAliveOther(Agents[i])], foodLocation);
                
            }
        }

        //Invoke("MoveToFood", speed);
        MoveToFood();
    }


    void MoveToFood()
    {
        int succeses = 0;
        int riverfalls = 0;
        Debug.Log("Time for the agents to find food at iteration " + iterationCount);
        FallenList.Clear(); //Reset list of fallen
        //FallenLocationList.Clear(); //Reset list of fallen

        ////////MOVING AND FALLING/////////
        for (int i = 0; i < Agents.Length; i++)
        {
            //If they have maximum energy OR if they are dead - they don't move
           if (Agents[i].energy < maximumEnergy && Agents[i].alive)
           {
               
            //If they DONT know where the food is
            if (Agents[i].foodLocationMemory != foodLocation)
            {
                //Move to a random island
                int tmp = UnityEngine.Random.Range(1, 5);
                Agents[i].moveTo(tmp);
                if (tmp == foodLocation)
                {
                    succeses++;
                }
            }
            else
            {
                //If they DO know where there is energy - move to it
                Agents[i].moveTo(Agents[i].foodLocationMemory);
                succeses++;
            }
            //Falling
            if (FallingDice() == true)
            {
                Agents[i].fallenInRiver = Agents[i].location; //Fallen at location [6]
                //Debug.Log("Agent " + i + " fell into the water at " + Agents[i][6]);
                FallenList.Add(Agents[i]); //Add agent to the fallenList
                //Debug.Log("Agent " + Agents[i].name + "fell in the river at iteration " + iterationCount );
                //FallenLocationList.Add(Agents[i].fallenInRiver); //Store location where someone has fallen
                riverfalls++;
            }
           }
           
        }
        Debug.Log("Succesfull food location was done " + succeses + " times, " + riverfalls + " agents fell in the river at iteration " + iterationCount);
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
        Debug.Log("There are " + FallenList.Count + " Agents in the river");
        int fallen = FallenList.Count;
        int rescued = 0;

        for (int i = 0; i < FallenList.Count; i++) //Loop every fallen agent
        {
            //Return list of possible saviors based on location
            List<Agent> Saviors = FindSaviors(FallenList[i]);
            int fallenName = FallenList[i].name;
            bool stillInRiver = true;

            //How dangerous it is
            float streamLevel = UnityEngine.Random.Range(-1.0f, 1.0f);
            //Debug.Log("Stream level: " + streamLevel);

            //If there are any saviors
            if (Saviors.Count != 0)
            {
                //For every savior for fallen agent
                for (int ii = 0; ii < Saviors.Count; ii++)
                {
                    if (Agents[fallenName].alive && Agents[fallenName].fallenInRiver != 0) 
                    {
                        if (Agents[Saviors[ii].name].contemplateSaving(streamLevel))
                        {
                            if (Agents[Saviors[ii].name].saveOther(streamLevel, Agents[fallenName]))
                            {
                                Debug.Log("Agent " + Saviors[ii].name + " saved agent " + fallenName + " from the river");
                                Agents[fallenName].isSaved();
                                stillInRiver = false;
                                rescued++; 
                            }else
                            {
                                Agents[fallenName].drowns();
                                stillInRiver = false; //now they're at the bottom of the river
                                DrownDeath += 2;
                                Debug.Log("Agent " + Saviors[ii].name + " died trying to rescue Agent " + i + " who also drowned") ;
                            }
                            
                        }
                    }
                    //If courage of the savior is great enough - save agent
                    
                }
            }

            if (stillInRiver == true) //No saviors or only cowards
            {
                //Agents[FallenList[i]][9] = 0;
                Debug.Log("Agent " + FallenList[i].name + " died from drowning because noone tried to save them");
                DrownDeath += 1;
            }

            //break; //terminates loop
        }
        Debug.Log("Of " + fallen + " fallen " + rescued +" were rescued at iteration " + iterationCount);
        //Invoke("MoveBack", speed);
        MoveBack();

    }

    void MoveBack()
    {

        for (int i = 0; i < Agents.Length; i++)
        {
            Agents[i].MoveBack(foodLocation, foodValue);
        }
        BegForFood();
    }

    void BegForFood()
    {
        //Beg for food function
        if (currentlyLiving > 1 && BegForFoodEnabled == true) //Has to be at least 2 alive
        { 
            
            for (int i = 0; i < Agents.Length; i++)
            {
                if (Agents[i].contemplateBegging())
                {
                    int tmp = RandomAliveOther(Agents[i]);
                    Debug.Log("Begtarget for agent " + i + " is agent " + tmp + " at iteration " + iterationCount);
                    Agents[i].begForFood(Agents[tmp]);
                }
            }
            
        }
        //Invoke("DeathRebirth", speed);
        if (StealingEnabled)
        {
            Stealing();
        }
        else
        {
            EatingAndStarving();
        }
        
        
    }

    void Stealing()
    {
        for (int i = 0; i < Agents.Length; i++)
        {
            Agent tmpOther = Agents[RandomAliveOther(Agents[i])];
            if (Agents[i].contemplateStealing(tmpOther))
            {
                Debug.Log("Agent "+ i+ " with "+ Agents[i].energy + " energy decided to steal from " + tmpOther.name + " who has " + tmpOther.energy+" energy");
                numberOfThefts += 1;
                Agents[i].stealFrom(tmpOther);
                Agents[i].evalStealing(tmpOther);
            }
        }
        EatingAndStarving();
    }

    void EatingAndStarving()
    {
        for (int i = 0; i < Agents.Length; i++)
        {
            if (!Agents[i].eat())
            {
                Debug.Log("Agent " + i + " died of STARVATION with energy "+ Agents[i].energy + " at iteration " + iterationCount);
                StarvDeath += 1; 
            }
        }
            //Death by aging here?
            MoralExemplar();
    }


    void MoralExemplar()
    {
        if (MoralExemplarEnabled)
        {            
            for (int i = 0; i < Agents.Length; i++)
            {
                if (Agents[i].alive) //Check if it is alive
                {
                    Agent otherAgent = Agents[RandomAliveOther(Agents[i])]; 
                    Agents[i].moralExemplar(otherAgent);
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
            if (!Agents[i].alive) //If they are dead
            {
                if (UnityEngine.Random.value > mutationRate) 
                {
                    Agent parentOne = Agents[RandomAliveOther(Agents[i])];
                    Agent parentTwo = Agents[RandomAliveOther(parentOne)];
                    Agents[i] = new Agent(parentOne, parentTwo, i);
                    Debug.Log("Agent "+ i + " was reborn with starting food " + Agents[i].energy);
                }
                else 
                {
                    Debug.Log("Mutation at iteration " + iterationCount);
                    Agents[i] = new Agent(startingEnergy, startingEType, maximumEnergy, i, learningRate);
                    Debug.Log("Agent "+ i + " was reborn with starting food " + Agents[i].energy);
                }
                for (int k = 0; k < Agents.Length; k++)
                {
                    //SentimentValues[k][i] = 0; //Every agent (k) reset sentiment values of the dead one (i)
                    //SentimentValues[i][k] = 0; //Every dead agent (i) reset their sentiment values of every other agent (k)
                }
            }
        }

        RepeatSimulation();
    }
    
    
    void RepeatSimulation()
    {

        float averageCourage = 0;
        float averageHonesty = 0;
        float averageGenerosity = 0;
        currentlyLiving = 0;

        //DEBUG INFO & Save currently living
        for (int i = 0; i < Agents.Length; i++)
        {
            //string agentInfo = string.Join(", ", Agents[i].Select(p => p.ToString()).ToArray());
            //DataString += "\n" + agentInfo; // Save data to string
            //Debug.Log ("Agent " + i + " = " + agentInfo);     TODO: ADD SOMETHING LIKE THIS FOR OOPier AGENTS
            averageCourage += Agents[i].courage;
            averageHonesty += Agents[i].honesty;
            averageGenerosity += Agents[i].generosity;

            if (Agents[i].alive)
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
        averageCourage = averageCourage / numberOfAgents;
        averageHonesty = averageHonesty / numberOfAgents;
        averageGenerosity = averageGenerosity / numberOfAgents;
        Debug.Log("AVERAGE VIRTUE 1 - COURAGE: " + averageCourage);
        Debug.Log("AVERAGE VIRTUE 2 - GENEROSITY: " + averageGenerosity);
        Debug.Log("AVERAGE VIRTUE 3 - HONESTY: " + averageHonesty);

        iterationCount += 1;
        IterationText.text = "experiment number " + CurrentExperimentNumber + "\n" + iterationCount.ToString("0");
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
                //string agentInfo = string.Join(", ", SentimentValues[i].Select(p => p.ToString()).ToArray());
                //DataString += "\n" + agentInfo; // Save data to string
                //Debug.Log ("Sentiment values for Agent " + i + " = " + agentInfo);
            }

            Debug.Log("EXPERIMENT NUMBER: " + CurrentExperimentNumber + " ENDED AT ITERATION " + iterationCount  + " // CURRENTLY LIVING: " + currentlyLiving);
            Debug.Log("AVERAGE VIRTUE 1 - COURAGE: " + averageCourage + " at the end of experiment " + CurrentExperimentNumber);
            Debug.Log("AVERAGE VIRTUE 2 - GENEROSITY: " + averageGenerosity + " at the end of experiment " + CurrentExperimentNumber);
            Debug.Log("AVERAGE VIRTUE 3 - HONESTY: " +  averageHonesty + " at the end of experiment " + CurrentExperimentNumber);
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
            
            if (Agents[i].location == 0) //If they are home
            {
                facePos = new Vector3(randVal1, randVal2, 0);

            }
            else if (Agents[i].location == 1) //At location 1 (TopRight)
            {
                facePos = new Vector3(1.2f + randVal1, 1.2f + randVal2, 0);
                if (Agents[i].fallenInRiver == 1) //Fallen over
                {
                    facePos = new Vector3(1.0f + randVal1, 0.5f + randVal2, 0);
                }
            }
            else if (Agents[i].location == 2) //At location 2 (BottomRight)
            {
                facePos = new Vector3(1.7f + randVal1, -1.0f + randVal2, 0);
                if (Agents[i].fallenInRiver == 2) //Fallen over
                {
                    facePos = new Vector3(1.3f + randVal1, -0.6f + randVal2, 0);
                }
            }
            else if (Agents[i].location == 3) //At location 3 (BottomLeft)
            {
                facePos = new Vector3(-1.7f + randVal1, -1.1f + randVal2, 0);
                if (Agents[i].fallenInRiver == 3) //Fallen over
                {
                    facePos = new Vector3(-0.7f + randVal1, -0.8f + randVal2, 0);
                }
            }
            else if (Agents[i].location == 4) //At location 4 (TopLeft)
            {
                facePos = new Vector3(-1.3f + randVal1, 1.2f + randVal2, 0);
                if (Agents[i].fallenInRiver == 4) //Fallen over
                {
                    facePos = new Vector3(-1.2f + randVal1, 0.5f + randVal2, 0);
                }
            }
            //TODO: MAKE FALLENINRIVER BOOL AGAIN AND UPDATE LOGIC, DOES NOT NEED TO BE INT SINCE FALLENLOCATION AND LOCATION ARE ALWAYS THE SAME 
            //if (Agents[i][9] == 0)
            //{Spawn death-face}

            //GameObject go = Instantiate(FacePrefab, facePos, Quaternion.identity);
            Instantiate(FacePrefab,facePos, Quaternion.identity);

        }

    }

    //Roll dice - if true - fallen over
    private bool  FallingDice()
    {
        return (UnityEngine.Random.value < fallingChance);
    }

    private int RandomAliveOther(Agent i)
    {

        List<int> ToAskList = new List<int>(); //Create list
        //Conditions:
        //1. They are not dead
        //2. They are not the agent

        for (int j = 0; j < Agents.Length; j++)
        {
            if (Agents[j].alive && Agents[j] != i)
            {
                ToAskList.Add(j);
            }
        }

        //Select and return random from list
        int ranNum = UnityEngine.Random.Range(0, ToAskList.Count);
        return ToAskList[ranNum];
    }

    private List<Agent> FindSaviors(Agent agentToBeSaved)
    {
      
        
        
        List<Agent> Saviors = new List<Agent>(); //Create list

        //Conditions:
        //1. Is on the same location (i)
        //2. Has not fallen themselves Agents[][8] = 0;
        //3. Is not dead Agents[][9] = 1;

        for (int j = 0; j < Agents.Length; j++)
        {
            
            if (Agents[j].isSaviour(agentToBeSaved)) //Three conditions
            {
                Saviors.Add(Agents[j]);
                //Debug.Log(j + " is an alive agent at same location that hasn't fallen in");
            }
        }

        string relevantSaviors = string.Join(", ", Saviors.Select(p => p.ToString()).ToArray());
        //Debug.Log("POSSIBLE saviors for = " + relevantSaviors); //Print list of possible NBs

        //Shuffle the list
        List<Agent> shuffledNumbers = new List<Agent>(); //List with shuffled order
        
        for (int k = 0; k < Saviors.Count; k++)
        {
            Agent ranSaviour = Saviors[UnityEngine.Random.Range(0, Saviors.Count)];
            //int ranNum = Saviors[UnityEngine.Random.Range(0, Saviors.Count)]; //generates random number
            shuffledNumbers.Add(ranSaviour); //adds the random number to shuffledNumbers
            Saviors.Remove(ranSaviour); //removes the used ones
        }

        string shuff = string.Join(", ", shuffledNumbers.Select(p => p.ToString()).ToArray());
        
        return shuffledNumbers;
    }

   
    class Agent
    {
        public float energy; //0
        public float courage; //1
        public float generosity; //2
        public float honesty; //3
        public int eType; //4
        public float eValue; //5
        public int location; //6
        public int foodLocationMemory; //7
        public int fallenInRiver; //8, 0 if not in river, otherwise it shows location where they are fallen in river
        public bool alive; //9
        public int age; //10
        public int name { get; set; }


        public int maxEnergy;
        //public int name;
        public float learningRate;
        //mutation
        public Agent(float startingEnergy, int startingEType, int maxEnergy, int name, float learnRate)
        {
            this.energy = startingEnergy;
            this.courage = UnityEngine.Random.Range(-1.0f, 1.0f);
            this.generosity = UnityEngine.Random.Range(-1.0f, 1.0f);
            this.honesty = UnityEngine.Random.Range(-1.0f, 1.0f);
            this.eType = startingEType;
            this.eValue = 0;
            this.location = 0;
            this.foodLocationMemory = 0;
            this.fallenInRiver = 0;
            this.alive = true;
            this.age = 0;
            this.maxEnergy = maxEnergy;
            this.name = name;
            this.learningRate = learnRate;
        }
        public Agent(float startingEnergy, int maxEnergy, int name, float learnRate)
        {
            this.energy = startingEnergy;
            this.courage = UnityEngine.Random.Range(-1.0f, 1.0f);
            this.generosity = UnityEngine.Random.Range(-1.0f, 1.0f);
            this.honesty = UnityEngine.Random.Range(-1.0f, 1.0f);
            this.eType = UnityEngine.Random.Range(0, 3);
            this.eValue = 0;
            this.location = 0;
            this.foodLocationMemory = 0;
            this.fallenInRiver = 0;
            this.alive = true;
            this.age = 0;
            this.maxEnergy = maxEnergy;
            this.name = name;
            this.learningRate = learnRate;
        }
        //no mutation
        public Agent(Agent parentOne, Agent parentTwo, int name)
        {
            this.energy = (parentOne.energy + parentTwo.energy) / 2;
            this.courage = (parentOne.courage + parentTwo.courage) / 2;
            this.generosity = (parentOne.generosity + parentTwo.generosity) / 2;
            this.honesty = (parentOne.honesty + parentTwo.honesty) / 2;
            this.eType = parentOne.eType; //Simply take etype of one of them
            this.eValue = (parentOne.eValue + parentTwo.eValue) / 2;
            this.location = 0;
            this.foodLocationMemory = 0;
            this.fallenInRiver = 0;
            this.alive = true;
            this.age = 0;
            this.maxEnergy = parentOne.maxEnergy;
            this.name = name;
            this.learningRate = parentOne.learningRate;
        }

        public bool contemplateSaving(float streamLevel)
        {
            if (this.courage > streamLevel)
            {
                return true;
            }
            this.evalIgnoreDrowning(streamLevel);
            return false;
        }

        public bool saveOther(float streamLevel, Agent fallenAgent)
        {
            float coinToss = UnityEngine.Random.Range(-1.0f, 1.0f);
                if (coinToss > streamLevel)//saving is successfull
                {
                    fallenAgent.fallenInRiver = 0;
                    evalSaving(streamLevel);
                    return true;
                }
                this.alive = false;
                //Debug.Log("Agent" + this.name + " died trying to rescue fallen agent");
                return false;
        }

        public void isSaved()
        {
            this.fallenInRiver = 0;
        }

        public void drowns()
        {
            Debug.Log("Agent " + this.name + "drowned");
            this.alive = false;
        }
        public void ignoreDrowning()
        {
            
        }

        public void donateFood()
        {
            
        }

        public void keepFood()
        {
            
        }
        
        public void tellTruth()
        {
            
        }
        
        public void tellLie()
        {
            
        }

        public void stealFood()
        {
            
        }

        public void noStealFood()
        {
            
        }

        public void askFoodLocation(Agent otherAgent, int foodLocation)
        
        {
            if(this.generosity > 0.5f || this.foodLocationMemory == foodLocation || !this.alive){return;}
            if (otherAgent.foodLocationMemory == foodLocation)
            {
                if (otherAgent.honesty > 0.0f)
                {
                    this.foodLocationMemory = otherAgent.foodLocationMemory;
                    otherAgent.evalTellingTruth();
                    //sentimentValue increase
                }
                else //lie if dishonest
                {
                    otherAgent.evalTellingLie();
                    //sentimentValue decrease
                }
            }
            else
            {
                if (otherAgent.honesty < 0.0f)
                {
                    otherAgent.evalTellingLie();
                    //sentimentValue decrease
                }
            }
        }

        public void moveTo(int island)
        {
            this.location = island;
        }

        public bool eat()
        {
            if (this.energy >= 1)
            {
                this.energy -= 1;
                this.age += 1;
                return true;
            }

            this.alive = false;
            return false;

        }

        public bool contemplateBegging()
        {
            if (!this.alive)
            {
                return false;
            }
            float selfishness = getSelfishness();
            float hunger = getHunger();
            float begFactor = hunger * selfishness;
            float begThreshold = 0.2f;
            return (begFactor > begThreshold) ;
        }

        public void begForFood(Agent otherAgent)
        {
            /*
            float otherSelfishness = otherAgent.getSelfishness();
            float otherHunger = otherAgent.getHunger();
            seemingly does nothing in the original code
            */
            if (otherAgent.generosity > 0.0f)
            {
                Debug.Log("Agent "+ otherAgent.name + " gave food to agent " + this.name + " with energy " + otherAgent.energy);
                this.energy += 1f; //foodvalue later
                otherAgent.energy -= 1f; //foodvalue later
                otherAgent.evalDonateFood(this);
                    //sentiment value update here.
            }
            else
            {
                otherAgent.evalIgnoreBeggar(this);
            }
            
        }

        public void MoveBack(int foodLocation, float foodValue)
        {
            //Debug.Log("Agent is at location " + this.location + "and food is at location " + foodLocation);
            if (this.location == foodLocation) //idk kev
            {
                //Debug.Log("Agent"+ this.name + " found food and gained " + foodValue + "energy");
                this.energy += foodValue; //foodValue here
                this.foodLocationMemory = this.location;
            }
            //Debug.Log("Agent"+ this.name + " failed to locate food");
            this.location = 0;
            this.fallenInRiver = 0;

        }

        public bool isStarving()
        {
            if (this.energy < 1 && this.alive)
            {
                //maybe invoke stealing and begging here, alternatively 
                Debug.Log("Agent died of starvation");
                this.die();
                // if tryBeg()
                // else considerSteal()
                //      steal()
                // die() if begging and stealing fails
                return true;
            }
            return false;
        }

        public void stealFrom(Agent target)
        {
            this.energy += 1;
            target.energy -= 1;
        }
        
        

        public void die()
        {
            this.alive = false;
            Debug.Log("Agent" + this.name + "died");
        }

        private float getSelfishness()
        {
            return 1 - ((this.generosity + 1) / 2); //0 er minst selfish 1 er mest selfish
        }

        private float getDishonesty()
        {
            return 1- ((this.honesty + 1) / 2); //0 er minst dishonest 1 er mest dishonest
        }

        private float getHunger()
        {
            return 1 - (this.energy / (float)this.maxEnergy); //0 er ingen hunger 1 er max
        }
        
        public bool contemplateStealing(Agent otherAgent)
        {
            if (!this.alive)
            {
                return false;
            }

            if (otherAgent.energy < 1)//can't steal nonexistent food
            {
                return false;
            }
            float selfishness = getSelfishness();
            float hunger = getHunger();
            float otherHunger = otherAgent.getHunger();
            float dishonesty = getDishonesty();
            float stealFactor = 0;
            return  (hunger*(selfishness+dishonesty) > otherHunger);
        }

        public bool isSaviour(Agent otherAgent)
        {
            return (this.location == otherAgent.location && this.fallenInRiver == 0  && this.alive);
        }

        //save, donate food, truth vs lie and steal go here. cvPaste and make OOP 
        
        //learning
        private void evalSaving(float riverSpeed)
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue += eRate; //"Approval"
                    this.courage += learningRate;
                    Debug.Log("Agent " + this.name + " got an approval for SAVING FALLEN, eudaemonia INCREASED and courage increased by " + learningRate );
                    break;
                case 1:
                    this.eValue -= eRate; //"disapproval"
                    this.courage -= learningRate;
                    Debug.Log("Agent " + this.name + " did something bad for self-survival, e-value DECREASED");
                    break;
                case 2:
                    Debug.Log(riverSpeed);
                    if(riverSpeed < 0.0f) //If the stream was less than half of maximum - good job
                    {
                        this.eValue += eRate; //"GOOD"
                        //this.courage += learningRate;
                        Debug.Log("Agent "  + this.name +" saved fallen when the risk was LOW, eudaemonia INCREASED");
                    }
                    else //If the stream was more than half of maximum - bad job
                    {
                        this.eValue -= eRate; //"BAD"
                        this.courage -= learningRate;
                        Debug.Log("Agent "  + this.name +" saved fallen when the risk was TOO HIGH, eudaemonia DECREASED");
                    }
                    break;
            }
            if (this.courage > 1.0f)
            {
                this.courage = 1.0f;
            }
            if (this.courage< -1.0f)
            {
                this.courage = -1.0f;
            }
        }

        private void evalIgnoreDrowning(float riverSpeed)
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue -= eRate; //"dispproval"
                    this.courage += learningRate;
                    Debug.Log("Agent " +this.name + " got a disapproval for IGNORING FALLEN, eudaemonia DECREASED and courage increased by " + learningRate);
                    break;
                case 1:
                    this.eValue += eRate; //"approval"
                    this.courage -= learningRate;
                    Debug.Log("Agent " +this.name + " did something good for self-survival, e-value INCREASED");
                    break;
                case 2:
                    Debug.Log(riverSpeed);
                    if(riverSpeed < 0.0f) //If the stream was less than half of maximum - bad job
                    {
                        this.eValue -= eRate; //"BAD"
                        this.courage += learningRate;
                        Debug.Log("Agent "  +this.name +" ignored fallen when the risk was LOW, eudaemonia DECREASED");
                    }
                    else //If the stream was more than half of maximum - good job
                    {
                        this.eValue += eRate; //"GOOD"
                        //this.courage += learningRate;
                        Debug.Log("Agent "  +this.name + " saved fallen when the risk was TOO HIGH, eudaemonia INCREASED");
                    }
                    break;
            }
            if (this.courage > 1.0f)
            {
                this.courage = 1.0f;
            }
            if (this.courage < -1.0f)
            {
                this.courage= -1.0f;
            }
        }

        public void evalNotStealing()
        {
            
        }

        public void evalStealing(Agent otherAgent)
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue -= eRate;
                    this.generosity += learningRate;
                    this.honesty += learningRate;
                    break;
                case 1:
                    if (this.energy >= (float)maxEnergy) //stealing is good for own survival unless you are at max food
                    {
                        this.eValue -= eRate;
                        this.generosity += learningRate;
                        this.honesty += learningRate;
                        Debug.Log("Agent"+ this.name+" stole despite having max food thus wasting potential future food, eudaemonia decreased");
                        break;
                    }
                    this.eValue += eRate;
                    this.generosity -= learningRate;
                    this.honesty -= learningRate;
                    Debug.Log("Agent " + this.name + " stole from " + otherAgent.name + " which increased his odds of survival, eudaemonia increased");
                    break;
                case 2:
                    if (otherAgent.getHunger() > this.getHunger()+0.3) //stealing can be permissible if the target is wealthy enough
                    {
                        this.eValue += eRate;
                        this.generosity -= learningRate;
                        this.honesty -= learningRate;
                        Debug.Log("Agent " + this.name + "stole from a significantly wealthier target which is permissible");
                        break;
                    }
                    this.eValue -= eRate;
                    this.generosity += learningRate;
                    this.honesty += learningRate;
                    Debug.Log("Agent " + this.name + "stole which is bad");
                    break;
            }
            if (this.honesty > 1.0f)
            {
                this.honesty = 1.0f;
            }
            if (this.honesty < -1.0f)
            {
                this.honesty = -1.0f;
            }

            if (this.generosity > 1.0f)
            {
                this.generosity = 1.0f;
            }
            if (this.generosity < -1.0f)
            {
                this.generosity = -1.0f;
            }
        }

        private void evalDonateFood(Agent beggar)
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue += eRate;
                    this.generosity += learningRate;
                    Debug.Log("Agent "  + this.name + " got an approval for GIVING FOOD, eudaemonia INCREASED");
                    Debug.Log("Positively reinforced generosity because it led to gain of eudaemonia");
                    break;
                case 1:
                    this.eValue -= eRate;
                    this.generosity -= learningRate;
                    Debug.Log("Agent "  + this.name +" did something bad for self approval, eudaemonia DECREASED");
                    Debug.Log("negatively reinforced generosity because it led to gain of euaemonia");
                    break;
                case 2:
                    if (beggar.energy >= this.energy)
                    {
                        this.eValue -= eRate;
                        this.generosity -= learningRate;
                        Debug.Log("Agent " + this.name +" fed a begging agent desipite having less food than beggar, eudaemonia DECREASED");
                        Debug.Log("negatively reinforced positive generosity because it led to loss of euaemonia");
                    }
                    else
                    {
                        this.eValue += eRate;
                        Debug.Log("Agent "+ this.name +" fed a begging agent who had less than her, eudaemonia INCREASED");
                    }
                    break;
            }
            if (this.generosity > 1.0f)
            {
                this.generosity = 1.0f;
            }
            if (this.generosity < -1.0f)
            {
                this.generosity = -1.0f;
            }
        }

        private void evalIgnoreBeggar(Agent beggar)
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue -= eRate; //less happiness from greed so be more charitable
                    this.generosity += learningRate;
                    Debug.Log("Agent "  + " got disapproval for DENYING FOOD, eudaemonia DECREASED");
                    break;
                case 1:
                    this.eValue += eRate; //more happy from greed therefore less charitable
                    this.generosity -= learningRate;
                    Debug.Log("Agent "  + " did something good for self approval, eudaemonia INCREASED");
                    break;
                case 2:
                    if (beggar.energy >= this.energy)
                    {
                        this.eValue += eRate;
                        Debug.Log("Agent " + " ignored a begging agent due to beggar having more food, eudaemonia INCREASED");
                    }
                    else
                    {
                        this.eValue -= eRate; //this eType only gets punished for being too nice to agents who don't need it
                        this.generosity += learningRate;
                        Debug.Log("Agent "+ " ignored a begging agent who had less than her, eudaemonia DECREASED");
                    }
                    break;
            }
            if (this.generosity > 1.0f)
            {
                this.generosity = 1.0f;
            }
            if (this.generosity < -1.0f)
            {
                this.generosity = -1.0f;
            }
        }

        
        private void evalTellingTruth( )
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue += eRate;
                    //this.honesty += learningRate;
                    Debug.Log("Agent " + " ACTED TRUTHFULLY, eudaemonia INCREASED");
                    break;
                case 1:
                    Debug.Log("Telling the truth does not impact the survival of the agent");
                    break;
                case 2:
                    this.eValue += eRate;
                    Debug.Log("Agent " + this.name +" got an approval for TELLING TRUTH, eudaemonia INCREASED");
                    
                    break;
            }
            if (this.honesty > 1.0f)
            {
                this.honesty = 1.0f;
            }
            if (this.honesty < -1.0f)
            {
                this.honesty = -1.0f;
            }
        }

        private void evalTellingLie( )
        {
            if (!LearningEnabled) { return; }
            switch (this.eType)
            {
                case 0:
                    this.eValue -= eRate;
                    this.honesty += learningRate;
                    Debug.Log("Agent " +this.name + " got a disapproval for TELLING LIE, eudaemonia DECREASED");
                    Debug.Log("Positively reinforced honesty by "+ learningRate + "because dishonesty led to loss of eudaemonia");
                    break;
                case 1:
                    Debug.Log("Telling the truth does not impact the survival of the agent");
                    break;
                case 2:
                    this.eValue -= eRate;
                    this.honesty += learningRate;
                    Debug.Log("Agent " +this.name + " TELLING LIES is bad for others, eudaemonia DECREASED");
                    break;
            }

            if (this.honesty > 1.0f)
            {
                this.honesty = 1.0f;
            }
            if (this.honesty < -1.0f)
            {
                this.honesty = -1.0f;
            }
        }

        public void moralExemplar(Agent otherAgent)
        {
            if (otherAgent.eValue > this.eValue && otherAgent.eType == this.eType )
            {
                this.generosity = otherAgent.generosity;
                this.courage = otherAgent.courage;
                this.honesty = otherAgent.honesty;
                Debug.Log("Agent "  + this.name +" copied the virtues of agent " +  otherAgent.name + " ------------ MORAL EXEMPLAR!!!");    
            }
            
        }

    }
}

