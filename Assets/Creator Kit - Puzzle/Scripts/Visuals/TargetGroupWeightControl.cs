using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;


public class TargetGroupWeightControl : MonoBehaviour
{
    [Header("Weight Control Settings")]
    public float weightDamping = 1f;
    public AnimationCurve speedToWeightCurve = new AnimationCurve(new Keyframe(0.1f, 0f), new Keyframe(1f, 1f, 3f, 3f));
    [Header("Post Processing Settings")]
    public PostProcessVolume postProcessVolume;
    public AnimationCurve averageToFocusDistanceCurve = AnimationCurve.Linear (0f, 0f, 20f, 20f);
    public AnimationCurve rangeToApertureCurve = AnimationCurve.Linear (0f, 0.65f, 6f, 1f);

    Rigidbody FocusTarget;
    Transform Camera;
    CinemachineTargetGroup m_TargetGroup;
    Rigidbody[] m_TargetRigidbodies;
    FloatParameter m_FocusDistanceParameter;
    FloatParameter m_ApertureParameter;

    void Awake ()
    {
        Camera = FindFirstObjectByType<Camera> ().transform;
        m_TargetGroup = GetComponent<CinemachineTargetGroup> ();

        for (int i = 0; i < m_TargetGroup.Targets.Count; i++)
        {
            m_TargetGroup.Targets[i].Weight = i == 0 ? 1f : 0f;
        }
        
        m_TargetRigidbodies = new Rigidbody[m_TargetGroup.Targets.Count];
        for (int i = 0; i < m_TargetRigidbodies.Length; i++)
        {
            m_TargetRigidbodies[i] = m_TargetGroup.Targets[i].Object.GetComponent<Rigidbody> ();
        }

        DepthOfField depthOfField = postProcessVolume.profile.GetSetting<DepthOfField> ();
        m_FocusDistanceParameter = depthOfField.focusDistance;
        m_FocusDistanceParameter.overrideState = true;
        m_ApertureParameter = depthOfField.aperture;
        m_ApertureParameter.overrideState = true;
    }

    void Update ()
    {
        for (int i = 0; i < m_TargetRigidbodies.Length; i++)
        {
            float weight;
            if (FocusTarget == null)
            {
                weight = speedToWeightCurve.Evaluate (m_TargetRigidbodies[i].linearVelocity.magnitude);
            }
            else
            {
                weight = m_TargetRigidbodies[i] == FocusTarget ? 1f : 0f;
            }
            weight = Mathf.Clamp01 (weight);
            m_TargetGroup.Targets[i].Weight = Mathf.MoveTowards (m_TargetGroup.Targets[i].Weight, weight, weightDamping * Time.deltaTime);
        }
        
        m_FocusDistanceParameter.value = averageToFocusDistanceCurve.Evaluate (Camera.InverseTransformPoint(m_TargetGroup.Sphere.position).z);
        m_ApertureParameter.value = rangeToApertureCurve.Evaluate (m_TargetGroup.Sphere.radius * 2f);
    }

    public void ApplySpecificFocus (Rigidbody focusTarget)
    {
        FocusTarget = focusTarget;
    }
}
