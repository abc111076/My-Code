using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public enum GameState
{
    None,
    GameReady,
    GameStart,
    GamePlaying,
    GameFinish,
}

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }

            return _instance;
        }
    }

    [SerializeField]
    private GameState currentGameState = GameState.None;

    private float gameTime = 0;
    private bool gameStart;

    public GameState CurrentGameState { get { return currentGameState; } }
    public float GameTime { get { return gameTime; } }

    private void Awake()
    {
        if (Initialize.Connect)
            EventDataWrapper.AddEventAction(EventHandler);

        EventManager.ChangeGameStateHandler += ChangeGameState;
    }

    private void Update()
    {
        if (gameStart)
        {
            gameTime += Time.deltaTime;
            EventManager.EventHappen(EventManagerCode.UpdateUIEvent, UIInformation.TimeUpdate, gameTime);
        }
        else
        {
            if (gameTime != 0)
                gameTime = 0;
        }
    }

    private void OnApplicationQuit()
    {
        BikeAPI.SportData.QuitApp();
    }

    private void EventHandler(byte eventCode, object content, int senderId)
    {
        EventDataWrapper edw = new EventDataWrapper(content);
        switch (eventCode)
        {
            case cEventCode.ChangeGameState:
                PhotonCatchGameState(edw);
                break;
        }
    }

    private void ChangeGameState(GameState gs)
    {
        if (currentGameState == gs)
            return;

        EventManager.EventHappen(EventManagerCode.GetGameStateEvent, gs);

        if (CurrentGameState != GameState.None)
        {
            PhotonSendGameStateInfo(gs);
        }

        currentGameState = gs;

        switch (gs)
        {
            case GameState.GameStart:
                currentGameState = GameState.GamePlaying;
                EventManager.EventHappen(EventManagerCode.GetGameStateEvent, GameState.GamePlaying);
                gameStart = true;
                break;

            case GameState.GameFinish:
                gameStart = false;
                break;
        }
    }

    private void SetGameState(GameState gs)
    {
        EventManager.EventHappen(EventManagerCode.GetGameStateEvent, gs);
        currentGameState = gs;

        switch (gs)
        {
            case GameState.GameStart:
                currentGameState = GameState.GamePlaying;
                EventManager.EventHappen(EventManagerCode.GetGameStateEvent, GameState.GamePlaying);
                gameStart = true;
                break;

            case GameState.GameFinish:
                gameStart = false;
                break;
        }
    }

    private void PhotonSendGameStateInfo(GameState gs)
    {
        var edw = new EventDataWrapper();
        edw.AddObject(cParameterCode.GameState, (int)gs);
        edw.ToSend(cEventCode.ChangeGameState);
    }

    private void PhotonCatchGameState(EventDataWrapper edw)
    {
        int state = edw.ReadInt(cParameterCode.GameState);
        SetGameState((GameState)state);
    }
}
