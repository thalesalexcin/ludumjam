﻿using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameController : MonoBehaviour 
{
    public GameObject GameScene;
    public GameObject ScoreScene;

    public CursorController CursorCtrl;
    public BlockController BlockCtrl;
    public GameManager GameManager;
    public UIManager UIManager;

    public float EnterFadeDuration = 1;
    public Image Fader;

    public AudioSource LevelUp;
    public AudioSource BackgroundThemeMusic;
    public AudioSource BackgroundMenuMusic;
    public AudioSource GameOver;
    public AudioSource ValidBlock;
    public AudioSource InvalidBlock;

    public GameObject ValidArea;
    public GameObject BlockArea;

    private Vector3 _OldBlockAreaPosition;

    private GameObject _CurrentValidationBlocks;

    public Text ScoreValue;
    public InputField InputField;

    public List<Text> LeaderboardNames;
    public List<Text> LeaderboardScore;
    public Text YourScore;
    public Text YourScoreLeaderboard;

    public GameObject Leaderboard;
    public GameObject SubmitForm;

    void Start()
    {
        _OldBlockAreaPosition = BlockArea.transform.position;
        _CurrentSceneState = SceneState.GAME;
        _CurrentState = GameState.LOADING;
        _CurrentScoreState = ScoreState.SUBMITING;
        _BDDManager = GameManager.GetComponent<BDDManager>();
        Fader.SetOpacity(1);
    }
    
    private GameState _CurrentState;
    private ScoreState _CurrentScoreState;
    private SceneState _CurrentSceneState;
    private float _CurrentTimer;
    private BDDManager _BDDManager;

    enum ScoreState
    {
        SUBMITING,
        WAITING_SUBMISSION,
        LOADING_SCORE,
        SHOW_SCORE,
        ERROR
    }

    enum SceneState
    {
        GAME,
        PAUSE,
        SCORE,
    }

    enum GameState
    {
        LOADING,
        START,
        MOVING,
        ANIMATE_NEXT_LEVEL,
        GENERATE_NEXT_BLOCK
    }

	// Update is called once per frame
	void Update () 
    {
        switch (_CurrentSceneState)
        {
            case SceneState.GAME: _GameState();
                break;
            case SceneState.PAUSE: _PauseState();
                break;
            case SceneState.SCORE: _ScoreState();
                break;
        }
	}

    private void _ScoreState()
    {
        switch (_CurrentScoreState)
        {
            case ScoreState.SUBMITING: _SubmitingScoreState();
                break;
            case ScoreState.WAITING_SUBMISSION: _WaitingSubmissionState();
                break;
            case ScoreState.LOADING_SCORE: _LoadingScoreState();
                break;
        }
    }

    private void _LoadingScoreState()
    {
        if (_BDDManager.IsFinished)
        {
            if (_BDDManager.HasError)
                _CurrentScoreState = ScoreState.ERROR;
            else
            {
                SubmitForm.SetActive(false);
                Leaderboard.SetActive(true);

                YourScoreLeaderboard.text = GameManager.score.ToString().PadLeft(7, '0');

                for (int i = 0; i < 5; i++)
                {
                    LeaderboardNames[i].text = _BDDManager.listePseudo[i+1];
                    LeaderboardScore[i].text = _BDDManager.listeScore[i+1];
                }
            }
        }
    }

    private void _WaitingSubmissionState()
    {
        if (_BDDManager.IsFinished)
        {
            if (_BDDManager.HasError)
                _CurrentScoreState = ScoreState.ERROR;
            else
            {
                _BDDManager.loadScores();
                _CurrentScoreState = ScoreState.LOADING_SCORE;
            }
        }
    }

    private void _SubmitingScoreState()
    {
        InputField.Select();
        YourScore.text = GameManager.score.ToString().PadLeft(7, '0');

        if (Input.GetKeyDown(KeyCode.Return))
        {
            ValidBlock.Play();
            GameManager.GetComponent<BDDManager>().recordScore(InputField.text, GameManager.score);
            _CurrentScoreState = ScoreState.WAITING_SUBMISSION;
        }
    }
        
    private void _PauseState()
    {
        
    }

    private void _GameState()
    {
        switch (_CurrentState)
        {
            case GameState.LOADING: _LoadState();
                break;
            case GameState.START: _StartState();
                break;
            case GameState.MOVING: _MoveState();
                break;
            case GameState.GENERATE_NEXT_BLOCK: _GenerateBlockState();
                break;
            case GameState.ANIMATE_NEXT_LEVEL: _AnimateNextLevelState();
                break;
        }
    }

    private void _AnimateNextLevelState()
    {
        _CurrentTimer += Time.deltaTime;
        UIManager.BlinkNextLevel();
        if (_CurrentTimer >= GameManager.NextLevelAnimationDuration)
        {
            UIManager.HideNextLevel();
            _CurrentState = GameState.GENERATE_NEXT_BLOCK;
        }
    }

    private void _GenerateBlockState()
    {
        BlockCtrl.SpeedMultiplier = GameManager.speed;
        _AddNextBlock();
        _CurrentState = GameState.MOVING;
    }

    private void _MoveState()
    {

        if (BlockCtrl.HasValidated)
        {
            var hasPassedLevel = GameManager.nextLevel(BlockCtrl.IsValid);

            if (GameManager.HasEnergyBar())
                _ContinueGame(hasPassedLevel);
            else
                _ChangeToScoreScene();
        }
    }

    private void _ContinueGame(bool hasPassedLevel)
    {
        var isValid = BlockCtrl.IsValid;
        _ClearLevel();

        if (hasPassedLevel)
        {
            _CurrentState = GameState.ANIMATE_NEXT_LEVEL;
            _CurrentTimer = 0;
            LevelUp.Play();
        }
        else
        {
            if (isValid)
                ValidBlock.Play();
            else
                InvalidBlock.Play();

            _CurrentState = GameState.GENERATE_NEXT_BLOCK;
        }
    }

    private void _ChangeToScoreScene()
    {
        ScoreValue.text = GameManager.score.ToString().PadLeft(7, '0');
        
        _CurrentSceneState = SceneState.SCORE;
        GameScene.SetActive(false);
        ScoreScene.SetActive(true);
        
        GameOver.Play();
        BackgroundMenuMusic.Play();
        BackgroundThemeMusic.Stop();
    }

    private void _LoadState()
    {
        _CurrentTimer += Time.deltaTime;

        var percentage = _CurrentTimer / EnterFadeDuration;

        Fader.SetOpacity(1 - percentage);

        if (_CurrentTimer >= EnterFadeDuration)
        {
            _CurrentTimer = 3;
            _CurrentState = GameState.START;
        }
    }

    private void _StartState()
    {
        _CurrentTimer -= Time.deltaTime;

        UIManager.SetCounter(_CurrentTimer);

        if (_CurrentTimer <= 0)
        {
            BackgroundThemeMusic.Play();
            _AddNextBlock();
            BlockCtrl.SpeedMultiplier = GameManager.speed;
            _CurrentState = GameState.MOVING;
        }
    }

    private void _AddNextBlock()
    {
        var validBlockSet = _GetNextValidBlockSet();
        _InstantiateValidBlockSet(validBlockSet);

        var blockSet = _InstantiateBlankBlockSet(validBlockSet);
        BlockCtrl.ClearBlockSet();
        BlockCtrl.SetBlockSet(blockSet);

        var cursor = _InstantiateCursor(BlockArea);
        CursorCtrl.SetCursor(cursor);

        _SetBlockAreaOpacity(1);
    }

    private GameObject _InstantiateCursor(GameObject blockSet)
    {
        var cursorPrefab = _GetNextCursor();
        var cursor = Instantiate(cursorPrefab);
        cursor.transform.parent = blockSet.transform;
        cursor.transform.localPosition = Vector3.zero;
        cursor.transform.localScale = Vector3.one;
        return cursor;
    }

    private GameObject _GetNextCursor()
    {
        return GameManager.cursor;
    }

    private GameObject _GetNextValidBlockSet()
    {
        return GameManager.Fac;
    }

    private void _ClearLevel()
    {
        if (_CurrentValidationBlocks != null)
            DestroyObject(_CurrentValidationBlocks);

        _SetBlockAreaOpacity(0);
        BlockCtrl.ClearBlockSet();
        CursorCtrl.ClearCursor();
    }

    private void _SetBlockAreaOpacity(float a)
    {
        var color = BlockArea.GetComponent<SpriteRenderer>().color;
        color.a = a;
        BlockArea.GetComponent<SpriteRenderer>().color = color;
    }

    private void _InstantiateValidBlockSet(GameObject blockSet)
    {
        _CurrentValidationBlocks = Instantiate(blockSet);
        _CurrentValidationBlocks.transform.parent = ValidArea.transform;
        _CurrentValidationBlocks.transform.localScale = Vector3.one;
        _CurrentValidationBlocks.transform.localPosition = Vector3.zero;
    }

    private GameObject _InstantiateBlankBlockSet(GameObject blockSet)
    {
        var blankBlockSet = Instantiate(blockSet);
        BlockArea.transform.position = _OldBlockAreaPosition;
        blankBlockSet.transform.parent = BlockArea.transform;
        blankBlockSet.transform.localPosition = Vector3.zero;
        blankBlockSet.transform.localScale = Vector3.one;
        return blankBlockSet;
    }
}
