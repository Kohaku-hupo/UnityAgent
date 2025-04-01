using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using System;

public class RoleBase : MonoBehaviour
{
    [Header("基础配置")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private float arriveThreshold = 0.1f;
    
    [Header("动画参数")]
    [SerializeField] private string walkParamName = "IsWalking";
    [SerializeField] private string interactParamName = "IsInteracting";
    [SerializeField] private string idleParamName = "IsIdle";

    // 添加动画效果触发器参数名
    [Header("效果触发器参数")]
    [SerializeField] private string sitTriggerName = "Sit";
    [SerializeField] private string standTriggerName = "Stand";
    [SerializeField] private string watchTriggerName = "Watch";
    [SerializeField] private string stopWatchTriggerName = "StopWatch";

    private Animator animator;
    private List<GameTask> curTasks = new();
    private int curTaskIndex = 0;
    private bool taskIng = false;
    private Vector3 moveTarget;
    private bool isMoving = false;
    private bool isInteracting = false;
    private float interactionTimer = 0f;
    private float interactionDuration = 2f;

    // 角色状态
    private enum RoleState
    {
        Idle,
        Moving,
        Interacting,
        Thinking
    }
    private RoleState currentState = RoleState.Idle;

    // 标记任务是否已完成但正在等待动画完成
    private bool isTaskCompletionPending = false;
    private GameTask pendingTask = null;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("未找到Animator组件！");
        }
        InitializeState();
    }

    void Update()
    {
        if (!taskIng || GetCurTask() == null)
        {
            UpdateIdleState();
            return;
        }

        switch (GetCurTask().action)
        {
            case "移动":
                UpdateMoveState();
                break;
            case "找到物品":
                UpdateFindItemState();
                break;
            case "交互":
                UpdateInteractState();
                break;
        }
    }

    private void InitializeState()
    {
        currentState = RoleState.Idle;
        UpdateAnimationState();
    }

    private void UpdateIdleState()
    {
        if (currentState != RoleState.Idle)
        {
            currentState = RoleState.Idle;
            UpdateAnimationState();
        }
    }

    private void UpdateMoveState()
    {
        if (currentState != RoleState.Moving)
        {
            currentState = RoleState.Moving;
            UpdateAnimationState();
            Debug.Log($"开始移动到: {moveTarget}");
        }

        if (moveTarget != null)
        {
            Vector3 direction = (moveTarget - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            if (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                SetWalking(false);
            }
            else
            {
                SetWalking(true);
                transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            }

            if (Vector3.Distance(transform.position, moveTarget) < arriveThreshold)
            {
                SetWalking(false);
                Debug.Log($"已到达目标位置: {moveTarget}");
                
                // 如果有待处理的任务，即此移动是为了交互
                if (isTaskCompletionPending && pendingTask != null)
                {
                    isTaskCompletionPending = false;
                    var task = pendingTask;
                    pendingTask = null;
                    
                    Debug.Log($"到达位置后开始执行交互: {task.itemId} - {task.interaction}");
                    // 完成交互任务
                    CompleteInteractionTask(task);
                }
                else
                {
                    // 正常的移动任务完成
                    Debug.Log("移动任务完成，准备执行下一个任务");
                    NextTask();
                }
            }
        }
    }

    private void UpdateFindItemState()
    {
        // 与移动状态相同的逻辑
        UpdateMoveState();
    }

    private void UpdateInteractState()
    {
        if (currentState != RoleState.Interacting)
        {
            currentState = RoleState.Interacting;
            UpdateAnimationState();
        }
        
        // 注意：交互完成后的NextTask调用现在由WaitForInteractionComplete协程处理
        // 这里只需保持动画状态即可
    }

    private void UpdateAnimationState()
    {
        SetWalking(currentState == RoleState.Moving);
        SetInteracting(currentState == RoleState.Interacting);
        SetIdle(currentState == RoleState.Idle);
    }

    private void SetWalking(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(walkParamName, value);
        }
    }

    private void SetInteracting(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(interactParamName, value);
        }
    }

    private void SetIdle(bool value)
    {
        if (animator != null)
        {
            animator.SetBool(idleParamName, value);
        }
    }

    private GameTask GetCurTask()
    {
        if (curTasks == null || curTasks.Count == 0)
        {
            return null;
        }
        return curTasks[curTaskIndex];
    }

    public void PerformTask(List<GameTask> tasks)
    {
        curTasks = tasks.ToList();
        curTaskIndex = 0;
        taskIng = true;
        currentState = RoleState.Thinking;
        UpdateAnimationState();
        PerformAction(GetCurTask());
    }

    private void PerformAction(GameTask task)
    {
        try
        {
            if (task == null)
            {
                Debug.LogError("任务对象为空");
                FinishTask();
                return;
            }

            if (string.IsNullOrEmpty(task.action))
            {
                Debug.LogError("任务动作为空");
                FinishTask();
                return;
            }

            if (string.IsNullOrEmpty(task.itemId))
            {
                Debug.LogError("任务物品ID为空");
                FinishTask();
                return;
            }

            var target = GameManager.Instance.roleManager.items.Find(e => e.ItemId == task.itemId);
            if (target == null)
            {
                Debug.LogError($"找不到目标物品: {task.itemId}");
                FinishTask();
                return;
            }

            Debug.Log($"执行任务：{task.action} -- 目标：{task.itemId}({target.ItemName}) -- 动作：{task.interaction}");

            switch (task.action)
            {
                case "移动":
                    if (target.rolePos == null)
                    {
                        Debug.LogError($"物品 {target.ItemName} 的rolePos为空");
                        FinishTask();
                        return;
                    }
                    moveTarget = new Vector3(target.rolePos.transform.position.x, 0, target.rolePos.transform.position.z);
                    UIManager.Instance.testPanel.SetCurTaskShow($"移动到: {target.ItemName}");
                    // 不调用NextTask()，让UpdateMoveState在到达目标后调用
                    break;

                case "找到物品":
                    if (target.rolePos == null)
                    {
                        Debug.LogError($"物品 {target.ItemName} 的rolePos为空");
                        FinishTask();
                        return;
                    }
                    moveTarget = new Vector3(target.rolePos.transform.position.x, 0, target.rolePos.transform.position.z);
                    UIManager.Instance.testPanel.SetCurTaskShow($"找到物品: {target.ItemName}");
                    // 不调用NextTask()，让UpdateFindItemState在到达目标后调用
                    break;

                case "交互":
                    if (string.IsNullOrEmpty(task.interaction))
                    {
                        Debug.LogError($"与物品 {target.ItemName} 的交互动作为空");
                        FinishTask();
                        return;
                    }

                    if (target.rolePos == null)
                    {
                        Debug.LogError($"物品 {target.ItemName} 的rolePos为空");
                        FinishTask();
                        return;
                    }

                    if (Vector3.Distance(transform.position, target.rolePos.transform.position) <= interactionDistance)
                    {
                        Debug.Log($"开始与物品 {target.ItemName} 执行 {task.interaction} 交互");
                        CompleteInteractionTask(task);
                    }
                    else
                    {
                        Debug.Log($"距离 {target.ItemName} 太远，需要先移动到位置");
                        moveTarget = new Vector3(target.rolePos.transform.position.x, 0, target.rolePos.transform.position.z);
                        UIManager.Instance.testPanel.SetCurTaskShow($"移动到: {target.ItemName} 进行交互");
                        
                        // 标记此移动是为了交互，并保存待处理的任务
                        isTaskCompletionPending = true;
                        pendingTask = task;
                    }
                    break;

                default:
                    Debug.LogError($"未知的任务动作: {task.action}");
                    FinishTask();
                    break;
            }

            // 输出剩余任务列表
            string remainingTasks = "剩余任务列表:\n";
            for (int i = curTaskIndex; i < curTasks.Count; i++)
            {
                var t = curTasks[i];
                if (t != null)
                {
                    remainingTasks += $"任务{i + 1}: {t.action} - 目标ID: {t.itemId} - 交互: {t.interaction}\n";
                }
                else
                {
                    remainingTasks += $"任务{i + 1}: 空任务\n";
                }
            }
            Debug.Log(remainingTasks);
        }
        catch (Exception e)
        {
            Debug.LogError($"执行任务时出错: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
            FinishTask();
        }
    }

    // 新增方法，专门处理交互任务的完成
    private void CompleteInteractionTask(GameTask task)
    {
        var target = GameManager.Instance.roleManager.items.Find(e => e.ItemId == task.itemId);
        if (target != null)
        {
            // 设置交互进行中状态
            currentState = RoleState.Interacting;
            UpdateAnimationState();
            
            // 显示当前任务状态
            UIManager.Instance.testPanel.SetCurTaskShow($"交互: {target.ItemName} - {task.interaction}");
            
            // 创建一个标志，表示交互正在进行
            bool interactionCompleted = false;
            
            // 执行物品交互，但使用自定义回调而不是直接调用NextTask
            target.RoleAction(task.interaction, this, () => {
                interactionCompleted = true;
            });
            
            // 启动协程等待交互完成
            StartCoroutine(WaitForInteractionComplete(() => interactionCompleted, NextTask));
        }
        else
        {
            Debug.LogError($"执行交互任务时找不到目标物品: {task.itemId}");
            NextTask();
        }
    }

    // 等待交互完成的协程
    private IEnumerator WaitForInteractionComplete(Func<bool> isCompleted, Action onComplete)
    {
        // 等待交互完成标志
        while (!isCompleted())
        {
            yield return null;
        }
        
        // 交互完成后，等待一小段时间确保动画和效果完成
        yield return new WaitForSeconds(0.5f);
        
        // 重置状态并调用完成回调
        currentState = RoleState.Idle;
        UpdateAnimationState();
        
        Debug.Log("交互已完成，准备执行下一个任务");
        onComplete?.Invoke();
    }

    public void NextTask()
    {
        curTaskIndex++;
        if (curTaskIndex >= curTasks.Count)
        {
            FinishTask();
            return;
        }
        PerformAction(GetCurTask());
    }

    private void FinishTask()
    {
        taskIng = false;
        currentState = RoleState.Idle;
        UpdateAnimationState();
        UIManager.Instance.testPanel.SetCurTaskShow("无");
        GameManager.Instance.roleManager.OnTaskFinish();
        Debug.Log("任务全部完成");
    }

    public void PlayEffect(string effectName)
    {
        if (animator == null) return;

        switch (effectName.ToLower())
        {
            case "sit":
                animator.SetTrigger(sitTriggerName);
                break;
            case "stand":
                animator.SetTrigger(standTriggerName);
                break;
            case "watch":
                animator.SetTrigger(watchTriggerName);
                break;
            case "stopwatch":
                animator.SetTrigger(stopWatchTriggerName);
                break;
            default:
                Debug.LogWarning($"未知的效果名称: {effectName}");
                break;
        }
    }
}
