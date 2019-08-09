using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ParticleSystemRenderer))]
[ExecuteInEditMode]
[CanEditMultipleObjects]
public class FastSoftParticle : MonoBehaviour
{
    public float m_BlendDepth = 1;

    public LayerMask m_CollisionLayerMask = -1;

    public enum Direction
    {
        Down,
        ScreenDown,
        ScreenHorizontal,
        CameraForward,
        Velocity
    };
    public Direction m_CollisionDirection = Direction.Down;

    public enum CollisionDetection
    {
        OnlyOnce,
        Always,
        AlwaysPerParticle,
    };

    public CollisionDetection m_CollisionDetection = CollisionDetection.Always;

    public ParticleSystemCustomData m_CustomDataType = ParticleSystemCustomData.Custom1;

    ParticleSystemRenderer m_ParticleSystemRenderer;
    ParticleSystem m_ParticleSystem;
    Vector4 m_Plane;
    int m_CheckOnce;

    private void OnEnable()
    {
        m_ParticleSystem = GetComponent<ParticleSystem>();
        m_ParticleSystemRenderer = GetComponent<ParticleSystemRenderer>();

#if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
#endif
        if (m_ParticleSystem == null || m_ParticleSystemRenderer == null)
            enabled = false;
        m_CheckOnce = 0;
    }

#if UNITY_EDITOR
    void OnEdited()
    {
        m_CheckOnce = 0;
    }
#endif

    Vector3 GetDirection()
    {
        if (m_CollisionDirection == Direction.ScreenDown)
            return -Camera.main.transform.up;
        if (m_CollisionDirection == Direction.CameraForward)
            return Camera.main.transform.forward;
        if (m_CollisionDirection == Direction.ScreenHorizontal)
        {
            Vector3 dir = transform.position - Camera.main.transform.position;
            dir.y = 0;
            return dir;
        }
        return Vector3.down;
    }
#if UNITY_EDITOR
    List<Vector3> m_CollisionPair = new List<Vector3>();
#endif
    void UpdateCustomVertex()
    {
        List<Vector4> m_CustomData = new List<Vector4>();
        m_ParticleSystem.GetCustomParticleData(m_CustomData, m_CustomDataType);

        if (m_CustomData.Count == 0)
        {
#if UNITY_EDITOR
            m_CollisionPair.Clear();
#endif
            m_CheckOnce = 0;
            return;
        }

        Vector3 dir = GetDirection();

        RaycastHit hit;
        if (m_CollisionDetection == CollisionDetection.AlwaysPerParticle)
        {
#if UNITY_EDITOR
            m_CollisionPair.Clear();
#endif
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[m_ParticleSystem.particleCount];
            int count = m_ParticleSystem.GetParticles(particles);
            Vector4 noblend = new Vector4(0, 0, 0, 1);
            for (int i = 0; i < count; i++)
            {
                if (m_CollisionDirection == Direction.Velocity)
                    dir = particles[i].velocity.normalized;

                float size = Vector3.Magnitude(particles[i].GetCurrentSize3D(m_ParticleSystem));
                Vector3 position = (m_ParticleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local ? transform.TransformPoint(particles[i].position) : particles[i].position) - dir * size;
                if (m_BlendDepth > 0 && Physics.Raycast(new Ray(position, dir), out hit, size * 2, m_CollisionLayerMask))
                {
#if UNITY_EDITOR
                    m_CollisionPair.Add(hit.point + hit.normal * m_BlendDepth);
                    m_CollisionPair.Add(hit.point);
#endif
                    //Vector3 n = -dir * (1.0f / m_BlendDepth);
                    Vector3 n = hit.normal * (1.0f / m_BlendDepth);
                    m_CustomData[i] = new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, hit.point));
                }
                else
                    m_CustomData[i] = noblend;
            }
        }
        else
        {
            if (m_CollisionDetection == CollisionDetection.Always || (m_CollisionDetection == CollisionDetection.OnlyOnce && m_CheckOnce++ == 0))
            {
#if UNITY_EDITOR
                m_CollisionPair.Clear();
#endif
                Bounds bounds = m_ParticleSystemRenderer.bounds;
                float size = bounds.size.y * 0.5f;
                Vector3 position = bounds.center - dir * size;
                if (m_BlendDepth > 0 && Physics.Raycast(new Ray(position, dir), out hit, size * 2, m_CollisionLayerMask))
                {
#if UNITY_EDITOR
                    m_CollisionPair.Add(hit.point + hit.normal * m_BlendDepth);
                    m_CollisionPair.Add(hit.point);
#endif
                    //Vector3 n = -dir * (1.0f / m_BlendDepth);
                    Vector3 n = hit.normal * (1.0f / m_BlendDepth);
                    m_Plane = new Vector4(n.x, n.y, n.z, -Vector3.Dot(n, hit.point));
                }
                else
                {
                    m_Plane = new Vector4(0, 0, 0, 1);
                }
            }

            for (int i = 0; i < m_ParticleSystem.particleCount; i++)
                m_CustomData[i] = m_Plane;
        }
        m_ParticleSystem.SetCustomParticleData(m_CustomData, m_CustomDataType);
    }

    private void OnWillRenderObject()
    {
#if UNITY_EDITOR
        if (m_ParticleSystemRenderer == null)
            return;
#endif
#if UNITY_EDITOR
        if (!Application.isPlaying || m_ParticleSystemRenderer.isVisible)
#else
        if (m_ParticleSystemRenderer.isVisible)
#endif
            UpdateCustomVertex();
    }

