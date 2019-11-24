using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy : LivingEntity
{
    private enum State
    {
        Patrol,
        Tracking,
        AttackBegin,
        Attacking
    }
    
    private State state;
    
    private NavMeshAgent agent;
    private Animator animator;

    public Transform attackRoot;
    public Transform eyeTransform;
    
    private AudioSource audioPlayer;
    public AudioClip hitClip;
    public AudioClip deathClip;
    
    private Renderer skinRenderer;

    public float runSpeed = 10f;
    [Range(0.01f, 2f)] public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;
    
    public float damage = 30f;
    public float attackRadius = 2f;
    private float attackDistance;
    
    public float fieldOfView = 50f;
    public float viewDistance = 10f;
    public float patrolSpeed = 3f;
    
    [HideInInspector] public LivingEntity targetEntity;
    public LayerMask whatIsTarget;


    private RaycastHit[] hits = new RaycastHit[10];
    private List<LivingEntity> lastAttackedTargets = new List<LivingEntity>();
    
    private bool hasTarget => targetEntity != null && !targetEntity.dead;
    

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        if(attackRoot != null) {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

            Gizmos.DrawSphere(attackRoot.position, attackRadius);
        }


        if(eyeTransform != null) {
            var leftEyeRotation = Quaternion.AngleAxis(-fieldOfView * 0.5f, Vector3.up);
            var leftRayRotation = leftEyeRotation * transform.forward;

            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            Handles.DrawSolidArc(eyeTransform.position, Vector3.up, leftRayRotation, fieldOfView, viewDistance);
        }
    }
    
#endif
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioPlayer = GetComponent<AudioSource>();
        skinRenderer = GetComponent<SkinnedMeshRenderer>();

        var attackPivot = attackRoot.position;
        attackPivot.y = transform.position.y;
        attackDistance = Vector3.Distance(transform.position, attackRoot.position) + attackRadius;
    
        agent.stoppingDistance = attackDistance;
        agent.speed = patrolSpeed;
    }

    public void Setup(float health, float damage, float runSpeed, float patrolSpeed, Color skinColor)
    {
        this.startingHealth = health;
        this.health = health;

        this.damage = damage;
        this.runSpeed = runSpeed;
        this.patrolSpeed = patrolSpeed;

        this.skinRenderer.material.color = skinColor;

        agent.speed = patrolSpeed;
    }

    private void Start()
    {
        StartCoroutine(UpdatePath());
    }

    private void Update()
    {
        if(dead) {
            return;
        }

        if(state == State.Tracking) {
            
            var distance = Vector3.Distance(targetEntity.transform.position, transform.position);

            if(distance <= attackDistance) {
                BeginAttack();
            }
        }

        animator.SetFloat("Speed", agent.desiredVelocity.magnitude);
    }

    private void FixedUpdate()
    {
        if (dead) return;

        if(state == State.AttackBegin || state == State.Attacking) {
            var lookRotation = Quaternion.LookRotation(targetEntity.transform.position - transform.position);
            var targetAngleY = lookRotation.eulerAngles.y;

            targetAngleY = Mathf.SmoothDamp(transform.eulerAngles.y, targetAngleY, ref turnSmoothVelocity, turnSmoothTime);
            transform.eulerAngles = Vector3.up * targetAngleY;
        }

        if(state == State.Attacking) {
            var direction = transform.forward;
            var deltaDistance = agent.velocity.magnitude * Time.deltaTime;

            var size = Physics.SphereCastNonAlloc(attackRoot.position, attackRadius, direction, hits, deltaDistance, whatIsTarget);

            for(var i=0; i<size; i++) {
                var attackTargetEntity = hits[i].collider.GetComponent<LivingEntity>();

                if(attackTargetEntity != null && !lastAttackedTargets.Contains(attackTargetEntity)) {
                    var message = new DamageMessage();

                    message.amount = damage;
                    message.damager = gameObject;
                    
                    if(hits[i].distance <= 0f) {
                        message.hitPoint = attackRoot.position;
                    } else {
                        message.hitPoint = hits[i].point;
                    }
                    
                    message.hitNormal = hits[i].normal;

                    attackTargetEntity.ApplyDamage(message);
                    lastAttackedTargets.Add(attackTargetEntity);

                    break;

                }
            }
        }
    }

    private IEnumerator UpdatePath()
    {
        while (!dead)
        {
            if (hasTarget)
            {
                if(state == State.Patrol) {
                    state = State.Tracking;
                    agent.speed = runSpeed;
                }

                agent.SetDestination(targetEntity.transform.position);
            }
            else
            {
                if (targetEntity != null) targetEntity = null;

                if(state != State.Patrol) {
                    state = State.Patrol;
                    agent.speed = patrolSpeed;
                }

                if(agent.remainingDistance <= 1f) {
                    var patrolTargetPosition = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                    agent.SetDestination(patrolTargetPosition);
                }

                var colliders = Physics.OverlapSphere(eyeTransform.position, viewDistance, whatIsTarget);

                foreach(var collider in colliders) {
                    if(!IsTargetOnSight(collider.transform)) {
                        continue;
                    }

                    var livingEntity = collider.GetComponent<LivingEntity>();

                    if(livingEntity != null && !livingEntity.dead) {
                        targetEntity = livingEntity;
                        break;
                    }
                }
            }

            yield return new WaitForSeconds(0.05f);
        }
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage)) return false;
        
        if(targetEntity == null) {
            targetEntity = damageMessage.damager.GetComponent<LivingEntity>();


        }

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform, EffectManager.EffectType.Flesh);
        audioPlayer.PlayOneShot(hitClip);

        return true;
    }

    public void BeginAttack()
    {
        state = State.AttackBegin;

        agent.isStopped = true;
        animator.SetTrigger("Attack");
    }

    public void EnableAttack()
    {
        state = State.Attacking;
        
        lastAttackedTargets.Clear();
    }

    public void DisableAttack()
    {
        if(hasTarget) {
            state = State.Tracking;
        } else {
            state = State.Patrol;
        }
        
        agent.isStopped = false;
    }

    private bool IsTargetOnSight(Transform target)
    {
        var direction = target.position - eyeTransform.position;
        direction.y = eyeTransform.forward.y;

        if(Vector3.Angle(direction, eyeTransform.forward) > fieldOfView * 0.5f) {
            return false;
        }

        direction = target.position - eyeTransform.position;

        RaycastHit hit;

        if(Physics.Raycast(eyeTransform.position, direction, out hit, viewDistance, whatIsTarget)) {
            if(hit.transform == target) {
                return true;
            }
        }

        return false;
    }
    
    public override void Die()
    {
        base.Die();

        GetComponent<Collider>().enabled = false;

        agent.isStopped = true;
        agent.enabled = false;

        animator.applyRootMotion = true;
        animator.SetTrigger("Die");

        audioPlayer.PlayOneShot(deathClip);
    }
}