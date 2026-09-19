using UnityEngine;

public enum LinkType
{
    Teleport,
    Jump,
    Elevator
}

public class NavLink : MonoBehaviour
{
    public LinkType linkType = LinkType.Jump;
    public float duration = 0.6f;
    public float height = 1.5f;
    public float speed = 3.5f;
}
