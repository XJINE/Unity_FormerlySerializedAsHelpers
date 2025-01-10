using UnityEngine;
using UnityEngine.Serialization;

public class Sample : MonoBehaviour
{
    // [FormerlySerializedAs("sampleInt")]
    public int sampleInt;
    public SampleData sampleData;

    [System.Serializable]
    public class SampleData
    {
        [FormerlySerializedAs("sampleFloatA")]
        public float sampleFloat;
    }
}