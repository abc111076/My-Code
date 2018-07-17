using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public enum GameState
{
    None,
    GameReady,
    GameStart,
    GamePlaying,
    GameFinish
}

public class GameManager : MonoBehaviour, IGameSystemPhotonMethod
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

    [SerializeField] private int gameTotalTime;
    [SerializeField] private GameObject circularCamera;
    [SerializeField] private AudioSource gameFinishAudio;

    private GameState currentState = GameState.None;
    private UIRootInGame uiRootInGame;
    private int gameCurrentTime;

    public bool IsPlaying { get { return currentState == GameState.GamePlaying; } }

    private void Awake()
    {
#if CONNECTION //<--------判斷是單機模式還是連線模式
        PhotonNetwork.OnEventCall += OnEventHandler;
#endif
    }

    private void Start()
    {
        Init();
    }

    private void OnApplicationQuit()
    {
        BikeAPI.SportData.QuitApp();
    }

    /// <summary>
    /// 初始化參數，並把GameState轉為GameReady
    /// </summary>
    private void Init()
    {
        EventManager.ChangeGameStateHandler += ChangeState;
        uiRootInGame = FindObjectOfType<UIRootInGame>();
        gameCurrentTime = gameTotalTime;
        circularCamera.SetActive(false);

#if UNITY_EDITOR
        ChangeState(GameState.GameReady);
#endif
    }

    /// <summary>
    /// 改變GameState的狀態
    /// </summary>
    /// <param name="gs"></param>
    private void ChangeState(GameState gs)
    {
        if (currentState == gs)
            return;

        if (currentState == GameState.None)
        {
            if (!circularCamera.activeInHierarchy)
                circularCamera.SetActive(true);
        }

        currentState = gs;

#if CONNECTION
        PhotonSendGameStateInfo(gs);
#endif

        EventManager.EventHappen(EventManagerCode.GetGameStateEvent, gs);

        switch (gs)
        {
            case GameState.GameStart:
                DoGameStart();
                break;

            case GameState.GamePlaying:

                if (PUNConnect.IsMasterClient(PhotonNetwork.AuthValues.UserId))
                    StartCoroutine(DoGamePlaying());

                break;

            case GameState.GameFinish:
                DoGameFinish();
                break;
        }
    }

    /// <summary>
    /// 當GameState狀態變更為GameStart時執行
    /// </summary>
    private void DoGameStart()
    {
        if (circularCamera.activeInHierarchy)
            circularCamera.SetActive(false);

        uiRootInGame.ShowReadyGo();

#if UNITY_EDITOR
#elif UNITY_STANDALONE
        BikeAPI.SportData.StartSport();
#endif
    }

    /// <summary>
    /// 遊戲中進行遊戲時間倒數的功能
    /// </summary>
    /// <returns></returns>
    private IEnumerator DoGamePlaying()
    {
        while (gameCurrentTime >= 0)
        {
#if CONNECTION
            PhotonSendGameTimeInfo(gameCurrentTime);
#endif
            uiRootInGame.UpdateUIInfo(UIInformation.GameTime, gameCurrentTime);
            gameCurrentTime--;
            yield return new WaitForSeconds(1);
        }

        ChangeState(GameState.GameFinish);
    }

    /// <summary>
    /// 當GameState狀態變更為GameFinish時執行
    /// </summary>
    private void DoGameFinish()
    {
        gameFinishAudio.Play();
        gameCurrentTime = gameTotalTime;
    }

    /*****************以下為網路功能********************/

    /// <summary>
    /// 收到網路Event封包時判斷要做什麼
    /// </summary>
    /// <param name="eventCode"></param>
    /// <param name="content"></param>
    /// <param name="senderId"></param>
    private void OnEventHandler(byte eventCode, object content, int senderId)
    {
        EventDataWrapper edw = new EventDataWrapper(content);

        switch (eventCode)
        {
            case cEventCode.ChangeGameState:
                PhotonCatchGameStateInfo(edw);
                break;

            case cEventCode.GameTime:
                PhotonCatchGameTimeInfo(edw);
                break;
        }
    }

    /// <summary>
    /// 送出變更GameState的訊息給其他玩家
    /// </summary>
    /// <param name="gs"></param>
    public void PhotonSendGameStateInfo(GameState gs)
    {
        var edw = new EventDataWrapper();
        edw.AddObject(cParameterCode.GameState, gs);
        edw.ToSend(cEventCode.ChangeGameState);
    }

    /// <summary>
    /// 接收從其他玩家傳來的變更GameState的訊息
    /// </summary>
    /// <param name="edw"></param>
    public void PhotonCatchGameStateInfo(EventDataWrapper edw)
    {
        int game_state = edw.ReadInt(cParameterCode.GameState);
        currentState = (GameState)game_state;
        EventManager.EventHappen(EventManagerCode.GetGameStateEvent, currentState);

        switch (currentState)
        {
            case GameState.GameStart:
                DoGameStart();
                break;

            case GameState.GamePlaying:

                if (PUNConnect.IsMasterClient(PhotonNetwork.AuthValues.UserId))
                    StartCoroutine(DoGamePlaying());

                break;

            case GameState.GameFinish:
                DoGameFinish();
                break;
        }
    }

    /// <summary>
    /// 同步所有玩家的遊戲時間(送出封包)
    /// </summary>
    /// <param name="time"></param>
    public void PhotonSendGameTimeInfo(int time)
    {
        var edw = new EventDataWrapper();
        edw.AddObject(cParameterCode.GameTime, time);
        edw.ToSend(cEventCode.GameTime);
    }

    /// <summary>
    /// 同步所有玩家的遊戲時間(接收封包)
    /// </summary>
    /// <param name="time"></param>
    public void PhotonCatchGameTimeInfo(EventDataWrapper edw)
    {
        int time = edw.ReadInt(cParameterCode.GameTime);
        uiRootInGame.UpdateUIInfo(UIInformation.GameTime, time);
    }
}