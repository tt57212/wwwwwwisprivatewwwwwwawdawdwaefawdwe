using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewMonoBehaviourScript : MonoBehaviour
{
    public int scene = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    public void OnbuttonClick()
    {
       SceneManager.LoadScene(scene);
    }
    // Update is called once per frame
    void aaaa()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("Scene 1", LoadSceneMode.Single);
    }
}
