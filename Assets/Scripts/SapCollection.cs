using UnityEngine;

public class SapCollection : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
        {
            gameObject.AddComponent<GoToLevel>().LoadScene("MainMenu");
        }
    }
}
