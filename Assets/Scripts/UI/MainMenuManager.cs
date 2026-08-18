using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class MainMenuManager : MonoBehaviour
{
    private string Settings = "SettingsMenu";
    // private PlayerController controller;
    public GameObject mainMenuUi;
    public GameObject SettingBackButton;
    public GameObject quitConfirm;
    //public GameObject creditMenu;

    private void Start()
    {
        Debug.Log("MainMenu script initialized");
    }
    public void PlayGame(string scenename)
    {
        SceneManager.LoadScene(scenename);
    }

    public void Level1()
    {
        PlayGame("Level");
    }
    public void Level2()
    {
        PlayGame("Level2");
    }
    public void Level3()
    {
        PlayGame("Level3");
    }

    public void QuitConfirm()
    {
        quitConfirm.SetActive(true);
    }
    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    public void MainMenuLoad()
    {
        mainMenuUi.SetActive(true);
    }

    public void SettingsLoad()
    {

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1, LoadSceneMode.Additive);
    }

    public void SettingsClose()
    {
        SceneManager.UnloadSceneAsync(Settings);
    }

    //public void SettingBack()
    //{
    //    SettingBackButton.SetActive(true);
   // }

   // public void CreditMenu()
   // {
     //   creditMenu.SetActive(true);
    //}
}
