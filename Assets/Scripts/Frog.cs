using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SteeringCalcs;
using Globals;
using System;
using TMPro;
using UnityEngine.SceneManagement;


public class Frog : MonoBehaviour
{
    // Frog status.
    public int Health = 3;
    public int flies = 0;

    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // The arrival radius is set up to be dynamic, depending on how far away
    // the player right-clicks from the frog. See the logic in Update().
    public float ArrivePct;
    public float MinArriveRadius;
    public float MaxArriveRadius;
    private float _arriveRadius;

    // Turn this off to make it easier to see overshooting when seek is used
    // instead of arrive.
    public bool HideFlagOnceReached;

    // References to various objects in the scene that we want to be able to modify.
    private Transform _flag;
    private SpriteRenderer _flagSr;
    private DrawGUI _drawGUIScript;
    private Animator _animator;
    private Rigidbody2D _rb;

    // Stores the last position that the player right-clicked. Initially null.
    private Vector2? _lastClickPos;

    //WEEK6
    //Used by DTs to make decision
    public float scaredRange;
    public float huntRange;
    private Fly closestFly;
    private Snake closestSnake;
    private float distanceToClosestFly;
    private float distanceToClosestSnake;
    public float anchorWeight;
    public Vector2 AnchorDims;

    // For pathfinding
    private Node[] path;
    private int pathIndex = 0; 
    public LayerMask slowMask;
    public float overlapCircleRadius;
    public float slowDownMultiplier;


    // For decision tree
    public bool isAIControlled = true;
    private Vector2 lifePos;
    public float FleeDistance = 7f;

    private float damageCooldown = 1f;  
    private float lastDamageTime = -10f;

    private GameObject gameOverPanel;
    private TextMeshProUGUI gameOverText;
    private TextMeshProUGUI restartHintText; 
    private bool gameEnded = false;



    void Start()
    {   
        gameOverPanel = GameObject.Find("GameOverPanel");

        if (gameOverPanel != null)
        {
            TextMeshProUGUI[] texts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var text in texts)
            {
                if (text.name == "GameOverText")
                    gameOverText = text;
                else if (text.name == "RestartHintText")
                    restartHintText = text;
            }
            gameOverPanel.SetActive(false); 
        }


        // Initialise the various object references.
        _flag = GameObject.Find("Flag").transform;
        _flagSr = _flag.GetComponent<SpriteRenderer>();
        _flagSr.enabled = false;

        GameObject uiManager = GameObject.Find("UIManager");
        if (uiManager != null)
        {
            _drawGUIScript = uiManager.GetComponent<DrawGUI>();
        }

        _animator = GetComponent<Animator>();

        _rb = GetComponent<Rigidbody2D>();

        _lastClickPos = null;
        _arriveRadius = MinArriveRadius;

