using UnityEngine;

// https://www.geeksforgeeks.org/java/implementing-our-own-hash-table-with-separate-chaining-in-java/
//https://www.youtube.com/watch?v=FsfRsGFHuv4v
//https://docs.unity3d.com/Packages/com.unity.collections@0.7/api/Unity.Collections.NativeMultiHashMap-2.html
//https://www.scribd.com/document/420189304/Hashing
//https://docs.unity3d.com/550/Documentation/ScriptReference/Hashtable.html
//https://learn.microsoft.com/en-us/dotnet/api/system.collections.hashtable?view=net-10.0&redirectedfrom=MSDN
//https://www.youtube.com/watch?v=qtlt_8GPmPk
//https://catlikecoding.com/unity/tutorials/pseudorandom-noise/hashing/
public class HashNode<K, V>
{
    public K Key;
    public V Value;
    public int HashCode;       // stored hash code — avoids recomputing on resize
    public HashNode<K, V> Next; // reference to the next node in the chain

    public HashNode(K key, V value, int hashCode)
    {
        Key = key;
        Value = value;
        HashCode = hashCode;
        Next = null;
    }
}
