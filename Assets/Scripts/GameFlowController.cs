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
    private GameObject messagePanel = null;
    [SerializeField]
    private GameObject actionButtonsPanel = null;
    [SerializeField]
    private GameObject actionButtonPrefab = null;
    [SerializeField]
    private GameObject actionGroupLabelPrefab = null;


    private int currentBattle = 2;

    private List<Unit> heroesMasterList = new List<Unit> { new Unit("Warrior", 1, 3, 1, 10), new Unit("Rogue", 1, 2, 1, 5), new Unit("Cleric", 1, 1, 4, 5) };
    private List<Unit> heroesCurrentBattleList = new List<Unit>();
    private List<List<Unit>> enemiesInEachBattleMasterList = new List<List<Unit>> {
        //Battle 1
        new List<Unit>{ new Unit("Goblin", 1, 1, 1, 5) },
        //Battle 2
        new List<Unit>{ new Unit("Goblin", 1, 1, 1, 5), new Unit("Goblin", 1, 1, 1, 5) },
        //Battle 3
        new List<Unit>{ new Unit("Goblin", 1, 1, 1, 5), new Unit("Goblin Cleric", 1, 1, 4, 4), new Unit("Ogre", 3, 3, 1, 10) }
    };
    private List<Unit> enemiesCurrentBattleList = new List<Unit>(); 

    // Start is called before the first frame update
    void Start()
    {
        buttonActionsAndNames.AddRange(new List<(Action, String)>{(OnAttackButton, "Attack"), 
                                                                (OnDefensiveStanceButton, "Defensive Stance"), 
                                                                (OnHealButton, "Heal"), 
                                                                (OnHideButton, "Hide"), 
                                                                (OnIntimidateButton, "Intimidate"), 
                                                                (OnRestButton, "Rest"), 
                                                                (OnSneakAttackButton, "Sneak Attack")});
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
        //Initialize the current battles units with clones from the master.
        heroesCurrentBattleList.Clear();
        foreach(Unit hero in heroesMasterList){
            heroesCurrentBattleList.Add(new Unit(hero));
        }
        enemiesCurrentBattleList.Clear();
        foreach(Unit enemy in enemiesInEachBattleMasterList[currentBattle])
        {
            enemiesCurrentBattleList.Add(new Unit(enemy));
        }

        battleStartUI.SetActive(true);
        UpdateTurnOrderPanel();
        gameState = GameState.PLAYER_TURN;
        StartCoroutine(PlayerTurn());
        while (true){
            //Display text for the battle here, until all the text has been advanced through.
            yield return null;
        }
        //We probably want to set this false after the battle moves to the win or lose state.
        //battleStartUI.SetActive(false);
        

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
                    
                }
                go.transform.SetParent(turnOrderPanel.transform);
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
                messageBoxNeedsUserConfirmation = true;
                DisplayMessage(currentUnit.name + " is the acting hero!");
                currentUnit.defensiveStance--;
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
                {
                    GameObject go = Instantiate(actionGroupLabelPrefab);
                    go.name = $"Unstable Actions Label";
                    go.GetComponent<TextMeshProUGUI>().text = $"Unstable Actions";
                    go.transform.SetParent(actionButtonsPanel.transform);
                }
                for (int i = 0; i < 3; i++)
                {
                    GameObject go = Instantiate(actionButtonPrefab);
                    (Action, String) a = buttonActionsAndNames[UnityEngine.Random.Range(0, buttonActionsAndNames.Count)];
                    go.GetComponent<Button>().onClick.AddListener(() => a.Item1.Invoke());
                    go.name = a.Item2 + " button";
                    go.GetComponentInChildren<TextMeshProUGUI>().text = a.Item2;
                    go.transform.SetParent(actionButtonsPanel.transform);
                }
                if(UnityEngine.Random.Range(0f, 1f) > .75)
                {
                    GameObject go = Instantiate(actionButtonPrefab);
                    go.GetComponent<Button>().onClick.AddListener(() => OnStabilizeActionButton());
                    go.name = "Stabilize Action" + " button";
                    go.GetComponentInChildren<TextMeshProUGUI>().text = "Stabilize Action";
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
                    gameState = GameState.BATTLE_LOST;
                    StartCoroutine(BattleLost());
                    break;
                }
                if (CheckBattleWon())
                {
                    gameState = GameState.BATTLE_WON;
                    StartCoroutine(BattleWon());
                    break;
                }
            }
            else
            {
                //All players have acted, so break.
                Debug.Log("All players acted");
                break;
            }
        }
        //playerTurnUI.SetActive(false);
        gameState = GameState.ENEMY_TURN;
        StartCoroutine(EnemyTurn());
    }

    private void PlayerTurnInput()
    {
        if (AnyKeyDownMatched(enterKey))
        {
            StopDisplayingMessage();
        }
    }
    #endregion

    #region EnemyTurn
    private IEnumerator EnemyTurn()
    {
        Debug.Log("Enemy turn begin");
        enemyTurnUI.SetActive(true);
        yield return null;
        enemyTurnUI.SetActive(true);
    }

    private void EnemyTurnInput()
    {
        if (AnyKeyDownMatched(enterKey))
        {
            StopDisplayingMessage();
        }
    }
    #endregion

    #region BattleWon
    private IEnumerator BattleWon()
    {
        battleWonUI.SetActive(true);
        yield return null;
        battleWonUI.SetActive(false);
    }

    private void BattleWonInput()
    {
        if (AnyKeyDownMatched(enterKey))
        {
            StopDisplayingMessage();
        }
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
        return true;
    }
    #endregion

    #region BattleLost
    private IEnumerator BattleLost()
    {
        battleLostUI.SetActive(true);
        yield return null;
        battleLostUI.SetActive(false);
    }

    private void BattleLostInput()
    {
        if (AnyKeyDownMatched(enterKey))
        {
            StopDisplayingMessage();
        }
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
        return true;
    }
    #endregion    

    private void DisplayMessage(String message)
    {
        messagePanel.gameObject.SetActive(true);
        messagePanel.transform.GetComponentInChildren<TextMeshProUGUI>().text = message;
        messageBoxNeedsUserConfirmation = true;
    }
    private void StopDisplayingMessage()
    {
        if (messageBoxNeedsUserConfirmation)
        {
            messageBoxNeedsUserConfirmation = false;
            messagePanel.gameObject.SetActive(false);
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
        DisplayMessage($"{currentUnit.name} rested, restoring {newHP - prevHP} HP.");
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

        DisplayMessage($"{currentUnit.name} attacked {currentTarget.name} for {prevHealth-newHealth}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
        }
        if (fatal)
        {
            int prevThreat = currentUnit.currentThreatLevel;
            currentUnit.ModifyThreat(3);
            int newThreat = currentUnit.currentThreatLevel;
            DisplayMessage($"{currentTarget.name} has taken fatal damage. {currentUnit.name} increased threat by {newThreat-prevThreat}.");
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
            DisplayMessage($"{currentUnit.name} increased threat by {newThreat - prevThreat}.");
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
        yield return null;
        ClearActionButtonsPanel();
        int prevThreat = currentUnit.currentThreatLevel;
        currentUnit.Hide();
        int newThreat = currentUnit.currentThreatLevel;
        DisplayMessage($"{currentUnit.name} hid, reducing threat by {prevThreat-newThreat}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
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
        if (currentUnit.currentThreatLevel <= 3)
        {
            int prevHealth = currentTarget.currentHP;
            bool fatal = currentTarget.TakeDamage(currentUnit.damage * 3);
            int newHealth = currentTarget.currentHP;
            DisplayMessage($"{currentUnit.name} snuck up on {currentTarget.name}, dealing {prevHealth-newHealth} damage!");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            if (fatal)
            {
                int prevThreat = currentUnit.currentThreatLevel;
                currentUnit.ModifyThreat(3);
                int newThreat = currentUnit.currentThreatLevel;
                DisplayMessage($"{currentTarget.name} has taken fatal damage. {currentUnit.name} increased threat by {newThreat - prevThreat}.");
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
                DisplayMessage($"{currentUnit.name} increased threat by {newThreat - prevThreat}.");
                while (messageBoxNeedsUserConfirmation)
                {
                    yield return null;
                }
            }
        }
        else
        {
            DisplayMessage($"{currentUnit.name} attempted to sneak attack, but {currentTarget.name} noticed because their threat was too high.");
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
        DisplayMessage($"{currentUnit.name} intimidated the enemy, increasing threat by {newThreat - prevThreat}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
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
        DisplayMessage($"{currentUnit.name} healed {currentTarget.name} for {newHP-prevHP}.");
        while (messageBoxNeedsUserConfirmation)
        {
            yield return null;
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
            DisplayMessage($"{currentUnit.name} maintained a defensive stance.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            currentUnit.defensiveStance = 2;
        }
        else
        {
            DisplayMessage($"{currentUnit.name} took a defensive stance.");
            while (messageBoxNeedsUserConfirmation)
            {
                yield return null;
            }
            currentUnit.defensiveStance = 2;
        }

        currentUnitTakingAction = false;
    }

    public void OnStabilizeActionButton()
    {
        StartCoroutine(OnStabilizeAction());
    }

    public IEnumerator OnStabilizeAction()
    {
        //Populate with the ActionsAndNames that can be stabilized.
        //ActionToStabilizeSelection();
        while (actionAndNameToStabilize == (null, null))
        {
            yield return null;
        }
        foreach (Transform child in actionButtonsPanel.transform)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        if (actionAndNameToStabilize != (null, null) && !currentUnit.stableActionsAndNames.Contains(actionAndNameToStabilize))
        {
            //TODO: Check if the class trying to stabilize is allowed to stabilize that action, if not fail out and let the player know that a class can only stabilize it's own actions.

            currentUnit.stableActionsAndNames.Add(actionAndNameToStabilize);
            DisplayMessage($"{currentUnit.name} stabilized the ({actionAndNameToStabilize.Item2}) action!");
        }
        else
        {
            DisplayMessage($"{currentUnit.name} has already stabilized the ({actionAndNameToStabilize.Item2}) action.");
        }
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
