using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    public AudioClip itemPickupClip;
    public int lifeRemains = 3;
    private AudioSource playerAudioPlayer;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerShooter playerShooter;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();
        playerHealth = GetComponent<PlayerHealth>();
        playerAudioPlayer = GetComponent<AudioSource>();

        playerHealth.OnDeath += HandleDeath;

        UIManager.Instance.UpdateLifeText(lifeRemains);
        Cursor.visible = false;
    }
    
    private void HandleDeath()
    {
        playerMovement.enabled = false;
        playerShooter.enabled = false;

        if(lifeRemains > 0) {
            lifeRemains--;
            UIManager.Instance.UpdateLifeText(lifeRemains);

            Invoke("Respawn", 3f);
        } else {
            GameManager.Instance.EndGame();
        }

        Cursor.visible = true;
    }

    public void Respawn()
    {
        gameObject.SetActive(false);

        transform.position = Utility.GetRandomPointOnNavMesh(transform.position, 30f, NavMesh.AllAreas);

        playerMovement.enabled = true;
        playerShooter.enabled = true;

        gameObject.SetActive(true);

        playerShooter.gun.ammoRemain = 100;

        Cursor.visible = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(playerHealth.dead) {
            return;
        }

        var item = other.GetComponent<IItem>();

        if(item != null) {
            item.Use(gameObject);
            playerAudioPlayer.PlayOneShot(itemPickupClip);
        }
    }
}