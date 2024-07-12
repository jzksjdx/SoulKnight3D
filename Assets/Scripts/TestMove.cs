using System.Collections;
using System.Collections.Generic;
using SoulKnight3D;
using UnityEngine;

public class TestMove : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

        Vector2 lookVector = PlayerInputs.Instance.GetLookVector();
        transform.Rotate(new Vector3(0f, lookVector.x * 10, 0f));
    }
}
