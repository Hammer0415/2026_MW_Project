using System.Collections.Generic;
using UnityEngine;

public class hDynamicBone : MonoBehaviour
{
    [System.Serializable]
    public class BoneNode
    {
        public Transform transform;
        public Vector3 position;
        public Vector3 prevPosition;
        public Vector3 localPosition;
        public Quaternion initialLocalRotation; // 초기 로컬 회전 저장
        public float length;

        public BoneNode(Transform t)
        {
            transform = t;
            position = t.position;
            prevPosition = t.position;
            localPosition = t.localPosition;
            initialLocalRotation = t.localRotation; // 시작 시 회전값 기억
        }
    }

    [System.Serializable]
    public class BoneChain
    {
        public List<BoneNode> nodes = new List<BoneNode>();
    }

    [Header("Roots")]
    public List<Transform> rootBones = new List<Transform>();

    [Header("Setup")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);
    [Range(0, 1)] public float damping = 0.1f;
    [Range(0, 1)] public float elasticity = 0.05f;

    private List<BoneChain> chains = new List<BoneChain>();
    private Vector3 lastObjectPosition;

    void Awake()
    {
        foreach (var root in rootBones)
        {
            if (root == null) continue;
            BoneChain newChain = new BoneChain();
            SetupNodes(root, newChain);
            chains.Add(newChain);
        }
        lastObjectPosition = transform.position;
    }

    private void SetupNodes(Transform current, BoneChain chain)
    {
        BoneNode node = new BoneNode(current);
        if (current.parent != null)
        {
            node.length = current.localPosition.magnitude;
        }

        chain.nodes.Add(node);
        foreach (Transform child in current) SetupNodes(child, chain);
    }

    void LateUpdate()
    {
        lastObjectPosition = transform.position;

        foreach (var chain in chains)
        {
            ProcessPhysics(chain);
        }
    }

    private void ProcessPhysics(BoneChain chain)
    {
        float dt = Time.deltaTime;
        if (dt == 0) return;

        chain.nodes[0].position = chain.nodes[0].transform.position;

        for (int i = 0; i < chain.nodes.Count - 1; i++)
        {
            var parentNode = chain.nodes[i];
            var childNode = chain.nodes[i + 1];

            parentNode.transform.localRotation = parentNode.initialLocalRotation;

            float dynamicDamping = Mathf.Clamp(damping + (i * 0.02f), 0, 1);
            Vector3 velocity = (childNode.position - childNode.prevPosition) * (1f - dynamicDamping);
            childNode.prevPosition = childNode.position;
            childNode.position += velocity;

            Vector3 targetWorldPos = parentNode.transform.TransformPoint(childNode.localPosition);
            childNode.position = Vector3.Lerp(childNode.position, targetWorldPos, elasticity);

            Vector3 currentBoneDir = parentNode.transform.TransformDirection(childNode.localPosition).normalized;
            Vector3 dir = (childNode.position - parentNode.transform.position).normalized;
            if (dir != Vector3.zero)
            {
                Quaternion swing = Quaternion.FromToRotation(currentBoneDir, dir);
                Quaternion targetRotation = swing * parentNode.transform.rotation;
                parentNode.transform.rotation = Quaternion.Slerp(parentNode.transform.rotation, targetRotation, dt * 25.0f);
            }

            Vector3 finalPos = parentNode.transform.position + (dir * childNode.length);
        }
    }

    void OnDrawGizmos()
    {
        if (chains == null || chains.Count == 0) return;

        foreach (var chain in chains)
        {
            for (int i = 0; i < chain.nodes.Count; i++)
            {
                var node = chain.nodes[i];
                if (node.transform == null) continue;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(node.position, 0.02f);
            }
        }
    }
}