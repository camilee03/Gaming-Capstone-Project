using UnityEngine;
using System.Collections.Generic;

public class Cobweb : MonoBehaviour
{
    [SerializeField]
    Material Material;
    [SerializeField]
    Gradient gradient;
    [SerializeField]
    float width;
    [SerializeField]
    float minLength, maxLength, minClosestThreadPoint;
    [SerializeField]
    int threads, connectingThreads;
    List<Vector3> threadEndPoints = new List<Vector3>();
    public bool generate;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Generate()
    {
        generate = false;
        threadEndPoints.Clear();
        LineRenderer newLR = gameObject.AddComponent<LineRenderer>();
        newLR.colorGradient = gradient;
        newLR.material = Material;
        newLR.startWidth = newLR.endWidth = width;
        newLR.positionCount = 0;
        float attempts = 0;
        for (int i = 0; i < threads; i++)
        {
            if (attempts > 1000)
            {
                Debug.LogError("Amount of threads Not Possible!");
                break;
            }
            attempts++;
            RaycastHit hit;
            if (Physics.Raycast(transform.position, new Vector3(Random.Range(-100, 100), Random.Range(-100, 100), Random.Range(-100, 100))/100, out hit, maxLength))
            {
                if (hit.distance > minLength)
                {
                    bool farenoughAway = true;
                    foreach (Vector3 endPoint in threadEndPoints)
                    {
                        if (Vector3.Distance(hit.point, endPoint) < minClosestThreadPoint)
                        {
                            farenoughAway = false;
                            break;
                        }
                    }
                    if (farenoughAway)
                    {
                        threadEndPoints.Add(hit.point);/*
                        newLR.positionCount += 2;
                        newLR.SetPosition(newLR.positionCount - 2, transform.position);
                        newLR.SetPosition(newLR.positionCount - 1, hit.point);*/
                    }
                    else i--;
                }
                else i--;
            }
            else i--;
        }
        Vector3 center = Vector3.zero;
        foreach (Vector3 endPoint in threadEndPoints)
        {
            center += (transform.position - endPoint);
        }
        center = center.normalized;
        List<(Vector3, float)> pointsWithAngles = new List<(Vector3, float)>();
        foreach (Vector3 endPoint in threadEndPoints)
        {
            float angle = Vector3.SignedAngle(transform.position, endPoint, center);
            pointsWithAngles.Add((endPoint, angle));
        }
        pointsWithAngles.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        foreach(var pair in pointsWithAngles)
        {
            Debug.Log(pair);
            newLR.positionCount += 2;
            newLR.SetPosition(newLR.positionCount - 2, transform.position);
            newLR.SetPosition(newLR.positionCount - 1, pair.Item1);
        }
        Debug.DrawRay(transform.position, center * 5, Color.red, 5);
        newLR.positionCount++;
        newLR.SetPosition(newLR.positionCount - 1, transform.position);
        for (int i = 0; i < connectingThreads; i++)
        {
            int thread1 = i % threadEndPoints.Count;
            int thread2 = (i + 1) % threadEndPoints.Count;
            float percentage1 = (float)i / connectingThreads;
            float percentage2 = (float)(i + 1) / connectingThreads;
            Vector3 thread1Pos = threadEndPoints[thread1] + (transform.position - threadEndPoints[thread1]) * percentage1;
            Vector3 thread2Pos = threadEndPoints[thread2] + (transform.position - threadEndPoints[thread2]) * percentage2;
            newLR.positionCount += 2;
            newLR.SetPosition(newLR.positionCount - 2, thread1Pos);
            newLR.SetPosition(newLR.positionCount - 1, thread2Pos);
        }
    }
    private void Update()
    {
        if (generate)
        {
            Destroy(gameObject.GetComponent<LineRenderer>());
            Generate();
        }
    }
}
