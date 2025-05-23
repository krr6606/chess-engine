using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

public abstract class AIPlayer : BasePlayer
{

    protected ChessGameState currentState;
    [SerializeField] protected float thinkTime = 1.0f;
    public float ThinkTime => thinkTime;
    protected float lastMoveTime;
    protected bool isThinking = false;
    public bool IsThinking => isThinking;

    // 비동기 연산 관리를 위한 변수
    protected CancellationTokenSource cancelTokenSource;
    protected Task aiTask;
    protected Move selectedMove;
    protected bool moveReady = false;
    readonly public Guid guid;
    // 스레드 안전한 랜덤 생성기
    protected System.Random random = new System.Random();
    
    // 체스 상태 매니저 추가
    protected ChessStateCalculator stateManager;

    public override void Initialize(BoardManager manager)
    {
        base.Initialize(manager);

        if (guid == default(Guid))
        {
            Guid guid = Guid.NewGuid();
        }
        playerName = "AI Player" + guid;
        
        // 랜덤 생성기 초기화
        random = new System.Random(Environment.TickCount);
        
        // 체스 상태 매니저 초기화
        stateManager = new ChessStateCalculator();

    }

    public override void OnTurnStarted()
    {
        isMyTurn = true;
        Debug.Log($"{playerName}의 턴 시작");
        
        currentState = chessManager.GetCurrentState();
        
        // 이미 AI 턴 진행 중인지 확인
        if (isThinking)
        {
            Debug.LogWarning("중복 턴 시작 탐지됨: 이미 AI가 생각 중입니다.");
            return;
        }
        Debug.Log($"{playerName}의 이동 계산 시작");

        StartThinking();
        Debug.Log($"{playerName}의 이동 계산 끝?");
    }

    public override void OnMoveExecuted(Move move)
    {
        base.OnMoveExecuted(move);
        Debug.Log($"{playerName}의 이동 완료");
        
        // 생각 상태 초기화
        isThinking = false;
        
        // 비동기 작업 취소
        CancelThinking();

        // AI 턴 종료를 ChessManager에 알림
        chessManager.NotifyAITurnEnded(move);
    }

    protected virtual void StartThinking()
    {
        isThinking = true;
        lastMoveTime = Time.time;
        moveReady = false;
        
        // 새로운 취소 토큰 생성
        cancelTokenSource = new CancellationTokenSource();
        
        // 비동기적으로 AI 이동 계산 시작
        aiTask = CalculateMoveAsync(cancelTokenSource.Token);
        
    }
    
    protected void CancelThinking()
    {
        if (cancelTokenSource != null)
        {
            cancelTokenSource.Cancel();
            cancelTokenSource.Dispose();
            cancelTokenSource = null;
        }
    }
    private void Start()
    {
        
    }
    public override void Update()
    {
        base.Update();
       
        // 생각 중이 아니면 처리하지 않음
        if (!isThinking) return;
        
        // 생각 시간이 지났는지 확인
        if ((Time.time + thinkTime) - (lastMoveTime + thinkTime) >= thinkTime || moveReady)
        {
            // 이동이 준비되었으면 실행
            if (moveReady && selectedMove.FromSquare != 0)
            {
                // 이동 유효성 최종 검사
                ValidateAndExecuteMove();
            }
            // 아직 이동이 준비되지 않았지만 시간이 다 되었으면 강제로 이동 결정
            else if ((Time.time + thinkTime) - (lastMoveTime + thinkTime) >= thinkTime) 
            {
                Debug.Log($"지난 시간: {Time.time} 설정된 마지막 시간:{lastMoveTime} isThinking: {isThinking}"); 
                ForceMoveSelection();
            }

        }
    }
    
    // 유효성 검사 후 이동 실행
    protected virtual void ValidateAndExecuteMove()
    {
        if (selectedMove.FromSquare == selectedMove.ToSquare) return;

        // 현재 게임 상태에서 이동이 유효한지 확인
        var currentLegalMoves = GetLegalMoves();
        bool isValidMove = false;

        foreach (var move in currentLegalMoves)
        {
            if (move.FromSquare == selectedMove.FromSquare && move.ToSquare == selectedMove.ToSquare)
            {
                isValidMove = true;
                selectedMove = move; // 정확한 플래그 값 사용
                break;
            }
        }

        if (!isValidMove)
        {
            Debug.LogWarning($"AI가 유효하지 않은 이동을 시도했습니다: {selectedMove}");

            // 대체 이동 선택
            if (currentLegalMoves.Count > 0)
            {
                selectedMove = currentLegalMoves[UnityEngine.Random.Range(0, currentLegalMoves.Count)];
                Debug.Log($"대체 이동 선택: {selectedMove}");
            }
            else
            {
                // 유효한 이동이 없으면 턴 종료
                isThinking = false;
                return;
            }
        }

        // 이동 큐에 추가
        Debug.Log($"AI가 이동을 실행합니다: {selectedMove}");
        ExecuteMove(selectedMove);
    }

