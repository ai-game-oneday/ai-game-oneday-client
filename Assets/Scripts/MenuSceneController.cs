using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSceneController : MonoBehaviour
{
  public void MoveMenuScene()
  {
    AudioManager.I.StopAllSFXSequences();
    AudioManager.I.StopAllSFX();

    SceneManager.LoadScene(0);
  }

  public void MoveScene1()
  {
    AudioManager.I.StopAllSFXSequences();
    AudioManager.I.StopAllSFX();

    SceneManager.LoadScene(1);
  }

  public void MoveScene2()
  {
    AudioManager.I.StopAllSFXSequences();
    AudioManager.I.StopAllSFX();

    SceneManager.LoadScene(2);
  }
}
