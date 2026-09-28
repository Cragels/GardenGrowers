using UnityEngine;
using System;
using System.Collections.Generic;

// https://www.geeksforgeeks.org/java/implementing-our-own-hash-table-with-separate-chaining-in-java/
//https://www.youtube.com/watch?v=FsfRsGFHuv4v
//https://docs.unity3d.com/Packages/com.unity.collections@0.7/api/Unity.Collections.NativeMultiHashMap-2.html
//https://www.scribd.com/document/420189304/Hashing
//https://docs.unity3d.com/550/Documentation/ScriptReference/Hashtable.html
//https://learn.microsoft.com/en-us/dotnet/api/system.collections.hashtable?view=net-10.0&redirectedfrom=MSDN
//https://www.youtube.com/watch?v=qtlt_8GPmPk
//https://catlikecoding.com/unity/tutorials/pseudorandom-noise/hashing/

public class HashMap<K, V> 
{
   
    private List<HashNode<K, V>> bucketArray; // bucketArray stores the head node of each chain 

    private int numBuckets;// numBuckets is the current capacity of the bucket array 

    
    private int size; // size is the number of key-value pairs stored

    private const float LoadFactor = 0.7f; //if size/numBuckets exceeds this this will double the table

    public int Count => size;
 

    public HashMap() // Initialises capacity, size and empty chains.
    {
        bucketArray = new List<HashNode<K, V>>();
        numBuckets  = 10;
        size        = 0;
 
        
        for (int i = 0; i < numBuckets; i++) // Create empty chains 
            bucketArray.Add(null);
    }
 
    
    public int GetSize() { return size; }
 
    public bool IsEmpty() { return size == 0; }
 
    private int GetHashCode(K key)
    {
        return key.GetHashCode();
    }
 
    private int GetBucketIndex(K key) // This  method is using the % Modulo to ccomppress  the hash  code into  the hashmaps rang
    {
        int hashCode = GetHashCode(key);
        int index    = hashCode % numBuckets;
 
        index = index < 0 ? index * -1 : index;
        return index;
    }
 
    public V Get(K key) // returns the value for the key 
    {
        // Find head of chain for given key 
        int bucketIndex          = GetBucketIndex(key);
        int hashCode             = GetHashCode(key);
        HashNode<K, V> head      = bucketArray[bucketIndex];
 
        // Search key in chain — mirrors GFG's while loop
        while (head != null)
        {
            if (head.Key.Equals(key) && head.HashCode == hashCode)
                return head.Value;
            head = head.Next;
        }
 
        // If key not found — GFG returns null; we throw since C# generics
        // can't safely return null for value types
        throw new Exception($"Key '{key}' not found in HashMap.");
    }
 
    
   
    public bool Remove(K key)
    {
        // Apply hash function to find index for given key — mirrors GFG comment
        int bucketIndex     = GetBucketIndex(key);
        int hashCode        = GetHashCode(key);
 
        // Get head of chain — mirrors GFG
        HashNode<K, V> head = bucketArray[bucketIndex];
        HashNode<K, V> prev = null;
 
        // Search for key in its chain — mirrors GFG's while loop
        while (head != null)
        {
            // If Key found — mirrors GFG's break condition
            if (head.Key.Equals(key) && hashCode == head.HashCode)
                break;
 
            // Else keep moving in chain — mirrors GFG comment
            prev = head;
            head = head.Next;
        }
 
        // If key was not there — mirrors GFG's null return
        if (head == null)
            return false;
 
        // Reduce size — mirrors GFG
        size--;
 
        // Remove key — mirrors GFG's two cases (prev null = head removal)
        if (prev != null)
            prev.Next = head.Next;
        else
            bucketArray[bucketIndex] = head.Next;
 
        return true;
    }
 
   
    // add() — mirrors GFG's add(K key, V value)
    
    public void Add(K key, V value)
    {
        // Find head of chain for given key — mirrors GFG comment
        int bucketIndex     = GetBucketIndex(key);
        int hashCode        = GetHashCode(key);
        HashNode<K, V> head = bucketArray[bucketIndex];
 
        // Check if key is already present — mirrors GFG's while loop
        while (head != null)
        {
            if (head.Key.Equals(key) && head.HashCode == hashCode)
            {
                head.Value = value;
                return;
            }
            head = head.Next;
        }
 
        // Insert key in chain — mirrors GFG's prepend to head
        size++;
        head = bucketArray[bucketIndex];
        HashNode<K, V> newNode = new HashNode<K, V>(key, value, hashCode);
        newNode.Next           = head;
        bucketArray[bucketIndex] = newNode;
 
        // If load factor goes beyond threshold, then double hash table size
        // Mirrors GFG's load factor check: (1.0 * size) / numBuckets >= 0.7
        if ((1.0f * size) / numBuckets >= LoadFactor)
        {
            // Store old bucket array — mirrors GFG's temp variable
            List<HashNode<K, V>> temp = bucketArray;
            bucketArray = new List<HashNode<K, V>>();
            numBuckets  = 2 * numBuckets;
            size        = 0;
 
            for (int i = 0; i < numBuckets; i++)
                bucketArray.Add(null);
 
            // Rehash all existing entries into the new array
            // Mirrors GFG's nested for/while loop rehash
            foreach (HashNode<K, V> headNode in temp)
            {
                HashNode<K, V> current = headNode;
                while (current != null)
                {
                    Add(current.Key, current.Value);
                    current = current.Next;
                }
            }
        }
    }
 
    
  
    public bool ContainsKey(K key)
    {
        int bucketIndex     = GetBucketIndex(key);
        int hashCode        = GetHashCode(key);
        HashNode<K, V> head = bucketArray[bucketIndex];
 
        while (head != null)
        {
            if (head.Key.Equals(key) && head.HashCode == hashCode)
                return true;
            head = head.Next;
        }
 
        return false;
    }
 
    
    public bool TryGetValue(K key, out V value)
    {
        int bucketIndex     = GetBucketIndex(key);
        int hashCode        = GetHashCode(key);
        HashNode<K, V> head = bucketArray[bucketIndex];
 
        while (head != null)
        {
            if (head.Key.Equals(key) && head.HashCode == hashCode)
            {
                value = head.Value;
                return true;
            }
            head = head.Next;
        }
 
        value = default(V);
        return false;
    }
 
  
    public void Clear()
    {
        bucketArray = new List<HashNode<K, V>>();
        size        = 0;
 
        for (int i = 0; i < numBuckets; i++)
            bucketArray.Add(null);
    }
 
    
    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"HashMap [size={size}, numBuckets={numBuckets}]");
 
        for (int i = 0; i < numBuckets; i++)
        {
            if (bucketArray[i] == null) continue;
 
            sb.Append($"  Bucket[{i}]: ");
            HashNode<K, V> current = bucketArray[i];
 
            while (current != null)
            {
                sb.Append($"({current.Key} -> {current.Value})");
                if (current.Next != null) sb.Append(" -> ");
                current = current.Next;
            }
            sb.AppendLine();
        }
 
        return sb.ToString();
    }
}
