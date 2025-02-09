using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RoleBase : MonoBehaviour
{

    private List<Task> curTasks = new();

    private int curTaskIndex = 0;
    private Task CurTask { get => curTasks[curTaskIndex]; }

    private float moveSpeed = 5;
    private float turnSpeed = 200;
    private bool taskIng = false;
    private Vector3 moveTarget;

    private float actionTime = 3;
    private float curXctionTime = 0;

    void Start()
    {
        // curTasks = new();
        // curTaskIndex = 0;

    }

    void Update()
    {
        if (taskIng && CurTask != null)
        {
            if (CurTask.action == "移动")
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
            else if (CurTask.action == "找到物品")
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
                    }
                    else
                    {
                        // 移动到目标位置
                        transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                    }
                    // // 移动到目标位置
                    // transform.Translate(direction * moveSpeed * Time.deltaTime);
                }
                if (Vector3.Distance(transform.position, moveTarget) < 0.1f)
                {
                    // 到达目标点1后的逻辑
                    NextTask();
                }
            }
            else if (CurTask.action == "交互")
            {

            }
        }
    }

    public void PerformTask(List<Task> tasks)
    {
        curTasks = tasks.ToList();
        curTaskIndex = 0;
        taskIng = true;
        PerformAction(CurTask);
    }

    private void PerformAction(Task task)
    {
        var target = GameManager.Instance.roleManager.items.Find(e => e.ItemId == task.itemId);
        Debug.Log("执行任务：" + task.action + "--目标： " + task.itemId + "--动作： " + task.interaction);

        if (task.action == "移动")
        {

        }
        else if (task.action == "找到物品")
        {
            if (target != null)
            {
                moveTarget = new Vector3(target.rolePos.transform.position.x, 0, target.rolePos.transform.position.z);

                UIManager.Instance.testPanel.SetCurTaskShow("找到物品: " + target.ItemName);
            }
        }
        else if (task.action == "交互")
        {
            if (target != null)
            {
                target.RoleAction(task.interaction, this);
                UIManager.Instance.testPanel.SetCurTaskShow("交互 目标: " + target.ItemName + " 动作: " + task.interaction);
            }
        }

    }

    public void NextTask()
    {
        curTaskIndex++;
        if (curTaskIndex >= curTasks.Count)
        {
            taskIng = false;
            UIManager.Instance.testPanel.SetCurTaskShow("无");
            Debug.Log("任务全部完成");
            return;
        }
        PerformAction(CurTask);
    }
}
