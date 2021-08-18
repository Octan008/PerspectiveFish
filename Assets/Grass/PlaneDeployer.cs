using UnityEngine;


public class PlaneDeployer : MonoBehaviour
{
    [SerializeField, Range(1, 50)]
    private int width = 10;
    [SerializeField, Range(1, 50)]
    private int height = 10;
    [SerializeField]
    private Material material;


    private void Awake()
    {
        for(var i = 0; i < this.width; i++)
        {
            for(var j = 0; j < this.height; j++)
            {
                
                // var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.GetComponent<Renderer>().material = this.material;

                plane.transform.parent = transform;
                plane.transform.position = new Vector3(
                    0.1f * 10f * (i - this.width / 2),
                    -5f,
                    0.1f * 10f * (j - this.height / 2)
                );
                plane.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

                var rand = Random.value;
                plane.transform.rotation = Quaternion.Euler(
                    0f,
                    rand <= 0.25f ? 0f : (rand <= 0.5f ? 90f : (rand <= 0.75f ? 180f : 270f)),
                    0f
                );
            }
        }
    }
}