    // 강제로 이동 선택 (시간 초과시)
    public virtual void ForceMoveSelection()  // public으로 변경하여 외부에서 호출 가능하게
    {
        Debug.Log($"{playerName}의 이동 강제 선택");

        // 아직 생각 중이라면 취소
        CancelThinking();

        // 현재까지의 최선의 이동 실행(기본 랜덤)
        var legalMoves = GetLegalMoves();
        if (legalMoves.Count > 0)
        {
            int randomIndex = random.Next(0, legalMoves.Count);
            selectedMove = legalMoves[randomIndex];
            moveReady = true;

            // 이동 처리 로직 진입
            ExecuteMove(selectedMove);

            isThinking = false;
        }
        else
        {
            Debug.LogError($"{playerName}에게 합법적인 이동이 없습니다!");
        }
    }
    // 비동기적으로 이동 계산
    protected virtual async Task CalculateMoveAsync(CancellationToken cancelToken)
    {
        await Task.Run(() => {
            try
            {
                // Unity 관련 작업은 백그라운드 스레드에서 수행하지 않음
                ChessGameState threadSafeState = null;
                
                // 게임 상태 복제는 미리 메인 스레드에서 수행
                threadSafeState = currentState.Clone();
                
                // 이동 계산 (하위 클래스에서 구현)
                // 스레드 안전한 상태 사용
                CalculateMoveWithState(threadSafeState, cancelToken);
                
                // 이동 계산이 완료되면 메인 스레드에서 처리할 수 있도록 플래그 설정
                if (!cancelToken.IsCancellationRequested)
                {
                    Debug.Log($"AI 이동 계산 완료: {selectedMove}");

                    moveReady = true;
                }
            }
            catch (OperationCanceledException)
            {
                // 작업이 취소된 경우
                Debug.Log("AI 이동 계산이 취소되었습니다.");
            }
            catch (Exception e)
            {
                // 기타 예외 처리
                Debug.LogError($"AI 이동 계산 중 오류 발생: {e.Message}");
            }
        }, cancelToken);
    }
    
    // 스레드 안전한 상태를 사용하는 계산 메서드
    protected virtual void CalculateMoveWithState(ChessGameState threadSafeState, CancellationToken cancelToken)
    {
        // 기본 구현에서는 기존 CalculateMove 호출
        CalculateMove(cancelToken);
    }

    // 별도 스레드에서 실행될 이동 계산 메서드 (하위 클래스에서 구현)
    protected virtual void CalculateMove(CancellationToken cancelToken)
    {
        // 기본 구현은 아무것도 하지 않음 (하위 클래스에서 구현)
    }

    // 항상 ChessManager를 통해 합법적인 이동 얻기
    protected List<Move> GetLegalMoves()
    {
        return chessManager.GetLegalMoves();
    }
    
    // 체스 상태 매니저를 통해 합법적인 이동 얻기
    protected List<Move> GetLegalMovesFromState(ChessGameState state)
    {
        return stateManager.GenerateLegalMoves(state);
    }

    // 이동 검증 후 실행
    protected void ExecuteMove(Move move)
    {
        if (chessManager.IsValidMove(move))
        {
            OnMoveExecuted(move);
        }
        else
        {
            Debug.LogWarning($"AI가 유효하지 않은 이동을 시도했습니다: {move}");
            // 대체 이동 선택
            var legalMoves = GetLegalMoves();
            if (legalMoves.Count > 0)
            {
                int randomIndex = random.Next(0, legalMoves.Count);
                OnMoveExecuted(move);

            }
        }
    }
    
    // 현재 게임 상태로부터 AI 평가를 위한 임시 상태 생성
    protected ChessGameState GetEvaluationState()
    {
        return chessManager.GetCurrentState();
    }
    
    // 임시 상태에 이동 적용 (평가용)
    protected virtual void ApplyMoveToState(ChessGameState state, Move move)
    {
        if (state == null) return;
        
        // 체스 상태 매니저를 통해 이동 적용
        stateManager.ApplyMoveToState(state, move);
    }

    public override void OnGameEnded()
    {
        base.OnGameEnded();
        Debug.Log($"{playerName}에게 게임 종료 알림");
        
        // 생각 중이었다면 중단
        isThinking = false;
        
        // 비동기 작업 취소
        CancelThinking();
        
        // AI 턴이 활성화 되어있었다면 상태 초기화
        if (isMyTurn)
        {
            isMyTurn = false;

        }
    }
    
    private void OnDisable()
    {
        // 컴포넌트가 비활성화될 때 비동기 작업 취소
        CancelThinking();
    }
    
    private void OnDestroy()
    {
        // 컴포넌트가 파괴될 때 비동기 작업 취소
        CancelThinking();
    }
    
    // 디버그 모드 활성화/비활성화 (하위 클래스에서 필요한 경우 오버라이드)
    public virtual void SetDebugMode(bool enabled)
    {
        // 기본 구현은 아무 작업도 수행하지 않음
    }
    
    // 난이도 설정 (하위 클래스에서 필요한 경우 오버라이드)
    public virtual void SetDifficulty(int level)
    {
        // 기본 구현은 아무 작업도 수행하지 않음
    }
} 