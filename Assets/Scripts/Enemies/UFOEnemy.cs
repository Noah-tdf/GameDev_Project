using UnityEngine;

public class UFOEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float targetUpdateInterval = 3f;
    
    [Header("Attack")]
    public GameObject beamPrefab;
    public float attackInterval = 4f;
    public float attackDuration = 1f;

    private Transform player;
    private float targetX;
    private float nextTargetTime;
    private float nextAttackTime;
    private bool isAttacking;

    private SpriteRenderer sr;
    private Animator anim;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        
        targetX = transform.position.x;
        nextAttackTime = Time.time + Random.Range(1f, attackInterval);
    }

    private void Update()
    {
        if (player == null) return;

        // Move towards target X
        float currentX = transform.position.x;
        float newX = Mathf.MoveTowards(currentX, targetX, moveSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        // Update target X periodically
        if (Time.time >= nextTargetTime)
        {
            targetX = player.position.x;
            nextTargetTime = Time.time + targetUpdateInterval;
        }

        // Trigger attack
        if (Time.time >= nextAttackTime && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
            nextAttackTime = Time.time + attackInterval;
        }

        // Visuals
        if (newX < currentX) sr.flipX = true;
        else if (newX > currentX) sr.flipX = false;
    }

    private System.Collections.IEnumerator AttackRoutine()
    {
        isAttacking = true;
        
        // Visual indicator/animation trigger if exists
        if (anim != null) anim.SetTrigger("Attack");

        // Spawn beam
        if (beamPrefab != null)
        {
            GameObject beam = Instantiate(beamPrefab, transform.position, Quaternion.identity);
            beam.transform.SetParent(transform); // Move with UFO? 
            // Or let it stay? "target me... they'll shoot their beam". 
            // Usually beams are stationary or follow. I'll make it follow by parenting.
            
            UFOTractorBeam beamScript = beam.GetComponent<UFOTractorBeam>();
            if (beamScript != null)
            {
                beamScript.Activate(attackDuration);
            }
            
            yield return new WaitForSeconds(attackDuration);
        }

        isAttacking = false;
    }
}
