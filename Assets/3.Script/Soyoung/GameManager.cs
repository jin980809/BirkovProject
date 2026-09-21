using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject MenuPanel;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool isActive = !MenuPanel.activeSelf;
            MenuPanel.SetActive(isActive);
            if (isActive)
            {
                Time.timeScale = 0f;
            }
            else 
            {
                Time.timeScale = 1f;
            }
        }
    }

    public void GameExit()
    {
        Application.Quit();
    }
}
