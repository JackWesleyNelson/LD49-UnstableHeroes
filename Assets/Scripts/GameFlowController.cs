using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GameState { TITLE, CHARACTER_CREATION, BATTLE_START, PLAYER_TURN, ENEMY_TURN, BATTLE_WON, BATTLE_LOST }

public class GameFlowController : MonoBehaviour
{
    GameState gameState = GameState.TITLE;
    private bool titleWaitingOnInput = true;
    private bool messageBoxNeedsUserConfirmation = false;
    private bool currentUnitTakingAction = false;
    private bool pausedGame = false;
   
    private Unit currentUnit = null;
    private Unit currentTarget = null;
    private (Action, String) actionAndNameToStabilize = (null, null);

    private readonly List<KeyCode> enterKey = new List<KeyCode> { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space,  KeyCode.Mouse0};
    private readonly List<KeyCode> upKey = new List<KeyCode> { KeyCode.UpArrow, KeyCode.W };
    private readonly List<KeyCode> downKey = new List<KeyCode> { KeyCode.DownArrow, KeyCode.S };
    private readonly List<KeyCode> leftKey = new List<KeyCode> { KeyCode.LeftArrow, KeyCode.A };
    private readonly List<KeyCode> rightKey = new List<KeyCode> { KeyCode.RightArrow, KeyCode.D };

    private List<(Action, String)> buttonActionsAndNames = new List<(Action, string)>();

    [SerializeField]
    private GameObject titleUI = null;
    //[SerializeField]
    //private GameObject characterCreationUI = null;
    [SerializeField]
    private GameObject battleStartUI = null;
    [SerializeField]
    private GameObject playerTurnUI = null;
    [SerializeField]
    private GameObject enemyTurnUI = null;
    [SerializeField]
    private GameObject battleWonUI = null;
    [SerializeField]
    private GameObject battleLostUI = null;
    [SerializeField]
    private GameObject pauseUI = null;

    [SerializeField]
    private GameObject turnMarkerPrefab = null;
    [SerializeField]
    private GameObject turnOrderPanel = null;

    [SerializeField] 
    private GameObject messageBattlePanel = null;
    [SerializeField]
    private GameObject messageGeneralPanel = null;
    [SerializeField]
    private GameObject actionButtonsPanel = null;
    [SerializeField]
    private GameObject actionButtonPrefab = null;
    [SerializeField]
    private GameObject actionGroupLabelPrefab = null;


    private int currentBattleIndex;
    private int turn = 1;

    private List<Unit> heroesMasterList = new List<Unit> { new Unit("Warrior", 1, 3, 1, 10), new Unit("Rogue", 1, 2, 1, 5), new Unit("Cleric", 1, 1, 4, 5) };
    private List<Unit> heroesCurrentBattleList = new List<Unit>();

    private List<List<String>> storyTextbeforeEachBattleList = new List<List<String>>
    {
        //Before Battle 1
        new List<string>{
            "A Warrior, Rogue and Cleric travel the lands. In search of what, they are not sure.",
            "It's been a while since their last expedition. The can still swing their instruments of war, but beyond that they aren't sure how to work as a group anymore.",
            "They struggle to remember their true actions. But only by using them will they create a more stable party.",
            "As they walk through the forest, the stumble across an enemy."
            },
        //Before Battle 2
        new List<string>{
            "Quick work was made of the goblin, but it appears it wasn't alone."
            },
        //Before Battle 3
        new List<string>{
            "As the enemy draws it's last breath, the party feels relief.",
            "Just as they are about to rest, a snap is heard off in the distance.",
            "Everyone jumps up, as the Rogue darts off toward a distant tree.",
            "The Warrior and Cleric follow the Rogue's lead and give chase.",
            "The Rogue throws a dagger that seems to bend between the tree branches, and sinks into the goblin scout, dropping it to the ground.",
            "However, it was moments too late as the sound of a horn echoes through the wood you know another fight is yet to come.",
            "The party takes a breath, and proceeds forward towards the snipping sounds of the goblins and a lumbering sound."
            },
    };

    private List<List<Unit>> enemiesInEachBattleMasterList = new List<List<Unit>> 
    {
        //Battle 1
        new List<Unit>{ new Unit("Goblin", 3, 1, 1, 5) },
        //Battle 2
        new List<Unit>{ new Unit("Goblin", 3, 1, 1, 5), new Unit("Goblin", 3, 1, 1, 5) },
        //Battle 3
        new List<Unit>{ new Unit("Goblin", 3, 1, 1, 5), new Unit("Goblin Cleric", 1, 1, 4, 4), new Unit("Goblin Cleric", 1, 1, 4, 4), new Unit("Ogre", 4, 3, 1, 20) }
    };
    private List<Unit> enemiesCurrentBattleList = new List<Unit>(); 

