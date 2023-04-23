using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimpleObjectMover : MonoBehaviour
{
    void Start()
    {
        
    }
    
    void Update()
    {
        move();
    }

    protected abstract void move();
}