#if UNITY_EDITOR
    static bool m_ShowCollision = false;

    private void OnDrawGizmos()
    {
        if (m_ShowCollision == true)
        {
            Handles.color = Color.yellow;
            for (int i = 0; i < m_CollisionPair.Count; i += 2)
            {
                float unitsize = WorldSize(10.0f, Camera.current, m_CollisionPair[i + 1]);
                Handles.DrawLine(m_CollisionPair[i + 0], m_CollisionPair[i + 1]);
                Handles.DrawLine(m_CollisionPair[i + 1] + new Vector3(-1f, 0, -1f) * unitsize, m_CollisionPair[i + 1] + new Vector3(1f, 0, 1f) * unitsize);
                Handles.DrawLine(m_CollisionPair[i + 1] + new Vector3(1f, 0, -1f) * unitsize, m_CollisionPair[i + 1] + new Vector3(-1f, 0, 1f) * unitsize);
            }
        }
    }

    static float WorldSize(float screensize, Camera camera, Vector3 p)
    {
        return (!camera.orthographic ? Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * camera.transform.InverseTransformPoint(p).z : camera.orthographicSize) * screensize / camera.pixelHeight;
    }

    [CustomEditor(typeof(FastSoftParticle))]
    public class FakeSoftParticleEditor : Editor
    {
        private void OnEnable()
        {
            if (targets.Length == 1)
            {
                FastSoftParticle _target = (FastSoftParticle)target;
                ParticleSystemRenderer particleSystemRenderer = _target.GetComponent<ParticleSystemRenderer>();

                if (particleSystemRenderer.AreVertexStreamsEnabled(ParticleSystemVertexStreams.Custom1) == false)
                {
                    Undo.RecordObject(_target, "EnableVertexStreams,ParticleSystemVertexStreams.Custom1");
                    particleSystemRenderer.EnableVertexStreams(ParticleSystemVertexStreams.Custom1);
                }
            }
        }
        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                FastSoftParticle _target = (FastSoftParticle)target;
                ParticleSystemRenderer particleSystemRenderer = _target.GetComponent<ParticleSystemRenderer>();
                if (particleSystemRenderer == null)
                {
                    EditorGUILayout.HelpBox("No ParticleSystemRenderer", MessageType.Error);
                }
                else
                {
                    Material m = particleSystemRenderer.sharedMaterial;

                    if (m != null)
                    {
                        GUILayout.BeginVertical(GUI.skin.box);
                        string[] match = new string[] {
                            "Legacy Shaders/Particles/Additive", "Particles/Fast/Additive",
                            "Legacy Shaders/Particles/~Additive-Multiply", "Particles/Fast/~Additive-Multiply",
                            "Legacy Shaders/Particles/Additive (Soft)", "Particles/Fast/Additive (Soft)",
                            "Legacy Shaders/Particles/Alpha Blended", "Particles/Fast/Alpha Blended",
                            "Legacy Shaders/Particles/Anim Alpha Blended", "Particles/Fast/Anim Alpha Blended",
                            "Legacy Shaders/Particles/Blend", "Particles/Fast/Blend",
                            "Legacy Shaders/Particles/Multiply", "Particles/Fast/Multiply",
                            "Legacy Shaders/Particles/Multiply (Double)", "Particles/Fast/Multiply (Double)",
                            "Legacy Shaders/Particles/Alpha Blended Premultiply", "Particles/Fast/Alpha Blended Premultiply",
                            "Particles/Additive", "Particles/Fast/Additive",
                            "Particles/~Additive-Multiply", "Particles/Fast/~Additive-Multiply",
                            "Particles/Additive (Soft)", "Particles/Fast/Additive (Soft)",
                            "Particles/Alpha Blended", "Particles/Fast/Alpha Blended",
                            "Particles/Anim Alpha Blended", "Particles/Fast/Anim Alpha Blended",
                            "Particles/Blend", "Particles/Fast/Blend",
                            "Particles/Multiply", "Particles/Fast/Multiply",
                            "Particles/Multiply (Double)", "Particles/Fast/Multiply (Double)",
                            "Particles/Alpha Blended Premultiply", "Particles/Fast/Alpha Blended Premultiply",
                        };
                        for (int i = 0; i < match.Length; i += 2)
                        {
                            Shader shader = Shader.Find(match[i]);
                            if (shader != null && m.shader == shader)
                            {
                                if (GUILayout.Button("Switch Shader to Fast SoftParticle Shader", GUILayout.Height(30)))
                                {
                                    Undo.RecordObject(m, "SwitchShader");
                                    m.shader = Shader.Find(match[i + 1]);
                                }
                            }
                        }
                        GUILayout.EndVertical();
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            base.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    FastSoftParticle _target = (FastSoftParticle)targets[i];
                    _target.OnEdited();
                }
            }

            GUILayout.Space(8);

            m_ShowCollision = GUILayout.Toggle(m_ShowCollision, "Show Collision Result");
        }
    }
#endif
}