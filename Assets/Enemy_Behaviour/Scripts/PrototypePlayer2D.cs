using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EnemyBehaviourPrototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PrototypePlayer2D : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpSpeed = 11f;
        [SerializeField] private float attackCooldown = 0.3f;
        [SerializeField] private float hitImpulse = 5f;
        [SerializeField] private Vector2 attackSize = new Vector2(1.2f, 1f);
        [SerializeField] private Transform attackVisual;

        private Rigidbody2D body;
        private BoxCollider2D playerCollider;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];
        private readonly HashSet<Rigidbody2D> struckBodies = new HashSet<Rigidbody2D>();
        private ContactFilter2D groundFilter;
        private float horizontal;
        private float facing = 1f;
        private float nextAttack;
        private float visualUntil;
        private bool jumpQueued;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            playerCollider = GetComponent<BoxCollider2D>();
            groundFilter = new ContactFilter2D { useTriggers = false };
            if (attackVisual != null) attackVisual.gameObject.SetActive(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            horizontal = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
                if (keyboard.spaceKey.wasPressedThisFrame) jumpQueued = true;
            }
            if (horizontal != 0f) facing = Mathf.Sign(horizontal);

            bool attackPressed = (keyboard != null && keyboard.jKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
            if (attackPressed && Time.time >= nextAttack) Attack();

            if (attackVisual != null)
                attackVisual.gameObject.SetActive(Time.time < visualUntil);
        }

        private void FixedUpdate()
        {
            Vector2 velocity = body.linearVelocity;
            velocity.x = horizontal * moveSpeed;
            if (jumpQueued && IsGrounded()) velocity.y = jumpSpeed;
            jumpQueued = false;
            body.linearVelocity = velocity;
        }

        private bool IsGrounded()
        {
            if (body.linearVelocity.y > 0.1f) return false;
            int count = playerCollider.Cast(Vector2.down, groundFilter, groundHits, 0.08f);
            for (int i = 0; i < count; i++)
                if (groundHits[i].normal.y > 0.5f) return true;
            return false;
        }

        private void Attack()
        {
            nextAttack = Time.time + attackCooldown;
            visualUntil = Time.time + 0.12f;
            Vector2 center = (Vector2)transform.position + new Vector2(facing * 1f, 0f);
            if (attackVisual != null)
            {
                attackVisual.localPosition = new Vector3(facing, 0f, 0f);
                attackVisual.localScale = new Vector3(attackSize.x, attackSize.y, 1f);
            }

            struckBodies.Clear();
            foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, attackSize, 0f))
            {
                Rigidbody2D target = hit.attachedRigidbody;
                if (hit.isTrigger || target == null || target == body
                    || target.bodyType != RigidbodyType2D.Dynamic || !struckBodies.Add(target)) continue;
                target.AddForce(new Vector2(facing * hitImpulse, hitImpulse * 0.3f), ForceMode2D.Impulse);
                // Optional hook for your enemy script: void OnPrototypeHit(float damage).
                target.gameObject.SendMessage("OnPrototypeHit", 1f, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
}
