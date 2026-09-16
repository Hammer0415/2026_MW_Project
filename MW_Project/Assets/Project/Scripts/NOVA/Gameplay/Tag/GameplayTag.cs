using System;
using UnityEngine;

[Serializable]
public struct GameplayTag : IEquatable<GameplayTag>
{
    [Header("Tag")]
    [Tooltip("태그명. Ability.Dodge, Combat.BasicAttack처럼 계층적으로 작성한다.")]
    [SerializeField] private string tag;

    public string Tag => tag;

    // 태그 문자열로 GameplayTag를 만든다.
    public GameplayTag(string tag)
    {
        this.tag = tag;
    }

    // 태그 문자열이 비어 있지 않은지 확인한다.
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(tag);
    }

    // 다른 태그와 문자열이 같은지 확인한다.
    public bool Equals(GameplayTag other)
    {
        return tag == other.tag;
    }

    // 오브젝트가 같은 태그인지 확인한다.
    public override bool Equals(object obj)
    {
        return obj is GameplayTag other && Equals(other);
    }

    // 태그 문자열의 해시코드를 반환한다.
    public override int GetHashCode()
    {
        return tag != null ? tag.GetHashCode() : 0;
    }

    // 태그를 문자열로 변환한다.
    public override string ToString()
    {
        return tag;
    }

    public static bool operator ==(GameplayTag left, GameplayTag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GameplayTag left, GameplayTag right)
    {
        return !left.Equals(right);
    }
}
