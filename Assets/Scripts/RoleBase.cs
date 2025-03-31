using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RoleBase : MonoBehaviour
{
    private Animator animator;

    private List<Task> curTasks = new();

    private int curTaskIndex = 0;

    private float moveSpeed = 1;
    private float turnSpeed = 200;
    private bool taskIng = false;
    private Vector3 moveTarget;

    // private float actionTime = 3;
    // private float curXctionTime = 0;

    void Start()
    {
        animator = GetComponent<Animator>();
        // curTasks = new();
        // curTaskIndex = 0;

    }

    void Update()
    {
        if (taskIng && GetCurTask() != null)
        {
            if (GetCurTask().action == "移动")
            {
                // 移动逻辑
                // // 到达目标点1后的逻辑
                // curTaskIndex++;
                // if (curTaskIndex >= curTasks.Count)
                // {
                //     taskIng = false;
                //     return;
                // }
                // PerformAction(CurTask);
            }
            else if (GetCurTask().action == "找到物品")
            {
                if (moveTarget != null)
                {
                    // 计算当前位置到目标位置的方向
                    Vector3 direction = (moveTarget - transform.position).normalized;
                    // // 先转向目标位置
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    if (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
                    {
                        // transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                        animator.SetBool("IsWalking", false); // 转向时不播放行走动画
                    }
                    else
                    {
                        // 移动时播放行走动画
                        animator.SetBool("IsWalking", true);
                        // 移动到目标位置

                        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                    }
                    // // 移动到目标位置
                    // transform.Translate(direction * moveSpeed * Time.deltaTime);
                }
                if (Vector3.Distance(transform.position, moveTarget) < 0.1f)
                {
                    // 到达目标后停止动画
                    animator.SetBool("IsWalking", false);
                    // 到达目标点1后的逻辑
                    NextTask();
                }
            }
            else if (GetCurTask().action == "交互")
            {
                // 交互时停止动画
                animator.SetBool("IsWalking", false);
            }
        }
    }

    private Task GetCurTask()
    {
        if (curTasks == null || curTasks.Count == 0)
        {
            return null;
        }
        return curTasks[curTaskIndex];
    }


    public void PerformTask(List<Task> tasks)
    {
        curTasks = tasks.ToList();
        curTaskIndex = 0;
        taskIng = true;
        PerformAction(GetCurTask());
    }

    private void PerformAction(Task task)
    {
        if (task == null || task.action == "" || task.action == "None")
        {
            Debug.Log("任务为空");
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

        Debug.Log("执行任务：" + task.action + "--目标： " + task.itemId + "--动作： " + task.interaction);

        if (task.action == "移动")
        {
            // TODO: 实现移动逻辑
            Debug.Log("移动动作待实现");
            NextTask();
        }
        else if (task.action == "找到物品")
        {
            moveTarget = new Vector3(target.rolePos.transform.position.x, 0, target.rolePos.transform.position.z);
            UIManager.Instance.testPanel.SetCurTaskShow("找到物品: " + target.ItemName + ":" + target.ItemId);
        }
        else if (task.action == "交互")
        {
            target.RoleAction(task.interaction, this, NextTask);
            UIManager.Instance.testPanel.SetCurTaskShow("交互 目标: " + target.ItemName + " 动作: " + task.interaction);
        }
        // 输出剩余任务列表
        string remainingTasks = "剩余任务列表:\n";
        for (int i = curTaskIndex; i < curTasks.Count; i++)
        {
            remainingTasks += $"任务{i + 1}: {curTasks[i].action} - 目标ID: {curTasks[i].itemId} - 交互: {curTasks[i].interaction}\n";
        }
        Debug.Log(remainingTasks);
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
        animator.SetBool("IsWalking", false);
        UIManager.Instance.testPanel.SetCurTaskShow("无");
        GameManager.Instance.roleManager.OnTaskFinish();
        Debug.Log("任务全部完成");
    }
}
