using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float speedIncrease;
    [SerializeField] private AudioClip bounceSound;
    Vector2 Velocity;
    Vector2 direction;

    public LayerMask collisionMask;
    public float detection = 0.01f;
    int counter = 0;

    void Start()
    {
        direction = Player.PlayerReference.transform.up;
    }

    /// <summary>
    /// Denna metod börjar med att kontrollera skottets rörelse.
    /// Sedan görs en raycast som tittar ifall skottet studsar med något.
    /// Ifall skottet studsar med något beräknas infallsradianen,
    /// räknar ut den nya vikeln och sätter skottets nya färdrikning till den 
    /// framräknade vikeln. Skottets snabbhet ökas och vi lägger även till en poäng
    /// på en räknare som sedan läggs till i poängsystemet om vi dödar en fiende.
    /// I slutet av metoden tittar vi så att hastigheten inte gått över maxhastigheten.
    /// </summary>
    void Update()
    {
        transform.position += transform.up * speed * Time.deltaTime;

        Ray2D ray = new Ray2D(transform.position, transform.up);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, Time.deltaTime * speed + detection, collisionMask);
        if (hit.collider != null)
        { 
            Vector2 v = Vector2.Reflect(ray.direction, hit.normal);
            float rot = Mathf.Atan2(v.x, v.y) * Mathf.Rad2Deg;
            transform.up = v;
            counter++;
            speed += speedIncrease;
            SoundEvent sound = new SoundEvent(transform, bounceSound);
            sound.FireEvent();
        }
        if (speed > maxSpeed)
        {
            speed = maxSpeed;
        }
    }

    protected void OnTriggerEnter2D(Collider2D collider)
    {
        //Debug.Log("Collision");
        if (collider.CompareTag("Enemy"))
        {
            if(counter > 0)
            {
                collider.gameObject.GetComponent<HealthComponent>().TakeDamage(50);
                GameManager.Instance.UpdateScore(counter);
            }
            ShakeCamera.ShakeCameraRef.Shake(0.01f, 0.2f);
            Destroy(gameObject);
        }
        else if (collider.Equals(Player.PlayerReference.GetComponent<Collider2D>()))
        {
            if(counter > 0)
            {
                Player.PlayerReference.Damaged();
                Destroy(gameObject);
            }
        }
    }
}
