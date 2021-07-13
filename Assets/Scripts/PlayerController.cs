using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum States
    {
        NORMAL,
        TONGUE,
        RETRACT,
        SHOOT,
        STATES
    };

    public States currentState;

    private Rigidbody rb;
    private Animator anim;

    private Vector2 joystickInput;
    private Vector3 forward;
    private Vector3 right;

    [SerializeField]
    private Transform head;

    [SerializeField]
    private Transform cam;
    private Vector3 target;

    [SerializeField]
    private float camSpeed;
    private float distance;

    [SerializeField]
    private float speed;

    [SerializeField]
    private LayerMask enemy;
    private Dictionary<int, HashSet<Transform>> enemies;

    [SerializeField]
    private LineRenderer tongue;

    [SerializeField]
    private Color[] tongueColors;

    [SerializeField]
    private float tongueSpeed;
    [SerializeField]
    private float tongueTurnAngle;

    [SerializeField]
    private float tongueDur;
    [SerializeField]
    private float tongueHoldDur;
    [SerializeField]
    private float tongueTimer;

    private Vector3 tongueDir;
    private Vector3 targetTongueDir;

    private int positionCap;

    [SerializeField]
    private int ammo;

    [SerializeField]
    private float burstDur;
    [SerializeField]
    private float burstTimer;

    [SerializeField]
    private GameObject projectile;
    [SerializeField]
    private float projectileSpeed;

    [SerializeField]
    private GameObject aimLine;

    // Start is called before the first frame update
    void Start()
    {
        enemies = new Dictionary<int, HashSet<Transform>>();

        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();

        distance = (transform.position - cam.position).magnitude;

        currentState = States.NORMAL;
    }

    private void LateUpdate()
    {
        // Animations
        anim.SetFloat("Speed", rb.velocity.magnitude);
        anim.SetBool("Tongue", currentState == States.TONGUE || currentState == States.RETRACT);

        // Adjust camera
        cam.transform.position = Vector3.Lerp(cam.transform.position, target - (cam.forward * distance), camSpeed * Time.deltaTime);

        // Blow up head based off ammo
        head.localScale = Vector3.one * ((ammo * 1.0f / 4.0f) + 1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        joystickInput = new Vector2(Input.GetAxis("Horizontal"),  Input.GetAxis("Vertical"));

        forward = new Vector3(cam.forward.x, 0.0f, cam.forward.z).normalized;
        right = new Vector3(cam.right.x, 0.0f, cam.right.z).normalized;

        switch (currentState)
        {
            case States.NORMAL:
                if (Input.GetButtonDown("Fire2"))
                {
                    // Shoot enemies
                    if (ammo > 0)
                    {
                        aimLine.SetActive(false);

                        burstTimer = burstDur;

                        rb.velocity = new Vector3(0.0f, rb.velocity.y, 0.0f);
                        currentState = States.SHOOT;
                    }
                    // Avtivate tongue
                    else
                    {
                        tongueTimer = 0.0f;
                        tongueDir = transform.forward;
                        targetTongueDir = tongueDir;

                        rb.velocity = new Vector3(0.0f, rb.velocity.y, 0.0f);
                        currentState = States.TONGUE;
                    }
                    break;
                }

                // Show aim line if player has ammo
                if (ammo > 0)
                    aimLine.SetActive(true);
                else
                    aimLine.SetActive(false);

                // Adjust line renderer
                tongue.positionCount = 2;
                tongue.SetPosition(0, tongue.transform.position);
                tongue.SetPosition(1, tongue.transform.position);

                transform.LookAt(transform.position + new Vector3(rb.velocity.x, 0.0f, rb.velocity.z).normalized);

                // Set camera target
                target = transform.position;

                // Apply movement
                rb.velocity = new Vector3(0.0f, rb.velocity.y, 0.0f) + (((forward * joystickInput.y) + (right * joystickInput.x)).normalized * speed / ((ammo * 1.0f / 4.0f) + 1.0f));
                break;
            case States.TONGUE:
                // Button was let go or tongue has reached it's limit
                if (Input.GetButtonUp("Fire2") || tongueTimer >= tongueDur + tongueHoldDur)
                {
                    while(tongue.positionCount < positionCap)
                    {
                        tongue.positionCount++;
                        tongue.SetPosition(tongue.positionCount - 1, tongue.GetPosition(tongue.positionCount - 2));
                    }

                    currentState = States.RETRACT;
                    break;
                }

                tongueTimer += Time.deltaTime;

                if (tongueTimer < tongueDur)
                {
                    positionCap = tongue.positionCount;

                    // Set camera target to the tip of the tongue
                    target = tongue.GetPosition(tongue.positionCount - 1);
                }
                else
                {
                    // Tongue is past the cap
                    if (tongue.positionCount >= positionCap)
                    {
                        // Remove the last for verts
                        tongue.positionCount -= 4;
                        tongueDir = (tongue.GetPosition(tongue.positionCount - 1) - tongue.GetPosition(tongue.positionCount - 2)).normalized;
                    }
                }

                Vector3 joystickDir = ((forward * joystickInput.y) + (right * joystickInput.x)).normalized;

                if (joystickDir != Vector3.zero)
                {
                    // Set target direction to joystick direction
                    targetTongueDir = joystickDir;
                }

                // The distance between the last two points are greater than 0.5
                if (Vector3.Distance(tongue.GetPosition(tongue.positionCount - 1), tongue.GetPosition(tongue.positionCount - 2)) >= 0.5f)
                {
                    // Add new point
                    tongue.positionCount++;
                    tongue.SetPosition(tongue.positionCount - 1, tongue.GetPosition(tongue.positionCount - 2));

                    // Rotate tongue in the direction of the target direction
                    float angle = Vector3.SignedAngle(tongueDir, targetTongueDir, Vector3.up);
                    angle = Mathf.Abs(angle) > tongueTurnAngle ? Mathf.Sign(angle) * tongueTurnAngle : angle;

                    tongueDir = Quaternion.AngleAxis(angle, Vector3.up) * tongueDir;
                }

                // Move tip of tongue in target direction
                tongue.SetPosition(tongue.positionCount - 1, tongue.GetPosition(tongue.positionCount - 1) + (tongueDir * tongueSpeed * Time.deltaTime));
                // Turn the tip of the tongue from red to yellow as it stretches
                tongue.endColor = Color.Lerp(tongueColors[0], tongueColors[1], tongueTimer / tongueDur);

                CheckTongueCollision();
                break;
            case States.SHOOT:
                if (ammo == 0)
                {
                    currentState = States.NORMAL;
                }
                // Shoot enemies in bursts
                else
                {
                    burstTimer += Time.deltaTime;

                    if (burstTimer >= burstDur)
                    {
                        ammo--;

                        // Create projectile
                        GameObject p = Instantiate(projectile, tongue.transform);
                        p.transform.localPosition = Vector3.zero;

                        Rigidbody projectileRigidBody = p.transform.GetComponent<Rigidbody>();

                        // Move projectile in the direction the player is facing
                        projectileRigidBody.velocity = transform.forward * projectileSpeed;

                        // Spin the projectile for a nice effect
                        projectileRigidBody.angularVelocity = new Vector3(0.0f, 50000.0f, 0.0f);

                        p.transform.parent = null;

                        burstTimer = 0.0f;
                    }
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case States.RETRACT:
                if (tongue.positionCount > 0)
                {
                    int oldCount = tongue.positionCount - 1;
                    target = tongue.GetPosition(oldCount);

                    // Decrese tonge points by two
                    tongue.positionCount -= tongue.positionCount - 2 < 0 ? 1 : 2;

                    int newCount = tongue.positionCount - 1;

                    // For each point removed
                    while (oldCount > newCount)
                    {
                        // If removed point contains enemies
                        if (enemies.ContainsKey(oldCount) && tongue.positionCount > 0)
                        {
                            // Move enemies to the tip of the tongue
                            foreach (Transform enemy in enemies[oldCount])
                            {
                                enemy.position = tongue.GetPosition(newCount);
                            }

                            // Add new key if not created with hashset filled with enemies in old key
                            if (!enemies.ContainsKey(newCount))
                            {
                                enemies.Add(newCount, enemies[oldCount]);
                            }
                            else
                            {
                                // Add new enemies to current hash set
                                foreach (Transform enemy in enemies[oldCount])
                                {
                                    enemies[newCount].Add(enemy);
                                }

                                enemies[oldCount].Clear();
                            }

                            enemies.Remove(oldCount);
                        }

                        oldCount--;
                    }
                }
                else
                {
                    // Destroy enemies add ammo
                    foreach (KeyValuePair<int, HashSet<Transform>> index in enemies)
                    {
                        foreach (Transform enemy in enemies[index.Key])
                        {
                            ammo++;
                            Destroy(enemy.gameObject);
                        }
                    }

                    enemies.Clear();

                    currentState = States.NORMAL;
                }
                break;
        }
    }

    void CheckTongueCollision()
    {
        // For each tongue point
        for (int i = 0; i < tongue.positionCount - 2; i++)
        {
            // Get vector using the current tongue point to the next
            Vector3 v = tongue.GetPosition(i + 1) - tongue.GetPosition(i);

            Vector3 dir = v.normalized;
            float length = v.magnitude;

            RaycastHit hit;

            // Tongue has collided with enemy
            if (Physics.Raycast(tongue.GetPosition(i), dir, out hit, length, enemy))
            {
                Debug.DrawRay(tongue.GetPosition(i), dir * length, Color.red);

                // Add index to dictionary
                if (!enemies.ContainsKey(i + 1))
                {
                    enemies.Add(i + 1, new HashSet<Transform>());

                    TrapEnemy(hit.transform, i);
                }
                else
                {
                    // Hashset at key index does not already contain this enemy
                    if (!enemies[i + 1].Contains(hit.transform))
                    {
                        TrapEnemy(hit.transform, i);
                    }
                }
            }
            else
            {
                Debug.DrawRay(tongue.GetPosition(i), dir * length, Color.green);
            }
        }
    }

    void TrapEnemy(Transform enemy, int index)
    {
        // Remove enemy components
        Destroy(enemy.transform.gameObject.GetComponent<Collider>());
        Destroy(enemy.transform.gameObject.GetComponent<Rigidbody>());

        // Expand enemy
        enemy.transform.localScale = Vector3.one * 1.25f;

        // Add enemy transform to hashset in dictionary with the current index as the key
        enemies[index + 1].Add(enemy.transform);
    }
}
