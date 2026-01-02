using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// MPMotionComponent handles all movement for actors, separating movement logic from the Actor class.
/// It supports both direct transform movement (Player) and NavMesh navigation (NPCs).
/// </summary>
public class MPMotionComponent : MonoBehaviour
{
    private NavMeshAgent _agent;
    private MPAttributeComponent _attributeComponent;
    private bool _isPlayer;

    public void Initialize(bool isPlayer, MPAttributeComponent attrComp)
    {
        _isPlayer = isPlayer;
        _attributeComponent = attrComp;
        _agent = GetComponent<NavMeshAgent>();
    }

    public void MoveDirectly(Vector3 direction, float deltaTime)
    {
        if (direction.sqrMagnitude < 0.001f) return;

        float speed = _attributeComponent != null 
            ? _attributeComponent.GetValue(AttributeType.MoveSpeed) 
            : 5f;

        transform.position += direction * (speed * deltaTime);
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    public void SetDestination(Vector3 destination)
    {
        if (_agent == null || !_agent.isOnNavMesh) return;

        if (_agent.isStopped) _agent.isStopped = false;

        float speed = _attributeComponent != null 
            ? _attributeComponent.GetValue(AttributeType.MoveSpeed) 
            : 3.5f;

        _agent.speed = speed;
        _agent.destination = destination;
    }

    public void Stop()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }
    }

    public void Resume()
    {
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
        }
    }

    public void SetNavMeshEnabled(bool enabled)
    {
        if (_agent != null)
        {
            _agent.enabled = enabled;
        }
    }

    public bool IsOnNavMesh => _agent != null && _agent.isOnNavMesh;
}
