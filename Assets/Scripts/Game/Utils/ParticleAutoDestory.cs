using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAutoDestory : MonoBehaviour
{
    [SerializeField] private float _destoryTime = 2f;
    private float _timer = 0f;
    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _destoryTime)
        {
            Destroy(gameObject);
        }
    }
}
