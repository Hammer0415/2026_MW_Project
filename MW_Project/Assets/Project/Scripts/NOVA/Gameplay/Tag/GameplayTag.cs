using System;
using UnityEngine;

[Serializable]
public struct GameplayTag : IEquatable<GameplayTag>
{
    [Header("Tag")]
    [Tooltip("태그명")]
    [SerializeField] private string tag;

    public string Tag => tag;

    // 태그 지정
    public GameplayTag(string tag)
    {
        this.tag = tag;
    }

    // 태그 여부 확인
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(tag);
    }

    // 태그 일치 확인
    public bool Equals(GameplayTag other)
    {
        return tag == other.tag;
    }

    // 오브젝트의 태그가 일치하는지 확인
    public override bool Equals(object obj)
    {
        return obj is GameplayTag other && Equals(other);
    }

    // 해시코드(객체의 고유 정수값) 가져오기
    public override int GetHashCode()
    {
        return tag != null ? tag.GetHashCode() : 0;
    }

    // 태그를 문자열로 변환
    public override string ToString()
    {
        return tag;
    }

    // 태그 비교
    public static bool operator ==(GameplayTag left, GameplayTag right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(GameplayTag left, GameplayTag right)
    {
        return !left.Equals(right);
    }
}