        path = new Node[0];

    }

    void Update()
    {

        // Check if the player right-clicked (mouse button #1).
        if (!isAIControlled && Input.GetMouseButtonDown(1))
        {
            _lastClickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Set the arrival radius dynamically.
            _arriveRadius = Mathf.Clamp(ArrivePct * ((Vector2)_lastClickPos - (Vector2)transform.position).magnitude, MinArriveRadius, MaxArriveRadius);

            _flag.position = (Vector2)_lastClickPos + new Vector2(0.55f, 0.55f);
            _flagSr.enabled = true;
            Pathfinding.UpdateObstaclesExternally();
            path = Pathfinding.RequestPath(transform.position, (Vector2)_lastClickPos);
            pathIndex = 0;
            Debug.Log("Path length: " + path.Length);

            // for (int i=0; i<path.Length; i++) {
            //     Debug.Log(path[i].worldPosition);
            // }

            // Change the world position of the final path node to the actual clicked position,
            // since the centre of the final node might be off somewhat.
            if (path.Length > 0)
            {
                Node fixedFinalNode = path[path.Length - 1].Clone();
                fixedFinalNode.worldPosition = (Vector2)_lastClickPos;
                path[path.Length - 1] = fixedFinalNode;
            }
        }
        else // show the relevant info about fly and snake
        {
            if (closestFly != null)
                Debug.DrawLine(transform.position, closestFly.transform.position, Color.black);
            if (closestSnake != null)
                Debug.DrawLine(transform.position, closestSnake.transform.position, Color.red);
        }

        // check if the frog is out of screen
        if (isOutOfScreen(transform))
        {
            Debug.Log("Frog is out of screen!");
        }
        if (!gameEnded)
        {
            if (Health <= 0)
            {
                EndGame("GAME OVER");
            }
            else if (flies >= 9)
            {
                EndGame("YOU WIN!!!");
            }
        }

        if (gameEnded && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f; 
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); 
        }


    }
    void FixedUpdate()
    {
        // If the frog is controlled by AI, run the decision tree to decide movement.
        if (isAIControlled)
        {
            findClosestFly();
            findClosestSnake();
            decisionTree();
        }

        //Debug line for A* Path
        for (int i = 1; i < path.Length; i++)
        {
            Debug.DrawLine(path[i - 1].worldPosition, path[i].worldPosition, Color.black);
        }

        Vector2 desiredVel = decideMovement();

        if (path.Length > 0 && pathIndex < path.Length)
        {
            Node currentTarget = path[pathIndex];
            if (currentTarget.walkable == Node.Walkable.Obstacle)
            {
                Debug.Log("Dynamic obstacle entered path. Recalculating...");
                Pathfinding.UpdateObstaclesExternally();  
                path = Pathfinding.RequestPath(transform.position, (Vector2)currentTarget.worldPosition);
                pathIndex = 0;
                return;  
            }
        }               

        // If the Frog enters a slow-terrain, apply multiplier to desiredVel
        if (Physics2D.OverlapCircle(transform.position, overlapCircleRadius, slowMask) != null)
        {
            desiredVel /= slowDownMultiplier;
        }

        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            _animator.SetBool("Walking", true);
            transform.up = _rb.linearVelocity;
        }
        else
        {
            _animator.SetBool("Walking", false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (Time.time - lastDamageTime > damageCooldown)
            {
                TakeDamage();
                lastDamageTime = Time.time;
            }
        }
    }

    private void EndGame(string message)
    {
        gameEnded = true;
        if (gameOverPanel != null && gameOverText != null && restartHintText != null)
        {
            gameOverText.text = message;
            restartHintText.text = "Press R to Restart";
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }



    public void TakeDamage()
    {
        if (Health > 0)
        {
            Health--;
        }
    }

    //TODO Implement the following Decision Tree
    // no health <= 0 --> set speed to 0 and color to red (1, 0.2, 0.2)
    // user clicked --> go to that click
    // nearby/outside of screen --> go towards screen (similar to flies)
    // closest snake nearby --> flee from snake within the screen
    // closest fly within screen --> go towards that fly
    // otherwise --> go to center of the screen

    //TODO SUGGESTED IMPROVEMENTS:
    //go to the center of mass of flies within screen
    //if 2 snake nearby -> freeze
    //Handle shooting bubbles
    //Come up with a better DT, for example: find flies that are within a circle around the frog that doesnt include any snake
    //Extra0 shoot bubble?
    //Extra1 update your code so that: 
    //Extra2 update your code with a better DT (find flies that are within a circle around the frog that doesnt include any snake)
    //Gameplay: tweak speed, range, acceleration and anchoring
    private Vector2 decideMovement()
    {   
        // If user clicks, a path exists and the frog is currently on the path
        if (_lastClickPos != null && path.Length > 0 && pathIndex < path.Length)
        {
            return getVelocityTowardsFlag();
        }

        else
        {
            return Vector2.zero;
        }
    }

    private void decisionTree()
    {
        // Health = 0 => frog is dead
        if (Health <= 0) {
            _animator.SetBool("Walking", false);
            MaxSpeed = 0;
            GetComponent<SpriteRenderer>().color = new Color(1f, 0.2f, 0.2f);
            return;
        }
        // If 2 snakes are nearby, freeze the frog forever
        else if (countNearbySnakes(scaredRange) >= 2) {
            _animator.SetBool("Walking", false);
            MaxSpeed = 0;
            return;
        }
        // If the frog is at low health and a life is nearby, go towards it
        else if (Health == 1 && findClosestLife()){
            setFlag(lifePos);
            return;
        }
        else {
            // ouside of screen => go towards screen
            if (isOutOfScreen(transform))
            {
                setFlag(Vector2.zero);
            }
            // closest snake nearby --> flee from snake
            else if (closestSnake != null && distanceToClosestSnake < scaredRange)
            {
                Vector2 fleeDir = (Vector2)transform.position - (Vector2)closestSnake.transform.position;
                setFlag((Vector2)transform.position + fleeDir.normalized * FleeDistance);
            }
            //  closest fly within screen --> go towards that fly
            else if (closestFly != null && !isOutOfScreen(closestFly.transform))
            {
                setFlag(closestFly.transform.position);
            }
            // go to the center of mass of flies (ignores snake proximity)
            else {
                Vector2 visibleFlyCenter = getVisibleFlyCenterOfMass();
                if (visibleFlyCenter != Vector2.zero)
                {
                    setFlag(visibleFlyCenter);
                }
                else {
                    // Go to center of safe flies (not near snakes)
                    Vector2 safeCenter = getVisibleSafeFlyCenterOfMass(2f);
                    if (safeCenter != Vector2.zero)
                    {
                        setFlag(safeCenter);
                    }
                    else {
                        // stay in the center of the screen
                        setFlag(Vector2.zero);
                    }
                }
            }
        }
    }

    private void setFlag(Vector2 target)
    {
        if (_lastClickPos == null || Vector2.Distance((Vector2)_lastClickPos, target) > 1f)
        {
            _lastClickPos = target;

            // Set the arrival radius dynamically.
            _arriveRadius = Mathf.Clamp(ArrivePct * ((Vector2)_lastClickPos - (Vector2)transform.position).magnitude, MinArriveRadius, MaxArriveRadius);

            _flag.position = (Vector2)_lastClickPos + new Vector2(0.55f, 0.55f);
            _flagSr.enabled = true;
            path = Pathfinding.RequestPath(transform.position, (Vector2)_lastClickPos);
            pathIndex = 0;

            if (path.Length > 0)
            {
                Node fixedFinalNode = path[path.Length - 1].Clone();
                fixedFinalNode.worldPosition = (Vector2)_lastClickPos;
                path[path.Length - 1] = fixedFinalNode;
            }
        }
    }

    private Vector2 getVelocityTowardsFlag()
    {
        Vector2 desiredVel;

        // Get target position of next item in path
        Vector2 targetPosition = path[pathIndex].worldPosition;

        // If the frog is AI controlled, use seek to go to the target position
        if (isAIControlled) {
            desiredVel = Steering.SeekDirect(transform.position, targetPosition, MaxSpeed); 
        }
        // Else use a combination of arrive and seek
        else {
            // If the last target in path is found, switch to arriveDirect (no overshooting)
            if(pathIndex != path.Length - 1) {
                desiredVel = Steering.SeekDirect(transform.position, targetPosition, MaxSpeed); 
            }
            else {
                desiredVel = Steering.ArriveDirect(transform.position, targetPosition, _arriveRadius, MaxSpeed); 
            }
        }

        if (path.Length > 0 && pathIndex < path.Length) 
        {   
            // Get distance of frog to next position
            float distanceToNextTarget = Vector2.Distance(transform.position, targetPosition);

            // If frog reaches next position, update path index to get next position
            if (distanceToNextTarget <= Constants.TARGET_REACHED_TOLERANCE)
            {
                pathIndex++;
                if (pathIndex >= path.Length)
                {
                    _lastClickPos = null;
                    pathIndex = 0;
                    if (HideFlagOnceReached)
                    {
                        _flagSr.enabled = false;
                    }
                }
            }
        }

        else
        {
            _lastClickPos = null;
            pathIndex = 0;
            if (HideFlagOnceReached)
            {
                _flagSr.enabled = false;
            }
        }


        return desiredVel;
    }

    private void findClosestFly()
    {
        distanceToClosestFly = Mathf.Infinity;

        foreach (Fly fly in (Fly[])GameObject.FindObjectsByType(typeof(Fly), FindObjectsSortMode.None))
        {
            float distanceToFly = (fly.transform.position - transform.position).magnitude;
            if (fly.GetComponent<Fly>().State != Fly.FlyState.Dead)
            {
                if (distanceToFly < distanceToClosestFly)
                {
                    closestFly = fly;
                    distanceToClosestFly = distanceToFly;

                }
            }

        }
    }

    //TODO See findClosestFly for inspiration
    private void findClosestSnake()
    {
        distanceToClosestSnake = Mathf.Infinity;

        foreach (Snake snake in (Snake[])GameObject.FindObjectsByType(typeof(Snake), FindObjectsSortMode.None))
        {
            float distanceToSnake = (snake.transform.position - transform.position).magnitude;
            if (snake.GetComponent<Snake>().State != Snake.SnakeState.Benign)
            {
                if (distanceToSnake < distanceToClosestSnake)
                {
                    closestSnake = snake;
                    distanceToClosestSnake = distanceToSnake;

                }
            }

        }
    }

    private bool findClosestLife()
    {
        float distanceToClosestLife = Mathf.Infinity;
        Vector2 closestLifePos = Vector2.zero; 

        foreach (GameObject life in GameObject.FindGameObjectsWithTag("Life"))
        {
            float distanceToLife = Vector2.Distance(life.transform.position, transform.position);
            if (distanceToLife < distanceToClosestLife)
            {
                distanceToClosestLife = distanceToLife;
                closestLifePos = life.transform.position;
            }
        }

        if (closestLifePos != Vector2.zero)
        {
            lifePos = closestLifePos;
            return true;
        }

        return false;
    }

    //TODO Check wether the current transform is out of screen (true) or not (false)
    private bool isOutOfScreen(Transform transform)
    {
        Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.x < 0 || screenPos.x > Screen.width || screenPos.y < 0 || screenPos.y > Screen.height)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public void CatchFly() {
		flies++;
	}

    // Check for the center of mass of all visible flies
    private Vector2 getVisibleFlyCenterOfMass() {
        Vector2 positionSum = Vector2.zero;
        int count = 0;

        foreach (Fly fly in GameObject.FindObjectsByType<Fly>(FindObjectsSortMode.None))
        {
            if (fly.State != Fly.FlyState.Dead && !isOutOfScreen(fly.transform))
            {
                positionSum += (Vector2)fly.transform.position;
                count++;
            }
        }
        return count == 0 ? Vector2.zero : positionSum / count;
    }

    // Check if the fly is far from all snakes within a certain radius
    private bool isFlyFarFromSnakes(Fly fly, float dangerRadius)
    {
        foreach (Snake snake in GameObject.FindObjectsByType<Snake>(FindObjectsSortMode.None))
        {
            if ((snake.transform.position - fly.transform.position).magnitude < dangerRadius)
            {
                return false;
            }
        }
        return true;
    }

    // Check if the fly is far from all snakes within a certain radius and for the center of mass of all visible flies
    private Vector2 getVisibleSafeFlyCenterOfMass(float dangerRadius)
    {
        Vector2 positionSum = Vector2.zero;
        int count = 0;

        foreach (Fly fly in GameObject.FindObjectsByType<Fly>(FindObjectsSortMode.None))
        {
            if (fly.State != Fly.FlyState.Dead &&
                !isOutOfScreen(fly.transform) &&
                isFlyFarFromSnakes(fly, dangerRadius))
            {
                positionSum += (Vector2)fly.transform.position;
                count++;
            }
        }

        return count == 0 ? Vector2.zero : positionSum / count;
    }

    // Count the number of snakes within a certain range of the frog
    private int countNearbySnakes(float range)
    {
        int count = 0;

        foreach (Snake snake in GameObject.FindObjectsByType<Snake>(FindObjectsSortMode.None))
        {
            float dist = (snake.transform.position - transform.position).magnitude;
            if (snake.State != Snake.SnakeState.Benign && dist < range)
            {
                count++;
            }
        }

        return count;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Life") && Health < 3)
        {
            Health++;
            Destroy(other.gameObject);
        }
    }

}
