using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Util;

public class PlatelinController : MonoBehaviour
{
    public float spawndelay;
    public float swellsize;
    public float swelltime;
    public GameObject goo;
    public GameObject gravitySource;

    float _spawntimer;
    float _swelltimer;

    public enum Mode
    {
        Immature,
        Idle,
        Swell,
    };

    public Mode mode;

    void Start()
    {
        mode = Mode.Immature;
        _spawntimer += 3 * spawndelay;
        GetComponent<SpriteRenderer>().color = Color.blue;
    }

    
    void Update()
    {

        if(transform.position.sqrMagnitude > 75 * 75)
        {
            GetComponent<CustomRigidbody2D>().AddForce(-10 * ((Vector2)transform.position).normalized);
        }

        if (mode == Mode.Immature)
        {
            _spawntimer -= Time.deltaTime;
            if (_spawntimer <= 0)
            {
                mode = Mode.Idle;
                _spawntimer = spawndelay;
                GetComponent<SpriteRenderer>().color = Color.white;
            }
        }

        if (mode == Mode.Idle)
        {
            _spawntimer -= Time.deltaTime;
            if (_spawntimer <= 0)
            {
                mode = Mode.Swell;
                _swelltimer = swelltime;
            }
        } else if (mode == Mode.Swell)
        {
            _swelltimer -= Time.deltaTime;
            float scale = 1.5f + (swellsize - 1.5f) * (swelltime - _swelltimer) / swelltime;
            transform.localScale = new Vector3(scale, scale, 1);
            if(_swelltimer <= 0)
            {
                mode = Mode.Idle;
                _spawntimer = spawndelay;
                transform.localScale = new Vector3(1.5f, 1.5f, 1);
                var copy = Instantiate(this, transform.parent);
                copy.transform.position += (Vector3) Random.insideUnitCircle;

                if(Random.Range(0f,1f) < .05f)
                {
                    copy.GetComponent<CustomRigidbody2D>().AddForce(10000 * Random.insideUnitCircle);
                    copy.GetComponent<PlatelinController>()._spawntimer += 5;
                }
            }
        }
    }

    private void OnDestroy()
    {
        var goo2 = Instantiate(goo, transform.position, transform.rotation);
        
    }
}
