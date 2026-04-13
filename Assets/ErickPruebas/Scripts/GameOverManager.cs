using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager instance;

    public GameObject gameOverPanel;
    public AudioSource screamAudio;

    private bool isGameOver = false;

    void Awake()
    {
        instance = this;
    }

    public void TriggerGameOver()
    {
        if (isGameOver) return;

        isGameOver = true;

        // sonido
        if (screamAudio != null)
            screamAudio.Play();

        // mostrar pantalla
        gameOverPanel.SetActive(true);

        // desbloquear cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // detener juego
        Time.timeScale = 0f;
    }

    // ESTA ES LA FUNCIÓN DEL BOTÓN
    public void RestartGame()
    {
        Time.timeScale = 1f;

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}