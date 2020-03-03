using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * @author Anders Ragnar & Niclas Älmeby
 */

    /// <summary>
    /// Detta script är fiendens synfält, den ritar ut två koner och ifall spelaren
    /// befinner sig inom dessa koner så ser fienden spelaren.
    /// </summary>
public class FieldOfView : MonoBehaviour
{
    [Tooltip("Det breda synfältets längd")]
    [SerializeField] private float bigViewRadius;
    [Tooltip("Det smala synfältets längd")]
    [SerializeField] private float smallViewRadius;
    [Range(0, 360)]
    [Tooltip("Det smala synfältets bredd")]
    [SerializeField] private float smallViewAngle;
    [Range(0, 360)]
    [Tooltip("Det breda synfältets bredd")]
    [SerializeField] private float bigViewAngle;
    [Tooltip("Lagermasken som synfälten ska träffa (spelaren i detta fallet)")]
    [SerializeField] private LayerMask TargetsToHit;
    [Tooltip("Hur lång tid det ska ta för fienden att upptäcka spelaren")]
    [SerializeField] private float smallViewTimer, bigViewTimer;
    [Tooltip("Tid det ska ta innan fienden stannar när den ser katten i stora synfältet")]
    [SerializeField] private float stopTime;
    [Tooltip("Hur lång tid det ska ta innan fienden tappar bort spelaren")]
    [SerializeField] private float losePlayerTime;
    [Tooltip("Vilken höjd finenden ska titta på katten ifrån(ögonen är ungefär 1.67)")]
    [SerializeField] private float viewHeight;
    [Tooltip("Lagrena som ska blockera fiendens syn")]
    [SerializeField] private LayerMask blockViewLayers;
    [Tooltip("Feedback ögonen")]
    [SerializeField] private GameObject alert, seen;
   
    private float findPlayerTimer, losePlayerTimer, stopTimer;
    private ViewState state;
    private bool noticed;
    private float idleAudioTimer;
    private float noticedTimer = 0;
    private EnemySound enemySound;

