using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

public class XRTargetingFilter : XRBaseTargetFilter
{
    [SerializeField]
    float m_MaxDistance = 10f;

    /// <summary>
    /// The maximum distance from the Interactor.
    /// Any target from this distance will receive a <c>0</c> normalized score.
    /// </summary>
    public float maxDistance
    {
        get => m_MaxDistance;
        set => m_MaxDistance = value;
    }

    static readonly Dictionary<IXRInteractable, float> s_InteractableFinalScoreMap = new Dictionary<IXRInteractable, float>();

    public override void Process(IXRInteractor interactor, List<IXRInteractable> targets, List<IXRInteractable> results)
    {
        try
        {
            results.Clear();

            // Get Evaluator
            (float, IXRInteractable) firstHoverObject = (0.0f, null);
            
            foreach (var interactable in targets)
            {
                var distanceSqr = interactable.GetDistanceSqrToInteractor(interactor);
                var distanceScore = 1f - Mathf.Clamp01(distanceSqr / (m_MaxDistance * m_MaxDistance));

                if (interactable.transform.tag == "HoverOnly")
                {
                    if (distanceScore > firstHoverObject.Item1)
                    {
                        firstHoverObject = (distanceScore, interactable);
                    }
                    continue;
                }

                if (distanceScore >= 0f)
                {
                    results.Add(interactable);
                    s_InteractableFinalScoreMap[interactable] = distanceScore;
                }
            }

            if (firstHoverObject.Item2 is not null)
            {
                results.Add(firstHoverObject.Item2);
                s_InteractableFinalScoreMap[firstHoverObject.Item2] = 0.0f;
            }

            //if (results.Count > 0)
            //{
            //    Debug.Log($"Hand priority filter returned {results[0]} from {targets.Count}");
            //}

            results.Sort(CompareInteractible);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    static int CompareInteractible(IXRInteractable x, IXRInteractable y)
    {
        var xFinalScore = s_InteractableFinalScoreMap[x];
        var yFinalScore = s_InteractableFinalScoreMap[y];
        if (xFinalScore < yFinalScore)
            return 1;
        if (xFinalScore > yFinalScore)
            return -1;

        return 0;
    }
}
