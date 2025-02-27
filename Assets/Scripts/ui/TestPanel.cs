using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestPanel : MonoBehaviour
{
    private Text txt_target;
    private Text txt_response;
    private Text txt_curTask;
    private GameObject tip_wait;
    private GameObject btn_start;

    private InputField inputField;

    void Start()
    {
        txt_target = CTool.Find<Text>(gameObject, "txt_target");
        txt_response = CTool.Find<Text>(gameObject, "txt_response");
        txt_curTask = CTool.Find<Text>(gameObject, "txt_curTask");
        tip_wait = CTool.Find(gameObject, "tip_wait");
        btn_start = CTool.Click(gameObject, "btn_start", ClickStart);
        inputField = CTool.Find<InputField>(gameObject, "inputField");
        inputField.onEndEdit.AddListener(OnInputFieldEndEdit);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetTargetShow(string target)
    {
        txt_target.text = "目标：" + target;
    }

    public void SetResponseShow(string response)
    {
        txt_response.text = "回复：" + response;
    }

    public void SetCurTaskShow(string curTask)
    {
        txt_curTask.text = "当前动作：" + curTask;
    }

    public void SetWaitShow(bool isWait)
    {
        tip_wait.SetActive(isWait);
    }

    public void ClickStart()
    {
        GameManager.Instance.deepSeekAPI.SendMessageToDeepSeek("", GameManager.Instance.roleManager.GetReturnData);
        btn_start.SetActive(false);
        // GameManager.Instance.roleManager.TestA();
    }

    private void OnInputFieldEndEdit(string input)
    {
        // 当检测到回车时才会触发（键盘的Enter键或手机的Done按钮）
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            Debug.Log($"用户输入内容：{input}");
            // 在这里处理输入内容：
            GameManager.Instance.roleManager.SubmitUserContent(input);
            inputField.text = ""; // 清空输入框
        }
    }
}
