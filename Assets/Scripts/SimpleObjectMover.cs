using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimpleObjectMover : MonoBehaviour
{
    void Update()
    {
        move();
    }

    protected abstract void move();
}
