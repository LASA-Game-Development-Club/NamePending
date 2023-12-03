using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnakePathfinder : MonoBehaviour, IPathfinder
{
    public float snakeyness;
    public float snaketime;

    private float _currSnakeTime;
    public Vector2 PathDirNorm(Vector2 currentPosition, Vector2 targetPosition)
    {
        var (tarAng, tarRad) = ToPolar(targetPosition);
        var (selfAng, selfRad) = ToPolar(currentPosition);
        float angDif = SmallestAngleDist(selfAng, tarAng);
        float radDif = tarRad - selfRad;
        float delta = .01f;
        Vector2 dir = ((delta * radDif + selfRad) * new Vector2(Mathf.Cos(delta * angDif + selfAng), Mathf.Sin(delta * angDif + selfAng))
            - (Vector2)currentPosition).normalized;
        float snakeAngle = snakeyness * Mathf.Sin(2 * Mathf.PI * _currSnakeTime / snaketime);
        dir = rot(dir, snakeAngle);
        return dir;
    }

    Vector2 rot(Vector2 vec, float angle)
    {
        return new Vector2(vec.x * Mathf.Cos(angle) - vec.y * Mathf.Sin(angle), vec.x * Mathf.Sin(angle) + vec.y * Mathf.Cos(angle));
    }
    (float, float) ToPolar(Vector2 vec)
    {
        float angle = Mathf.Atan2(vec.y, vec.x);
        float radius = vec.magnitude;
        return (angle, radius);
    }
    float MinUnsigned(float x, float y)
    {
        if (Mathf.Abs(x) < Mathf.Abs(y))
        {
            return x;
        }
        else
        {
            return y;
        }
    }

    float SmallestAngleDist(float orig, float tar)
    {
        return MinUnsigned(MinUnsigned(tar - orig, tar + 2 * Mathf.PI - orig), tar - 2 * Mathf.PI - orig);
    }

    private void Update()
    {
        _currSnakeTime = (_currSnakeTime + Time.deltaTime) % snaketime;
    }
}
