using System;
using UnityEngine;
using DG.Tweening;

public class SimpleObjectMover : MonoBehaviour
{
    [SerializeField] private Transform _transform;
    [SerializeField] private GameObject _blocks;
    [SerializeField] private GameObject _ankers;
    private StepPoints[] _stepPointsArray;
    private int _ankerIndex;
    private Vector3 _prePos; // position of 1 frame before
    public Vector3 DisPos { get; private set; } // displacement at 1 frame

    private class StepPoints
    {

        public Vector3 Start { get; private set; }
        public Vector3 Stop { get; private set; }
        public Vector3[] Controls { get; private set; }
        public float MoveTakenMs { get; private set; }

        public StepPoints(Vector3 start, Vector3 stop, Vector3[] controls, float moveTakenMs)
        {
            Start = start;
            Stop = stop;
            Controls = controls;
            MoveTakenMs = moveTakenMs;
        }
    }
    
    private void Start()
    {
        Initialize();
        _prePos = _transform.position;
        Easing();
    }

    private void Easing()
    {
        StepPoints curStep = _stepPointsArray[_ankerIndex];
        DOVirtual.Float(0f, 
                1f, 
                curStep.MoveTakenMs / 1000f, 
                value => BezierPoint(curStep, value))
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                _ankerIndex = (_ankerIndex+1)%_stepPointsArray.Length;
                Easing();
            });
    }
    
    private void Update()
    {
        Vector3 curPos = _transform.position;
        DisPos = curPos - _prePos;
        _prePos = curPos;
    }

    private void Initialize()
    {
        Transform ankerParent = _ankers.transform;
        int ankerNum = ankerParent.childCount;
        _stepPointsArray = new StepPoints[ankerNum];
        for (int ai = 0; ai < ankerNum; ai++)
        {
            Transform anker = ankerParent.GetChild(ai);
            int controlNum = anker.childCount;
            Vector3[] controls = new Vector3[controlNum];
            for (int ci = 0; ci < controlNum; ci++)
            {
                controls[ci] = anker.GetChild(ci).position;
            }
            _stepPointsArray[ai] = 
                new StepPoints(anker.position, 
                                    ankerParent.GetChild((ai+1)%ankerNum).position, 
                                    controls,
                                    anker.GetComponent<Anker>().MoveTakenMs);
        }

        _ankerIndex = 0;
    }

    private void BezierPoint(StepPoints stepPoints, float t)
    {
        Vector3 start = stepPoints.Start;
        Vector3 stop = stepPoints.Stop;
        Vector3[] controls = stepPoints.Controls;
        int degree = controls.Length + 1;
        Vector3 pos = Vector3.zero;
        pos += Mathf.Pow(1f-t, degree) * start;
        for (int i = 1; i <= degree-1; i++)
        {
            float weight = Comb(degree, i) * Mathf.Pow(t, i) * Mathf.Pow(1f - t, degree - i);
            pos += weight * controls[i-1];
        }
        pos += Mathf.Pow(t, degree) * stop;
        _transform.position = pos;
    }

    private int Comb(int n, int k) // calc nCk
    {
        if (n < k) { return 0; }

        if (n == k) { return 1; }

        int x = 1;
        for (int i = 0; i < k; i++)
        {
            x = x * (n - i) / (i + 1);
        }

        return x;
    }
}
