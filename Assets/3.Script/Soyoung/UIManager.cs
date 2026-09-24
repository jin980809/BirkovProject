using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI ÇÁ¸®Æé")]
    [SerializeField] private GameObject optionPrefab;

    private GameObject currentOptionUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else { Destroy(gameObject); }
    }

    public void OpenOptionMenu()
    {
        if (currentOptionUI != null)
        {
            return;
        }
            currentOptionUI = Instantiate(optionPrefab);
    }

}