    private void Start()
    {
        idleAudioTimer = RandomTimer();
        state = ViewState.LookingForPlayer;
        enemySound = GetComponent<EnemySound>();
    }
    public Vector3 DirectionFromAngle(float angle, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
            angle += transform.eulerAngles.y;
        return new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad), 0, Mathf.Cos(angle * Mathf.Deg2Rad));
    }

    private void Update()
    {
        switch (state)
        {
            case ViewState.LookingForPlayer:
                LookingForPlayer();
                idleAudioTimer -= Time.deltaTime;
                if(idleAudioTimer <= 0)
                {
                    enemySound.PlayDialogClips("idle");
                    idleAudioTimer = RandomTimer();
                }
                break;
            case ViewState.FoundPlayer:
                FoundPlayer();
                break;
            default:
                Debug.Log("field of view Default");
                break;
        }
        if(noticedTimer >= 0f)
        {
            noticedTimer -= Time.deltaTime;
        }
    }

    /// <summary>
    /// tittar ifall spelaren är innuti den stora eller den lilla konen
    /// </summary>
    private void LookingForPlayer()
    {
        if (TargetsInFieldOfView(smallViewRadius, smallViewAngle))
        {
            DiscoveringPlayer(smallViewTimer);
        }
        else if (TargetsInFieldOfView(bigViewRadius, bigViewAngle))
        {
            stopTimer -= Time.deltaTime;
            if (stopTimer <= 0)
            {
                if(noticedTimer <= 0)
                {
                    enemySound.PlayDialogClips("alerted");
                    noticedTimer = 5f;
                }
                noticed = true;
                new NoticePlayerEvent(GetComponent<EnemyBehaviourTree>()).FireEvent();
            }
            DiscoveringPlayer(bigViewTimer);
        }
        else
        {
            if (alert.activeInHierarchy)
            {
                if (noticed)
                {
                    stopTimer = stopTime;
                    noticed = false;
                    new LostAndFoundPlayerEvent(GetComponent<EnemyBehaviourTree>(), false).FireEvent();
                    enemySound.PlayDialogClips("lostCat");
                }
                alert.SetActive(false);
            }
            findPlayerTimer = 0;
            //Debug.Log("field of view haven't discovered player");
        }
    }

    /// <summary>
    /// A method that calculates when how long it should take until the player
    /// is descovered by the enemy,
    /// 
    /// En metod för att räkna ut när spelaren ska bli upptäckt.
    /// Den stora och den lilla konen ska vara beroende av samma timer.
    /// Går man från den lilla till den stora konen vill man använda samma timer
    /// annars skulle ett loophole skapas som kan utnyttjas ganska mycket (enligt min uppfattning).
    /// </summary>
    /// <param name="timer">Hur lång tid det ska ta för de olika konerna att upptäcka spelaren</param>
    private void DiscoveringPlayer(float timer)
    {
        if (!alert.activeInHierarchy)
        {
            new EnemyAlertEvent().FireEvent();
            alert.SetActive(true);

        }
        findPlayerTimer += Time.deltaTime;
        if (findPlayerTimer >= timer)
        {
            enemySound.PlayDialogClips("aware");
            alert.SetActive(false);
            seen.SetActive(true);
            state = ViewState.FoundPlayer;
            findPlayerTimer = 0;
            new LostAndFoundPlayerEvent(GetComponent<EnemyBehaviourTree>(), true).FireEvent();
            

        }
    }
    private void FoundPlayer()
    {
        if(TargetsInFieldOfView(smallViewRadius, smallViewAngle) || TargetsInFieldOfView(bigViewRadius, bigViewAngle))
        {
            losePlayerTimer = 0;
        }
        else
        {
            losePlayerTimer += Time.deltaTime;
            if(losePlayerTimer >= losePlayerTime)
            {
                seen.SetActive(false);
                losePlayerTimer = 0;
                new LostAndFoundPlayerEvent(GetComponent<EnemyBehaviourTree>(), false).FireEvent();
                state = ViewState.LookingForPlayer;
            }
        }
    }

   /// <summary>
   /// First a sphere is created, if it hits nothing the method cancels.
   /// If the sphere hits the player, direction to target calculates what direction the player is in.
   /// If the player is inside the direction of the field of view we will cast a linecast or a raycast.
   /// If nothing is in between we return true!.
   /// </summary>
   /// <param name="viewRadius">How long the filed of view should be</param>
   /// <param name="viewAngle">How wide the filed of view should be</param>
   /// <returns> if the enemy can see the player</returns>
    private bool TargetsInFieldOfView(float viewRadius, float viewAngle)
    {
        //Debug.Log("looks");
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, viewRadius, TargetsToHit);
        if(targetsInRadius.Length == 0)
        {
            return false;
        }
        //List<GameObject> objs = new List<GameObject>();
        for (int i = 0; i < targetsInRadius.Length; i++)
        {
            Transform target = targetsInRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                //layermasken är de lagrena som ska blockera fiendens syn
                if (!Physics.Linecast(new Vector3(transform.position.x, viewHeight, transform.position.z), target.position, blockViewLayers))
                {
                    Debug.DrawLine(new Vector3(transform.position.x, viewHeight, transform.position.z), target.position, Color.green);

                            return true;

                }

            }
        }
        return false;
    }
    public float RandomTimer()
    {
        return Random.Range(25, 85);
    }

    /// <summary>
    /// Draws the cone of the field of view.
    /// 
    /// Ritar ut fälten som fienden kommer att ha som synfält för att det ska bli enkelt
    /// att få en bild på hur värdena blir påverake av alla varialer.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        //Gizmos.DrawWireSphere(transform.position, viewRadius);
        float bigRayRange = bigViewRadius;
        float bigHalfFOW = bigViewAngle / 2.0f;
        Quaternion bigLeftRayRotation = Quaternion.AngleAxis(-bigHalfFOW, Vector3.up);
        Quaternion bigRightRayRotation = Quaternion.AngleAxis(bigHalfFOW, Vector3.up);
        Vector3 bigLeftRayDirection = bigLeftRayRotation * transform.forward;
        Vector3 bigRightRayDirection = bigRightRayRotation * transform.forward;
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, bigLeftRayDirection * bigRayRange);
        Gizmos.DrawRay(transform.position, bigRightRayDirection * bigRayRange);

        float halfFOV = smallViewAngle / 2.0f;
        float rayRange = smallViewRadius;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * transform.forward;
        Vector3 rightRayDirection = rightRayRotation * transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftRayDirection * rayRange);
        Gizmos.DrawRay(transform.position, rightRayDirection * rayRange);
        //Gizmos.DrawWireSphere(transform.position, viewRadius);
    }
    public void Reset()
    {
        //new LostAndFoundPlayerEvent(GetComponent<EnemyBehaviourTree>(), true).FireEvent();
        //state = ViewState.LookingForPlayer;
    }

    /// <summary>
    /// Simple state machine to avoid to many if cases 
    /// 
    /// Skapade en enkel statemachine för att slippa kompicerade ifsatser.
    /// Det är också förberedande ifall man vill göra fienden extra uppmärksam vid vissa tillfällen.
    /// </summary>
    private enum ViewState
    {
        FoundPlayer,
        LookingForPlayer
    }
}

