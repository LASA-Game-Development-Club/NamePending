using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidHandler : MonoBehaviour
{
    public float visualRangel;
    public float protectedRange;
    public float matchingFactor;
    public float centeringFactor;
    public float avoidFactor;
    public float targettingFactor;
    public float maxSpeed;
    public float minSpeed;
    public GameObject target;
    public GameObject bulletPrefab;
    public GameObject playArea;
    public GameObject gravitySource;
    public float shootDist;
    private Rigidbody2D _rigidbody2D;

    private void Start()
    {
        _rigidbody2D = transform.GetComponent<Rigidbody2D>();
    }

    float SqrHypot(Vector2 v, Vector2 w)
    {
        return (v.x - w.x) * (v.x - w.x) + (v.y - w.y) * (v.y - w.y);
    }

    void Shoot()
    {
        Vector2 diff = new Vector2(target.transform.position.x - transform.position.x, target.transform.position.y - transform.position.y);
        Quaternion rot = Quaternion.Euler(new Vector3(0, 0, -90 + Mathf.Rad2Deg * Mathf.Atan2(diff.y, diff.x)));
        
        if(diff.sqrMagnitude < shootDist * shootDist)
        {
            var bullet = Instantiate(bulletPrefab, transform.position, rot);
            bullet.GetComponent<DestroyOffScreen>().playArea = playArea;
            bullet.GetComponent<Gravitatable>().gravitySource = gravitySource;
            bullet.GetComponent<Rigidbody2D>().AddRelativeForce(new Vector2(0, 500));
        }

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        List<GameObject> otherBoids = new List<GameObject>() ;
        List<GameObject> nearBoids = new List<GameObject>();

        Vector2 avgVel = new Vector2();
        Vector2 avgPos = new Vector2();
        Vector2 pushaway = new Vector2();

        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform child = transform.parent.GetChild(i);
            if (SqrHypot(transform.position, child.transform.position) < visualRangel)
            {
                otherBoids.Add(child.gameObject);
                avgVel += child.GetComponent<Rigidbody2D>().velocity;
                avgPos += child.GetComponent<Rigidbody2D>().position;

                if (SqrHypot(transform.position, child.transform.position) < protectedRange)
                {
                    nearBoids.Add(child.gameObject);
                    pushaway += (_rigidbody2D.position - child.GetComponent<Rigidbody2D>().position) * avoidFactor;
                }
            }
        }
        if (otherBoids.Count > 0)
        {
            avgVel /= otherBoids.Count;
            avgPos /= otherBoids.Count;

            _rigidbody2D.velocity += pushaway;
            _rigidbody2D.velocity += (avgVel - _rigidbody2D.velocity) * matchingFactor;
            _rigidbody2D.velocity += (avgPos - _rigidbody2D.position) * centeringFactor;
            _rigidbody2D.velocity += ((Vector2)target.transform.position - _rigidbody2D.position) * targettingFactor;

            float sqrSpeed = _rigidbody2D.velocity.sqrMagnitude;
            if (sqrSpeed > maxSpeed * maxSpeed)
            {
                Vector2 uVel = _rigidbody2D.velocity.normalized;
                _rigidbody2D.velocity = uVel * maxSpeed;
            } else if (sqrSpeed < minSpeed * minSpeed)
            {
                Vector2 uVel = _rigidbody2D.velocity.normalized;
                _rigidbody2D.velocity = uVel * minSpeed;
            }

            Shoot();

        } else
        {
            _rigidbody2D.velocity = new Vector2();
        }
    }
}