using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Cannon : MonoBehaviour
{
    #region Variables
    public enum GameStates // Create Our Own Datatypes Whic Can Store The States We Want As Elements
    {
        AIM,
        SHOTSFIRED,
        WAIT,
        ENDROUND,
        NEXTLEVEL,
        RESTARTLEVEL
    }
    public GameStates currentGameStates; //Make A Variable Out Of Our Own Datatype For Current Game State
    Vector2 mousePositionInWorld; // To Store The Position Of Mouse Coordinates
    Vector2 startDragPosition; //Stores The Point We Start Dragging
    Vector2 endDragPosition; //Stores The Point We End Dragging 
    AimAssist aimAssistScriptAccessVariable;//Variable To Access Aim Assist Script
    public GameObject ballPrefab; //Stores Which Game Object TO Instantiate/Spawn
    public int numberOfBallsSpawned = 10;
    public float minShootValue = 0.3f;

    [SerializeField]
    int ballsInAirCheck; //  Variable To Store Number Of Balls In The Air
    public List<GameObject> spawnedBallsList = new List <GameObject>(); // Create A List That Can Store Game Objects
    GameObject[] activeBricksArray; //Make An Array To Store And Record Bricks
    public List <GameObject> activeBricksList = new List <GameObject>(); //Make A List To Store The Bricks
    public bool levelOver;
    float[] possibleXPositions = { -2.3f, 2.3f, 0f, 1.15f, -1.15f }; //Make A Float Array Of All Possible X Positions
    bool cannonHasMoved;
    public GameObject recallButtonCanvas;
    [Header("Round Stuff")]
    public int maxNumberOfRounds; //Edit This In Inspector To Change The Max Number Of Rounds
    int currentRound; // Keep Track Of The Round Being Played
    bool shouldIIncrementRound; // To Increment Round Once At A Time
    public GameObject victoryPanel;
    public GameObject losingPanel;
    public GameObject overallPanel;
    Scene currentScene ;
    int currentSceneBuildIndex; 
    #endregion Variables

    void createBalls() {    // Function To Create Balls On Awake
        for (int i = 0; i < numberOfBallsSpawned; i++)//Run A Loop Till The Required Number Of Balls Have Spawned
        {
            GameObject spawnedBall = Instantiate(ballPrefab, transform.position, Quaternion.identity);//Spawn One Clone And Store Its Variable
            spawnedBallsList.Add(spawnedBall);
            spawnedBall.SetActive(false);//Make The Spawned Object Invisible
        }
    }
    #region Awake And Start
    void Awake()
    {
        aimAssistScriptAccessVariable = GetComponent<AimAssist>();//Access Granted To Aim Assist Script Component
        createBalls();
        activeBricksArray = GameObject.FindGameObjectsWithTag("Brick"); //Find All The Objects With The Brick Tag And Store It In The Array
        foreach(GameObject activeBricks in activeBricksArray)
        {
            activeBricksList.Add(activeBricks);//Add Each Brick To The new List

        }
    }
    void Start()
    {
        currentGameStates = GameStates.AIM; //Set The Current Game State To AIM
        ballsInAirCheck = 0; //Set Balls In Air Check To 0
        cannonHasMoved = true;
        currentRound = 1;
        hideCanvas();
        hideScreen(overallPanel);
        hideScreen(victoryPanel);
        hideScreen(losingPanel);
        currentScene = SceneManager.GetActiveScene();
        currentSceneBuildIndex = currentScene.buildIndex;
    }
    #endregion Awake And Start

    void Update()
    {
        mousePositionInWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);// Convert Mouse Coordinates From Pixels To Unity World Units
                                                                                   //mousePositionInWorld.z =0;// Set z Value 0 To Get A 2D Vector
        #region State Machine
        switch (currentGameStates)
        {
            case GameStates.AIM:
                if (Input.GetMouseButtonDown(0))//If I Touch Screen
                {
                    StartDrag(mousePositionInWorld);//Call Start Drag Function With Mouse Point As Parameter
                }
                else if (Input.GetMouseButton(0))// If I Keep Holding Touch
                {
                    ContinueDrag(mousePositionInWorld);//Call Continue Drag Function with Mouse Position In Units As Parameter
                }
                else if (Input.GetMouseButtonUp(0))//If I Release Touch
                {
                    EndDrag();//Call End Drag Function
                    if (currentGameStates != GameStates.SHOTSFIRED) //If Current Game State Is Not Shotsfired
                    {
                        currentGameStates = GameStates.SHOTSFIRED;// Then Set It To ShotsFired
                    }
                }
                    break;
            case GameStates.SHOTSFIRED:

                    break;
            case GameStates.WAIT:
                if (cannonHasMoved)
                {
                    showCanvas();
                    cannonHasMoved = false;
                }
                    break;
            case GameStates.ENDROUND:
                levelOver = checkBricks();
                hideCanvas();
                if (levelOver)
                {
                    if (currentGameStates != GameStates.NEXTLEVEL)
                    {
                        currentGameStates = GameStates.NEXTLEVEL;//Change State To Next Level
                    }
                }
                else if (!levelOver)
                {
                    //Start A New Round
                    
                    if (!cannonHasMoved)
                    {
                        cannonHasMoved = true;
                        currentRound = currentRound + 1;
                        if (currentRound <= maxNumberOfRounds)
                        {
                            StartCoroutine(roundEnd());
                        }
                        else if (currentRound > maxNumberOfRounds)
                        {
                            if (currentGameStates != GameStates.RESTARTLEVEL)// If The Game Is Not In Restart Level State
                            {
                                currentGameStates = GameStates.RESTARTLEVEL;//Restart The Level
                            }
                        }

                        
                    }
                }
                    break;
            case GameStates.NEXTLEVEL:
                StartCoroutine(loadNextLevel());
                break;
            case GameStates.RESTARTLEVEL:
                StartCoroutine(reloadCurrentLevel());
                break;
        }
        #endregion State Machine

    }
    public void countReturningBalls()
    {
        ballsInAirCheck ++; //Adds One To The Number Of Balls In The Air
        //Debug.Log("hello");
        if (ballsInAirCheck == numberOfBallsSpawned)//If Balls In Air Check Is Equal To The Number Of Balls Spawned
        {
            if (currentGameStates != GameStates.ENDROUND)//If Game State Is Not Already End Round
            {
                currentGameStates = GameStates.ENDROUND;//Set Current Game State To EndRound
            }
            ballsInAirCheck = 0;//Set Balls In Air Check Back To 0 
        }
    }
    #region Inputs
    void StartDrag(Vector2 positionInWorld)
    {
        aimAssistScriptAccessVariable.EnabledLine();// Call Enabled Line Function From Aim Assist Script To Make line Visible
        startDragPosition = positionInWorld;//Assign Whatever Is Passed As A Parameter When A Function Is Called To StartingPosition
        aimAssistScriptAccessVariable.SetStartPointOfLine(transform.position);// Call Set Start Point Of Line Function from Aim Assist Script To Set The Start Point To The Cannon's Position
    }
    void ContinueDrag(Vector2 positionInWorld)
    {
        endDragPosition = positionInWorld;//Assign Whatever Is Passed As A Parameter When A Function Is Called To EndingPosition
        Vector2 directionDrag = endDragPosition - startDragPosition; //Get The Displacement Vector From Start Of The Drag To End
        Vector2 cannonPosition = transform.position;
        if (directionDrag.y >= minShootValue) //If Direction Of Deag Y's Coordinate Exceeds A Certain Amount In The Bottom Direction
        {
            directionDrag.y = -minShootValue; //Keep It Fixed To The Minimum Amount
        }
        aimAssistScriptAccessVariable.SetEndPointOfLine(cannonPosition - directionDrag);// Call End Point Of line Function From The Aim Assist Script
    }
    //Function To Call When We Lift Our Finger
    void EndDrag()
    {
        aimAssistScriptAccessVariable.DisabledLine();// Call Disabled Line Function From Aim Assist Script To Make line Invisible
        StartCoroutine(ShootBalls());
    }
    #endregion Inputs

    IEnumerator ShootBalls()
    {
        Vector2 directionOfBallSpawn = endDragPosition - startDragPosition;//Get The Displacement Vector Of The Finger Drag
        if (directionOfBallSpawn.y >= -minShootValue) //If Direction Of Drag Y's Coordinate Exceeds A Certain Amount In The Bottom Direction
        {
            directionOfBallSpawn.y = -minShootValue; //Keep It Fixed To The Minimum Amount
        }
        directionOfBallSpawn.Normalize();//Get The Direction Of The Finger Drag Without Magnitude
        foreach (GameObject ball in spawnedBallsList)//Run A Loop For All GameObjects spawnedBallList
        {
            ball.transform.position = transform.position;//Set The Spawned Balls To The Cannon's Position
            ball.SetActive(true);// Makes The Ball Visible
            ball.GetComponent<Rigidbody2D>().AddForce(-directionOfBallSpawn);//Add A Force To The Ball
            yield return new WaitForSeconds(0.2f);// Wait For X Seconds Before Spawning The NExt Ball
        }
        if (currentGameStates != GameStates.WAIT) //If Current Game State Is Not Shotsfired
        {
            currentGameStates = GameStates.WAIT;// Then Set It To ShotsFired
        }
    }
    #region Counting Bricks
    public bool checkBricks()
    {
        bool allBricksDead=false;//Make A Boolean To Keep Track Of Whether All Bricks Are Dead Or Not
        if (activeBricksList.Count <= 0)
        {
            allBricksDead = true;
            
        }
        else if (activeBricksList.Count > 0)//If There Is Something In The List
        {
            allBricksDead = false; //All Bricks Are Not Dead
        }
        return allBricksDead;
    }
    public void removeBricksFromList(GameObject brick)
    {
        if (activeBricksList != null) //If There Is Something In List 
        {
            for (int i=0; i < activeBricksList.Count; i++)//Run The Loop For The Length Of The List
            {
                if (activeBricksList[i].gameObject.name == brick.gameObject.name)//If The Brick's Name At The Current Iteration Of The Loop Is The Same As The Brick To Be Removed
                {
                    activeBricksList.RemoveAt(i);//Remove It From The List
                }
            }
        }
    }
    #endregion Counting Bricks
    #region Changing Cannon Position
    void moveCannon()
    {
        float newCannonXPosition = randomize();//Call Randomize Function And Store The Return Value
        transform.position = new Vector3(newCannonXPosition, transform.position.y, transform.position.z); //Set New Position For The Cannon
    }
    float randomize()
    {
        int pickedIndex = Random.Range(0, possibleXPositions.Length);//Pick a Random Position From 0 To Length Of The array
        float pickedPosition = possibleXPositions[pickedIndex];// Get The Number At The Picked Position
        while (transform.position.x == pickedPosition)
        {
            pickedIndex = Random.Range(0, possibleXPositions.Length);//Pick A Number Again
            pickedPosition = possibleXPositions[pickedIndex];// Assign A Position Again
        }
        return pickedPosition;// Return The Value Of Picked Position
    }
    #endregion Changing Cannon Position
    public void recallBalls()
    {
        foreach (GameObject ball in spawnedBallsList)
        {
            ball.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
            ball.transform.position = transform.position;
            ball.SetActive(false);
            if (currentGameStates != GameStates.ENDROUND)
            {
                currentGameStates = GameStates.ENDROUND;
            }
        }
    }
    IEnumerator roundEnd()
    {
        moveCannon();
        yield return new WaitForSeconds(0.5f);//Wait For 0.5 Seconds 
        if (currentGameStates != GameStates.AIM)//Check If The Game State Is AIm
        {
            currentGameStates = GameStates.AIM;//Change State To AIM
        }
    }
    void showCanvas()
    {
        recallButtonCanvas.SetActive(true);
    }
    void hideCanvas()
    {
        recallButtonCanvas.SetActive(false);
    }
    IEnumerator loadNextLevel()
    {
        
        int totalNumberOfScenes = SceneManager.sceneCountInBuildSettings;
        if (currentSceneBuildIndex == totalNumberOfScenes - 1)
        {
            showScreen(overallPanel);
            hideScreen(victoryPanel);
            hideScreen(losingPanel);
            yield return new WaitForSeconds(60);
            SceneManager.LoadScene(0);
        }
        else
        {
            showScreen(victoryPanel);
            hideScreen(overallPanel);
            hideScreen(losingPanel);
            yield return new WaitForSeconds(60f);
            SceneManager.LoadScene(currentSceneBuildIndex+1);
        }
    }
    IEnumerator reloadCurrentLevel()
    {
        showScreen(losingPanel);
        hideScreen(victoryPanel);
        hideScreen(overallPanel);
        Scene currentScene = SceneManager.GetActiveScene();
        int currentSceneBuildIndex = currentScene.buildIndex;
        yield return new WaitForSeconds(60f);
        SceneManager.LoadScene(currentSceneBuildIndex);
    }
    void showScreen (GameObject screenName) {
        screenName.SetActive(true);
    }
    void hideScreen(GameObject screenName) {
        screenName.SetActive(false);
    }
    public void loadNextLevelVictory()
    {
        SceneManager.LoadScene(currentSceneBuildIndex + 1);
    }
    public void restartLevel()
    {
        SceneManager.LoadScene(currentSceneBuildIndex);
    }
    public void overallVictory()
    {
        SceneManager.LoadScene(0);
    }
}
