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
   
    private Unit currentUnit = null;
    private Unit currentTarget = null;

    private readonly List<KeyCode> enterKey = new List<KeyCode> { KeyCode.Return, KeyCode.KeypadEnter, KeyCode.Space,  KeyCode.Mouse0};
    private readonly List<KeyCode> upKey = new List<KeyCode> { KeyCode.UpArrow, KeyCode.W };
    private readonly List<KeyCode> downKey = new List<KeyCode> { KeyCode.DownArrow, KeyCode.S };
    private readonly List<KeyCode> leftKey = new List<KeyCode> { KeyCode.LeftArrow, KeyCode.A };
    private readonly List<KeyCode> rightKey = new List<KeyCode> { KeyCode.RightArrow, KeyCode.D };

    [SerializeField]
    private GameObject titleUI = null;
    //[SerializeField]
    //private GameObject characterCreationUI = null;
    [SerializeField]
    private GameObject battleStartUI = null;
    [SerializeField]
    private GameObject playerTurnUI = null;
    [SerializeField]
    private GameObject enemyTurnUI= null;
    [SerializeField]
    private GameObject battleWonUI = null;
    [SerializeField]
    private GameObject battleLostUI= null;

    [SerializeField]
    private GameObject turnMarkerPrefab = null;
    [SerializeField]
    private GameObject turnOrderPanel = null;

    [SerializeField] 
    private GameObject messagePanel = null;

    private int currentBattle = 2;

    private List<Unit> heroesMasterList = new List<Unit> { new Unit("Warrior", 1, 3, 10), new Unit("Rogue", 1, 2, 5), new Unit("Cleric", 1, 1, 5) };
    private List<Unit> heroesCurrentBattleList = new List<Unit>();
    private List<List<Unit>> enemiesInEachBattleMasterList = new List<List<Unit>> {
        //Battle 1
        new List<Unit>{ new Unit("Goblin", 1, 1, 5) },
        //Battle 2
        new List<Unit>{ new Unit("Goblin", 1, 1, 5), new Unit("Goblin", 1, 1, 5) },
        //Battle 3
        new List<Unit>{ new Unit("Goblin", 1, 1, 5), new Unit("Goblin Cleric", 1, 1, 4), new Unit("Ogre", 3, 3, 10)}
    };
    private List<Unit> enemiesCurrentBattleList = new List<Unit>(); 

    // Start is called before the first frame update
    void Start()
    {
        if(gameState == GameState.TITLE)
        {
            StartCoroutine(Title());
        }
    }

    // Update is called once per frame
    void Update()
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

    public void OnQuitGameButton()
    {
        #if UNITY_STANDALONE
            Application.Quit();
        #endif
        #if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
        #endif
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
        playerTurnUI.SetActive(true);
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
                currentUnitTakingAction = true;
                while (currentUnitTakingAction)
                {
                    //Get the user selection from the UI, handle the event, display the message to user and then end the turn.
                    yield return null;
                }
                currentUnit.turnTaken = true;
                UpdateTurnOrderPanel();
            }
            else
            {
                //All players have acted, so break.
                break;
            }
        }
        playerTurnUI.SetActive(false);
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
        enemyTurnUI.SetActive(true);
        yield return null;
        enemyTurnUI.SetActive(true);
    }

    private void EnemyTurnInput()
    {
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
        currentUnitTakingAction = false;
        int prevHP = currentUnit.currentHP;
        currentUnit.Rest();
        int newHP= currentUnit.currentHP;
        DisplayMessage($"{currentUnit.name} rested, restoring {newHP-prevHP} HP.");
    }

    public void OnAttackButton()
    {
        currentUnitTakingAction = false;
        DisplayMessage($"{currentUnit.name} attacked {currentTarget.name} for {currentUnit.damage}.");
        if (currentTarget.TakeDamage(currentUnit.damage))
        {
            DisplayMessage($"{currentTarget.name} has taken fatal damage.");
            currentUnit.ModifyThreat(3);
        }
        else
        {
            currentUnit.ModifyThreat(1);
        }
    }

    public void OnHideButton()
    {

    }

}