    // Start is called before the first frame update
    void Start()
    {
        buttonActionsAndNames.AddRange(new List<(Action, String)>{(OnDefensiveStanceButton, "Defensive Stance"),
                                                                (OnIntimidateButton, "Intimidate"),
                                                                (OnHideButton, "Hide"),
                                                                (OnSneakAttackButton, "Sneak Attack"),
                                                                (OnHealButton, "Heal"),
                                                                (OnHealingCircleButton, "Healing Circle")});

        foreach(Unit hero in heroesMasterList)
        {
            hero.stableActionsAndNames.Add((OnAttackButton, "Attack"));
            hero.stableActionsAndNames.Add((OnRestButton, "Rest"));
        }

        heroesCurrentBattleList.Clear();
        foreach (Unit hero in heroesMasterList)
        {
            heroesCurrentBattleList.Add(new Unit(hero));
        }

        if (gameState == GameState.TITLE)
        {
            StartCoroutine(Title());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!pausedGame)
        {
            switch (gameState)
            {
                case GameState.TITLE:
                    TitleInput();
                    break;

                case GameState.CHARACTER_CREATION:
                    CharacterCreationInput();
                    break;

                case GameState.BATTLE_START:
                    BattleStartInput();
                    break;

                case GameState.PLAYER_TURN:
                    PlayerTurnInput();
                    break;

                case GameState.ENEMY_TURN:
                    EnemyTurnInput();
                    break;

                case GameState.BATTLE_WON:
                    BattleWonInput();
                    break;
                case GameState.BATTLE_LOST:
                    BattleLostInput();
                    break;
            }
            if (AnyKeyDownMatched(enterKey))
            {
                StopDisplayingMessage();
            }
        }
        

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnPauseToggleButton();
        }
    }

    bool AnyKeyDownMatched(List<KeyCode> keyCodesToCheck)
    {
        foreach (KeyCode keycode in keyCodesToCheck)
        {
            if (Input.GetKeyDown(keycode)) { return true; }
        }
        return false;
    }

    #region Title

    IEnumerator Title()
    {
        //Enable Title UI
        titleUI.SetActive(true);

        while (titleWaitingOnInput)
        {
            yield return null;
        }
        titleWaitingOnInput = true;

        currentBattleIndex = 0;
        turn = 1;

        //Disable Title UI
        titleUI.SetActive(false);
        gameState = GameState.CHARACTER_CREATION;
        StartCoroutine(CharacterCreation());
    }

    void TitleInput()
    {
        //if (AnyKeyDownMatched(enterKey))
        //{

        //}
    }

    public void OnNewGameButton()
    {
        titleWaitingOnInput = false;
    }

    #endregion

    #region CharacterCreation

    private IEnumerator CharacterCreation()
    {
        //characterCreationUI.SetActive(true);

        //while (!characterCreationConfirmed)
        //{
        //    yield return null;
        //}
        //characterCreationConfirmed = false;

        //characterCreationUI.SetActive(false);
        //gameState = GameState.BATTLE_START;
        //StartCoroutine(BattleStart());
        gameState = GameState.BATTLE_START;
        StartCoroutine(BattleStart());
        yield return null;
    }

    private void CharacterCreationInput()
    {
    }

    public void OnConfirmCreationButton()
    {

    }

    #endregion

    #region BattleStart

    private IEnumerator BattleStart()
    {
        if(currentBattleIndex < storyTextbeforeEachBattleList.Count)
        {
            foreach (String storyBite in storyTextbeforeEachBattleList[currentBattleIndex])
            {
                DisplayGeneralMessage(storyBite);
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
        }

        foreach(Unit hero in heroesCurrentBattleList)
        {
            hero.currentThreatLevel = 5;
            hero.defensiveStance = 0;
            hero.turnTaken = false;
        }

        //Initialize the current battles units with clones from the master.
        enemiesCurrentBattleList.Clear();
        foreach(Unit enemy in enemiesInEachBattleMasterList[currentBattleIndex])
        {
            enemiesCurrentBattleList.Add(new Unit(enemy));
        }

        battleStartUI.SetActive(true);
        UpdateTurnOrderPanel();
        gameState = GameState.PLAYER_TURN;
        turn = 1;
        StartCoroutine(PlayerTurn());
        while (true){
            if(gameState == GameState.BATTLE_LOST || gameState == GameState.BATTLE_WON) 
            {
                battleStartUI.SetActive(false);
                break;
            }
            yield return null;
        }
    }

    private void BattleStartInput()
    {

    }

    private void UpdateTurnOrderPanel()
    {
        if(turnOrderPanel != null)
        {
            foreach(Transform child in turnOrderPanel.transform)
            {
                GameObject go = child.gameObject;
                go.SetActive(false);
                Destroy(go);
            }
            List<Unit> displayOrder = new List<Unit>();
            List<Unit> heroesTurnTaken = new List<Unit>();
            List<Unit> heroesTurnNotTaken = new List<Unit>();
            List<Unit> enemiesTurnTaken = new List<Unit>();
            List<Unit> enemiesTurnNotTaken = new List<Unit>();

            //Fill the lists to see whose taken their turns (or ignore them if they're dead).
            foreach(Unit hero in heroesCurrentBattleList)
            {
                if (!hero.IsDead())
                {
                    if (hero.turnTaken)
                    {
                        heroesTurnTaken.Add(hero);
                    }
                    else
                    {
                        heroesTurnNotTaken.Add(hero);
                    }
                }
            }
            foreach (Unit enemy in enemiesCurrentBattleList)
            {
                if (!enemy.IsDead())
                {
                    if (enemy.turnTaken)
                    {
                        enemiesTurnTaken.Add(enemy);
                    }
                    else
                    {
                        enemiesTurnNotTaken.Add(enemy);
                    }
                }

            }
            //Fill in the order that they'll be taking their turns. For now, it's always in order, and alternating teams. 
            //TODO: Think about adding support for non-linear turn order.
            if(gameState == GameState.ENEMY_TURN)
            {
                displayOrder.AddRange(enemiesTurnNotTaken);
                displayOrder.AddRange(heroesTurnNotTaken);
                displayOrder.AddRange(enemiesTurnTaken);
            }
            else
            {
                displayOrder.AddRange(heroesTurnNotTaken);
                displayOrder.AddRange(enemiesTurnNotTaken);
                displayOrder.AddRange(heroesTurnTaken);
            }

            //TODO: If there are more than 6 units in display order, display only a range of 6 and enable buttons that scroll left and right between the content.

            int currentCount = 0;
            int maxCount = 6;
            foreach (Unit unit in displayOrder)
            {
                GameObject go = Instantiate(turnMarkerPrefab);
                go.name = unit.name;    
                foreach(Transform child in go.transform)
                {
                    if(child.gameObject.name == "Name")
                    {
                        child.GetComponent<TextMeshProUGUI>().SetText(unit.name);
                    }
                    if (child.gameObject.name == "Threat")
                    {
                        child.GetComponent<TextMeshProUGUI>().SetText($"Threat {unit.currentThreatLevel} / {unit.maxThreatLevel}");
                    }
                    if (child.gameObject.name == "Heart")
                    {
                        //Set the color of the units heart here?
                    }
                    if(child.gameObject.name == "FakeSpriteMask")
                    {
                        //Adjust the mask so that the heart stays centered, and it masks of a percentage of the heart based on missing health.
                        float defaultHeight = 50;
                        float heightToAdd =  2 * (97 * (float)unit.currentHP/(float)unit.maxHP);
                        RectTransform rectTransform = child.GetComponent<RectTransform>();
                        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, defaultHeight + heightToAdd);
                        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition3D.y -heightToAdd/2);
                    }
                    if(child.gameObject.name == "HP")
                    {
                        child.GetComponent<TextMeshProUGUI>().SetText($"HP\n {unit.currentHP} / {unit.maxHP}");
                    }
                    
                }
                go.transform.SetParent(turnOrderPanel.transform);
                float heightToMatch = turnOrderPanel.transform.GetComponent<RectTransform>().sizeDelta.y - 4;
                go.transform.localScale = new Vector3(heightToMatch/200, heightToMatch/200, go.transform.localScale.z);
                currentCount++;
                if (currentCount >= maxCount) { break; }
            }
        }
    }

    #endregion

    #region PlayerTurn
    private IEnumerator PlayerTurn()
    {
        //playerTurnUI.SetActive(true);
        while(true)
        {
            //Fetch a hero that hasn't taken their turn.
            currentUnit = null;
            foreach(Unit hero in heroesCurrentBattleList)
            {
                if(!hero.IsDead() && !hero.turnTaken)
                {
                    currentUnit = hero;
                    //hero.turnTaken = true;
                    break;
                }
            }            
            //Take their turn
            if (currentUnit != null)
            {
                DisplayBattleMessage(currentUnit.name + " is the acting hero!");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
                if (currentUnit.defensiveStance > 0)
                {
                    currentUnit.defensiveStance--;
                    if (currentUnit.defensiveStance == 0)
                    {
                        DisplayBattleMessage($"{currentUnit.name} is no longer in a defensive stance.");
                        while (messageBoxNeedsUserConfirmation)
                        {
                            yield return null;
                        }
                    }
                }
                currentUnitTakingAction = true;
                //Set the actions for this round.
                foreach(Transform child in actionButtonsPanel.transform)
                {
                    child.gameObject.SetActive(false);
                    Destroy(child.gameObject);
                }
                {
                    GameObject go = Instantiate(actionGroupLabelPrefab);
                    go.name = $"{currentUnit.name}'s Turn Label";
                    go.GetComponent<TextMeshProUGUI>().text = $"{currentUnit.name}'s Turn";
                    go.transform.SetParent(actionButtonsPanel.transform);
                }
                if(currentUnit.stableActionsAndNames.Count > 0)
                {
                    {
                        GameObject go = Instantiate(actionGroupLabelPrefab);
                        go.name = $"Stable Actions Label";
                        go.GetComponent<TextMeshProUGUI>().text = $"Stable Actions";
                        go.transform.SetParent(actionButtonsPanel.transform);
                    }
                    foreach ((Action, String) stableActionAndName in currentUnit.stableActionsAndNames)
                    {
                        GameObject go = Instantiate(actionButtonPrefab);
                        go.GetComponent<Button>().onClick.AddListener(() => stableActionAndName.Item1.Invoke());
                        go.name = stableActionAndName.Item2 + " button";
                        go.GetComponentInChildren<TextMeshProUGUI>().text = stableActionAndName.Item2;
                        go.transform.SetParent(actionButtonsPanel.transform);
                    }
                }
                if(buttonActionsAndNames.Count > 0)
                {
                    GameObject go = Instantiate(actionGroupLabelPrefab);
                    go.name = $"Unstable Actions Label";
                    go.GetComponent<TextMeshProUGUI>().text = $"Unstable Actions";
                    go.transform.SetParent(actionButtonsPanel.transform);

                }
                List<(Action, String)> pickedActions = new List<(Action, string)>();
                for (int i = 0; i < 18; i++)
                {
                    if(buttonActionsAndNames.Count == 0)
                    {
                        break;
                    }
                    (Action, String) a = buttonActionsAndNames[UnityEngine.Random.Range(0, buttonActionsAndNames.Count)];
                    if (pickedActions.Contains(a) || pickedActions.Count > 2) { continue; }
                    GameObject go = Instantiate(actionButtonPrefab);
                    pickedActions.Add(a);
                    go.GetComponent<Button>().onClick.AddListener(() => a.Item1.Invoke());
                    go.name = a.Item2 + " button";
                    go.GetComponentInChildren<TextMeshProUGUI>().text = a.Item2;
                    go.transform.SetParent(actionButtonsPanel.transform);
                }


                while (currentUnitTakingAction)
                {
                    //Get the user selection from the UI, handle the event, display the message to user and then end the turn.
                    yield return null;
                }
                currentUnit.turnTaken = true;
                currentTarget = null;
                actionAndNameToStabilize = (null, null);

                UpdateTurnOrderPanel();

                if (CheckBattleLost())
                {
                    Debug.Log("Players have Lost");
                    gameState = GameState.BATTLE_LOST;
                    StartCoroutine(BattleLost());
                    break;
                }
                if (CheckBattleWon())
                {
                    Debug.Log("Players have Won");
                    gameState = GameState.BATTLE_WON;
                    StartCoroutine(BattleWon());
                    break;
                }
            }
            else
            {
                foreach(Unit hero in heroesCurrentBattleList)
                {
                    hero.turnTaken = false;
                }
                break;
            }
        }
        if (gameState != GameState.BATTLE_LOST && gameState != GameState.BATTLE_WON)
        {
            gameState = GameState.ENEMY_TURN;
            StartCoroutine(EnemyTurn());
        }            
    }

    private void PlayerTurnInput()
    {

    }
    #endregion

    #region EnemyTurn
    private IEnumerator EnemyTurn()
    {
        while (true)
        {
            List<Unit> highestThreatsTargets = new List<Unit>();
            Unit highestThreatUnit = null;
            int highestThreatCount = 0;
            foreach(Unit hero in heroesCurrentBattleList)
            {
                if (!hero.IsDead())
                {
                    if(hero.currentThreatLevel > highestThreatCount)
                    {
                        highestThreatsTargets.Clear();
                        highestThreatsTargets.Add(hero);
                        highestThreatCount = hero.currentThreatLevel;
                    }
                    else if(hero.currentThreatLevel == highestThreatCount)
                    {
                        highestThreatsTargets.Add(hero);
                    }
                }
            }
            highestThreatUnit = highestThreatsTargets[UnityEngine.Random.Range(0, highestThreatsTargets.Count)];


            List<Unit> woundedUnits = new List<Unit>();
            Unit mostWoundedUnit = null;
            int mostHealingNeeded = 0;
            //Fetch a enemy that hasn't taken their turn.
            currentUnit = null;
            foreach (Unit enemy in enemiesCurrentBattleList)
            {
                if (!enemy.IsDead())
                {
                    if(enemy.maxHP > enemy.currentHP)
                    {
                        if(enemy.maxHP-enemy.currentHP > mostHealingNeeded)
                        {
                            woundedUnits.Clear();
                            woundedUnits.Add(enemy);
                            mostHealingNeeded = enemy.maxHP-enemy.currentHP;
                        }
                        else if (enemy.maxHP - enemy.currentHP == mostHealingNeeded)
                        {
                            woundedUnits.Add(enemy);
                        }
                    }
                    if(currentUnit == null && !enemy.turnTaken)
                    {
                        currentUnit = enemy;
                    }
                }
            }
            if(woundedUnits.Count > 0)
            {
                mostWoundedUnit = woundedUnits[UnityEngine.Random.Range(0, woundedUnits.Count)];
            }
            

            //Take their turn
            if (currentUnit != null)
            {
                DisplayBattleMessage($"The enemy {currentUnit.name} is about to act!");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
                if (currentUnit.defensiveStance > 0)
                {
                    currentUnit.defensiveStance--;
                    if (currentUnit.defensiveStance == 0)
                    {
                        DisplayBattleMessage($"{currentUnit.name} is no longer in a defensive stance.");
                        while (messageBoxNeedsUserConfirmation)
                        {
                            yield return null;
                        }
                    }
                }
                currentUnitTakingAction = true;
                if(turn % 4 == 0 && currentUnit.name == "Ogre")
                {
                    DisplayBattleMessage("The ogre lets loose a sweeping attack!");
                    while (messageBoxNeedsUserConfirmation)
                    {
                        yield return null;
                    }
                    foreach(Unit hero in heroesCurrentBattleList)
                    {
                        currentUnitTakingAction = true;
                        currentTarget = hero;
                        StartCoroutine(OnAttack());
                        while (currentUnitTakingAction)
                        {
                            yield return null;
                        }
                    }
                }
                else if (turn % 3 == 0 && currentUnit.name == "Ogre")
                {
                    DisplayBattleMessage("The Ogre is winding up an attack!");
                    while (messageBoxNeedsUserConfirmation)
                    {
                        yield return null;
                    }
                    currentUnitTakingAction = false;
                }
                else
                {
                    float r = UnityEngine.Random.Range(0f, 1f);
                    if (mostWoundedUnit != null && currentUnit.name == "Goblin Cleric")
                    {
                        currentTarget = mostWoundedUnit;
                        StartCoroutine(OnHeal());
                    }
                    else if(currentUnit.defensiveStance == 0 && r > .75f)
                    {
                        StartCoroutine(OnDefensiveStance());
                    }
                    else if(currentUnit.maxHP > currentUnit.currentHP && r > .5f)
                    {
                        StartCoroutine(OnRest());
                    }
                    else
                    {
                        currentTarget = highestThreatUnit;
                        StartCoroutine(OnAttack());
                    }
                }

                while (currentUnitTakingAction)
                {
                    yield return null;
                }

                currentUnit.turnTaken = true;
                currentTarget = null;
                actionAndNameToStabilize = (null, null);

                UpdateTurnOrderPanel();

                if (CheckBattleLost())
                {
                    Debug.Log("Players Have Lost");
                    gameState = GameState.BATTLE_LOST;
                    StartCoroutine(BattleLost());
                    break;
                }
                if (CheckBattleWon())
                {
                    Debug.Log("Players have won");
                    gameState = GameState.BATTLE_WON;
                    StartCoroutine(BattleWon());
                    break;
                }
            }
            else
            {
                foreach(Unit enemy in enemiesCurrentBattleList)
                {
                    enemy.turnTaken = false;
                }
                turn++;
                break;
            }
        }
        if (gameState != GameState.BATTLE_LOST && gameState != GameState.BATTLE_WON)
        {
            gameState = GameState.PLAYER_TURN;
            StartCoroutine(PlayerTurn());
        }
    }

    private void EnemyTurnInput()
    {
    }
    #endregion

    #region BattleWon
    private IEnumerator BattleWon()
    {
        currentBattleIndex++;
        //Check if there are more battles, and if not display a victory screen.
        if (currentBattleIndex < enemiesInEachBattleMasterList.Count)
        {
            //Check a Dictionary for any story text

            //There are more battles
            DisplayGeneralMessage("The battle has been won, but the journey is not over.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            gameState = GameState.BATTLE_START;
            StartCoroutine(BattleStart());
        }
        else
        {
            bool allSkillsStabilized = true;
            foreach(Unit hero in heroesMasterList)
            {
                if(hero.stableActionsAndNames.Count < 4)
                {
                    allSkillsStabilized = false;
                    break;
                }
            }
            DisplayGeneralMessage("The goblin encampment has been cleared, and the heroes have prevailed!");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            if (allSkillsStabilized)
            {
                DisplayGeneralMessage("The heroes still may not know what comes next, but through these encounters they have finally remembered what it's like to have a stable party.");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
            else
            {
                DisplayGeneralMessage("The heroes may have made it through this encounter, but can't help the nagging feeling something about the party still isn't quite right.");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
            DisplayGeneralMessage("Congratulations, and thanks for playing!");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            OnQuitGameButton();
        }
    }

    private void BattleWonInput()
    {
    }

    private bool CheckBattleWon()
    {
        foreach(Unit enemy in enemiesCurrentBattleList)
        {
            if (!enemy.IsDead())
            {
                return false;
            }
        }
        Debug.Log("Battle won");
        return true;
    }
    #endregion

    #region BattleLost
    private IEnumerator BattleLost()
    {
        DisplayGeneralMessage("Struggle as you might, you could not prevail. Perhaps the next group of adventurers will be a bit more stable.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        //Reset the list and start over?
        heroesCurrentBattleList.Clear();
        foreach (Unit hero in heroesMasterList)
        {
            heroesCurrentBattleList.Add(new Unit(hero));
        }
        gameState = GameState.TITLE;
        if (gameState == GameState.TITLE)
        {
            StartCoroutine(Title());
        }

    }

    private void BattleLostInput()
    {

    }

    private bool CheckBattleLost()
    {
        foreach(Unit hero in heroesCurrentBattleList)
        {
            if (!hero.IsDead())
            {
                return false;
            }
        }
        Debug.Log("battle lost");
        return true;
    }
    #endregion    

    private void DisplayBattleMessage(String message)
    {
        messageBattlePanel.gameObject.SetActive(true);
        messageBattlePanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = message;
        messageBoxNeedsUserConfirmation = true;
    }

    private void DisplayGeneralMessage(String message)
    {
        messageGeneralPanel.gameObject.SetActive(true);
        messageGeneralPanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = message;
        messageBoxNeedsUserConfirmation = true;
    }

    private void StopDisplayingMessage()
    {
        if (messageBoxNeedsUserConfirmation)
        {
            messageBoxNeedsUserConfirmation = false;
            messageGeneralPanel.gameObject.SetActive(false);
            messageBattlePanel.gameObject.SetActive(false);
        }
    }

    public void OnRestButton()
    {
        StartCoroutine(OnRest());
    }

    public IEnumerator OnRest()
    {
        ClearActionButtonsPanel();

        int prevHP = currentUnit.currentHP;
        currentUnit.Rest();
        int newHP = currentUnit.currentHP;
        DisplayBattleMessage($"{currentUnit.name} rested, restoring {newHP - prevHP} HP.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        int prevThreat = currentUnit.currentThreatLevel;
        currentUnit.ModifyThreat(-1);
        int newThreat = currentUnit.currentThreatLevel;
        DisplayBattleMessage($"{currentUnit.name} reduced threat by {newThreat - prevThreat}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }

        currentUnitTakingAction = false;
    }

    public void OnAttackButton()
    {
        StartCoroutine(OnAttack());
    }

    public IEnumerator OnAttack()
    {
        TargetSelection();

        while (currentTarget == null)
        {
            yield return null;
        }
        ClearActionButtonsPanel();

        int prevHealth = currentTarget.currentHP;
        bool fatal = currentTarget.TakeDamage(currentUnit.damage);
        int newHealth = currentTarget.currentHP;

        DisplayBattleMessage($"{currentUnit.name} attacked {currentTarget.name} for {prevHealth-newHealth} damage.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        if (fatal)
        {
            int prevThreat = currentUnit.currentThreatLevel;
            currentUnit.ModifyThreat(3);
            int newThreat = currentUnit.currentThreatLevel;
            DisplayBattleMessage($"{currentTarget.name} has taken fatal damage. {currentUnit.name} increased threat by {newThreat-prevThreat}.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        else
        {
            int prevThreat = currentUnit.currentThreatLevel;
            currentUnit.ModifyThreat(1);
            int newThreat = currentUnit.currentThreatLevel;
            DisplayBattleMessage($"{currentUnit.name} increased threat by {newThreat - prevThreat}.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }

        currentUnitTakingAction = false;
    }

    public void OnHideButton()
    {
        StartCoroutine(OnHide());
    }

    public IEnumerator OnHide()
    {

        ClearActionButtonsPanel();
        int prevThreat = currentUnit.currentThreatLevel;
        currentUnit.Hide();
        int newThreat = currentUnit.currentThreatLevel;
        DisplayBattleMessage($"{currentUnit.name} hid, reducing threat by {prevThreat-newThreat}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        if (currentUnit.name == "Rogue" && !currentUnit.stableActionsAndNames.Contains((OnHideButton, "Hide")))
        {
            DisplayBattleMessage($"The {currentUnit.name} is beginning to remember it's calling. Hide Stabilized!");
            currentUnit.stableActionsAndNames.Add((OnHideButton, "Hide"));
            foreach (Unit hero in heroesMasterList)
            {
                if (hero.name == "Rogue")
                {
                    hero.stableActionsAndNames.Add((OnHideButton, "Hide"));
                }
            }
            buttonActionsAndNames.Remove((OnHideButton, "Hide"));
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        currentUnitTakingAction = false;
    }

    public void OnSneakAttackButton()
    {
        StartCoroutine(OnSneakAttack());
    }

    public IEnumerator OnSneakAttack()
    {
        TargetSelection();
        while (currentTarget == null)
        {
            yield return null;
        }
        ClearActionButtonsPanel();
        bool lowestThreat = true;
        if (heroesCurrentBattleList.Contains(currentUnit))
        {
            foreach(Unit hero in heroesCurrentBattleList)
            {
                if(currentUnit.currentThreatLevel >= hero.currentThreatLevel && currentUnit != hero)
                {
                    lowestThreat = false;
                    break;
                }
            }
        }
        if (enemiesCurrentBattleList.Contains(currentUnit))
        {
            foreach(Unit enemy in enemiesCurrentBattleList)
            {
                if(currentUnit.currentThreatLevel >= enemy.currentThreatLevel)
                {
                    lowestThreat = false;
                    break;
                }
            }
        }

        if (lowestThreat)
        {
            int prevHealth = currentTarget.currentHP;
            bool fatal = currentTarget.TakeDamage(currentUnit.damage * 3);
            int newHealth = currentTarget.currentHP;
            DisplayBattleMessage($"{currentUnit.name} snuck up on {currentTarget.name}, dealing {prevHealth-newHealth} damage!");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            if (fatal)
            {
                DisplayBattleMessage($"{currentTarget.name} has taken fatal damage, but no one noticed. {currentUnit.name}'s current threat remains {currentUnit.currentThreatLevel}.");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
            else
            {
                int prevThreat = currentUnit.currentThreatLevel;
                currentUnit.ModifyThreat(2);
                int newThreat = currentUnit.currentThreatLevel;
                DisplayBattleMessage($"{currentUnit.name} increased threat by {newThreat - prevThreat}.");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
            if (currentUnit.name == "Rogue" && !currentUnit.stableActionsAndNames.Contains((OnSneakAttackButton, "Sneak Attack")))
            {
                DisplayBattleMessage($"The {currentUnit.name} is beginning to remember it's calling. Sneak Attack Stabilized!");
                currentUnit.stableActionsAndNames.Add((OnSneakAttackButton, "Sneak Attack"));
                foreach(Unit hero in heroesMasterList)
                {
                    if(hero.name == "Rogue")
                    {
                        hero.stableActionsAndNames.Add((OnSneakAttackButton, "Sneak Attack"));
                    }
                }
                buttonActionsAndNames.Remove((OnSneakAttackButton, "Sneak Attack"));
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
        }
        else
        {
            DisplayBattleMessage($"{currentUnit.name} attempted to sneak attack, but {currentTarget.name} noticed because their threat was too high.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }

        currentUnitTakingAction = false;
    }

    public void OnIntimidateButton()
    {
        StartCoroutine(OnIntimidate());
    }

    public IEnumerator OnIntimidate()
    {
        ClearActionButtonsPanel();
        int prevThreat = currentUnit.currentThreatLevel;
        currentUnit.Intimidate();
        int newThreat = currentUnit.currentThreatLevel;
        DisplayBattleMessage($"{currentUnit.name} intimidated the enemy, increasing threat by {newThreat - prevThreat}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        if (currentUnit.name == "Warrior" && !currentUnit.stableActionsAndNames.Contains((OnIntimidateButton, "Intimidate")))
        {
            DisplayBattleMessage($"The {currentUnit.name} is beginning to remember it's calling. Intimidate Stabilized!");
            currentUnit.stableActionsAndNames.Add((OnIntimidateButton, "Intimidate"));
            foreach (Unit hero in heroesMasterList)
            {
                if (hero.name == "Warrior")
                {
                    hero.stableActionsAndNames.Add((OnIntimidateButton, "Intimidate"));
                }
            }
            buttonActionsAndNames.Remove((OnIntimidateButton, "Intimidate"));
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        currentUnitTakingAction = false;
    }

    public void OnHealButton()
    {
        StartCoroutine(OnHeal());
    }

    public IEnumerator OnHeal()
    {
        TargetSelection();

        while (currentTarget == null)
        {
            yield return null;
        }
        ClearActionButtonsPanel();

        int prevHP = currentTarget.currentHP;
        currentTarget.Heal(currentUnit.restoration);
        int newHP = currentUnit.currentHP;
        DisplayBattleMessage($"{currentUnit.name} healed {currentTarget.name} for {newHP-prevHP}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        if (currentUnit.name == "Cleric" && !currentUnit.stableActionsAndNames.Contains((OnHealButton, "Heal")))
        {
            DisplayBattleMessage($"The {currentUnit.name} is beginning to remember it's calling. Heal Stabilized!");
            currentUnit.stableActionsAndNames.Add((OnHealButton, "Heal"));
            foreach (Unit hero in heroesMasterList)
            {
                if (hero.name == "Cleric")
                {
                    hero.stableActionsAndNames.Add((OnHealButton, "Heal"));
                }
            }
            buttonActionsAndNames.Remove((OnHealButton, "Heal"));
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        currentUnitTakingAction = false;
    }

    public void OnHealingCircleButton()
    {
        StartCoroutine(OnHealingCircle());
    }

    public IEnumerator OnHealingCircle()
    {
        foreach (Transform child in actionButtonsPanel.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        DisplayBattleMessage($"{currentUnit.name} begins drawing a symbols on the ground.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        DisplayBattleMessage($"Light pierces down radiatng in a circle near the symbols.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        foreach (Unit hero in heroesCurrentBattleList)
        {
            int prevHP = hero.currentHP;
            hero.Heal(currentUnit.restoration/3);
            int newHP = hero.currentHP;
            DisplayBattleMessage($"{currentUnit.name} healed {hero.name} for {newHP - prevHP}.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        if (currentUnit.name == "Cleric" && !currentUnit.stableActionsAndNames.Contains((OnHealingCircleButton, "Healing Circle")))
        {
            DisplayBattleMessage($"The {currentUnit.name} is beginning to remember it's calling. Healing Circle Stabilized!");
            currentUnit.stableActionsAndNames.Add((OnHealingCircleButton, "Healing Circle"));
            foreach (Unit hero in heroesMasterList)
            {
                if (hero.name == "Cleric")
                {
                    hero.stableActionsAndNames.Add((OnHealingCircleButton, "Healing Circle"));
                }
            }
            buttonActionsAndNames.Remove((OnHealingCircleButton, "Healing Circle"));
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        currentUnitTakingAction = false;
    }

    public void OnDefensiveStanceButton()
    {
        StartCoroutine(OnDefensiveStance());
    }

    public IEnumerator OnDefensiveStance()
    {
        ClearActionButtonsPanel();
        if (currentUnit.defensiveStance > 0)
        {
            DisplayBattleMessage($"{currentUnit.name} maintained a defensive stance.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            currentUnit.defensiveStance = 2;
        }
        else
        {
            DisplayBattleMessage($"{currentUnit.name} took a defensive stance.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            currentUnit.defensiveStance = 2;
        }

        if (currentUnit.name == "Warrior" && !currentUnit.stableActionsAndNames.Contains((OnDefensiveStanceButton, "Defensive Stance")))
        {
            DisplayBattleMessage($"The {currentUnit.name} is beginning to remember it's calling. Defensive Stance Stabilized!");
            currentUnit.stableActionsAndNames.Add((OnDefensiveStanceButton, "Defensive Stance"));
            foreach (Unit hero in heroesMasterList)
            {
                if (hero.name == "Warrior")
                {
                    hero.stableActionsAndNames.Add((OnDefensiveStanceButton, "Defensive Stance"));
                }
            }
            buttonActionsAndNames.Remove((OnDefensiveStanceButton, "Defensive Stance"));
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
        }
        
        currentUnitTakingAction = false;
    }

    public void OnQuitGameButton()
    {
        #if UNITY_STANDALONE
            Application.Quit();
        #endif
        #if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
        #endif
    }

    public void OnPauseToggleButton()
    {
        if (pausedGame)
        {
            pausedGame = false;
            pauseUI.gameObject.SetActive(false);
        }
        else
        {
            pausedGame = true;
            pauseUI.gameObject.SetActive(true);
        }
    }

    private void TargetSelection()
    {
        if(currentTarget != null) { return; }
        foreach (Transform child in actionButtonsPanel.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        //TODO: Cleanup the following two for loops, since we don't really the same code with different names for each list.        
        {
            GameObject go = Instantiate(actionGroupLabelPrefab);
            go.name = "Selectable Heroes Label";
            go.GetComponent<TextMeshProUGUI>().text = "Selectable Heroes";
            go.transform.SetParent(actionButtonsPanel.transform);
        }
        for (int i = 0; i < heroesCurrentBattleList.Count; i++)
        {
            Unit hero = heroesCurrentBattleList[i];
            if (!hero.IsDead())
            {
                GameObject go = Instantiate(actionButtonPrefab);
                go.GetComponent<Button>().onClick.AddListener(() => currentTarget = hero);
                go.name = hero.name;
                go.GetComponentInChildren<TextMeshProUGUI>().text = hero.name;
                go.transform.SetParent(actionButtonsPanel.transform);
            }
        }
        {
            GameObject go = Instantiate(actionGroupLabelPrefab);
            go.name = "Selectable Enemies Label";
            go.GetComponent<TextMeshProUGUI>().text = "Selectable Enemies";
            go.transform.SetParent(actionButtonsPanel.transform);
        }
        for(int i = 0; i < enemiesCurrentBattleList.Count; i++)
        {
            Unit enemy = enemiesCurrentBattleList[i];
            if (!enemy.IsDead())
            {
                GameObject go = Instantiate(actionButtonPrefab);
                go.GetComponent<Button>().onClick.AddListener(() => currentTarget = enemy);
                go.name = enemy.name;
                go.GetComponentInChildren<TextMeshProUGUI>().text = enemy.name;
                go.transform.SetParent(actionButtonsPanel.transform);
            }
        }
    }

    private void ClearActionButtonsPanel()
    {
        foreach (Transform child in actionButtonsPanel.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

}
