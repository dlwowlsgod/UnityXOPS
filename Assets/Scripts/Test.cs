using UnityEngine;
using UnityXOPS;
using System.IO;

[DefaultExecutionOrder(100)]
public class Test : MonoBehaviour
{
    private void Start()
    {
        var path = Path.Combine(Application.streamingAssetsPath,
            "data", "map10", "temp.bd1");
        BlockDataReader.Instance.LoadBD1(path);
    }
}
