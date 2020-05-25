using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
* Stuff left to do:
* Determine burned area.
* Set maximum range for affected skin area.
*/

/**
* Ways to optimise! 
* Change the pixel setting into a seperate function, 
* provide the alpha after it has been determined; the pixel is burned, or semi-burned. 
* With this method you don't have to go through every single pixel a second time, and don't have to store all data for which pixel is burned (only the adapted values of all pixels).
* 
* Ways To clean up!
* PUT STUFF IN INDIVIDUAL FUNCTIONS INSTEAD OF IN GENERATE!!
* 
*/

public class BurnGeneration : MonoBehaviour
{
    [SerializeField]
    Material 
        burn1, // The different burn Materials.
        burn2,
        burn3;

    [SerializeField]
    float
        _burnAlpha, // The transparency of the burnwound.
        semiBurnAreaMultiplier, // The area around the wound that is affected, though not completely burned.
        maxFacingDeviation, // The max difference between the burn angle, and the facing of the triangle.
        minBurnDistance; // The minimal distance a semiburned pixel can have from the burned ones.

    [SerializeField]
    int textureIndex;

    // The data of the character.
    private Vector2[] uv;
    private Vector3[] vertices;
    private Vector3[] normals;
    private int[] triangles;

    // The original Rotation.
    private Quaternion rotation;
    // Data of the burn location.
    private Vector3 burnLocation = new Vector3();

    [Space]

    // Contains data considering the total burned area.
    public float totalSkinIndex = 0; // The total amount of pixels the skin contains.
    public float burnedSkinIndex = 0; // The total amount of pixels considered burned.
    public float semiBurnedSkinIndex = 0; // The total amount of pixels affected, though not burned.

    private int tIndex = 0; // The index of the to be changed texture.

    private PatientBehaviour patient;
    
	// Use this for initialization
	void Start ()
    {
        // Reset the rotation to let it make sense for me.
        rotation = transform.rotation;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        
        burnLocation = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(0, 1f)).normalized;
        burnLocation += new Vector3(0, Random.Range(0.3f, 0.7f), 0);
        burnLocation += transform.position;

        //GameObject bl = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //bl.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        //bl.transform.position = burnLocation;

        // Gets all the mesh' info.
        //Mesh characterMesh = GetComponent<MeshFilter>().mesh;
        Mesh characterMesh = GetComponent<MeshFilter>().mesh;
        uv = characterMesh.uv;
        triangles = characterMesh.triangles;
        vertices = characterMesh.vertices;
        normals = characterMesh.normals;

        patient = this.GetComponent<PatientBehaviour>();
        Generate();
        
        Debug.Log(Time.realtimeSinceStartup + " Generation Finish");
        // Set back the rotation to properly display the thing.
        transform.rotation = rotation;
    }
	
    private Vector3 GetTriangleFacing(int i)
    {
        // Calculates the severity of the wound.
        // The position of the triangle
        Vector3 worldPosition = transform.TransformPoint((vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3);
        // The direction towards the burnLocation from the triangle.
        Vector3 direction = (burnLocation - worldPosition).normalized;
        // The normal direction of the Triangle.
        Vector3 facing = new Vector3(
            (normals[triangles[i]].x + normals[triangles[i + 1]].x + normals[triangles[i + 2]].x),
            (normals[triangles[i]].y + normals[triangles[i + 1]].y + normals[triangles[i + 2]].y),
            (normals[triangles[i]].z + normals[triangles[i + 1]].z + normals[triangles[i + 2]].z)).normalized;

        // The difference in angle between the normal and the direction.
        Vector3 angle = direction - facing;

        return angle;
    }

    /// <summary>
    /// Changes the model's texture based of the burn severity.
    /// </summary>
    private void Generate()
    {
        // Both textures.
        Texture2D burnTexture = (burn1.GetTexture("_MainTex") as Texture2D);
        tIndex = (GetComponent<SkinnedMeshRenderer>().materials.Length > 1) ? textureIndex : 0; // Determines whether the gameObject is used for testing, or gameplay.
        Texture2D skinTexture = (GetComponent<SkinnedMeshRenderer>().materials[tIndex].GetTexture("_MainTex") as Texture2D);
        
        // The newly generated texture.
        Color32[] newTexture = new Color32[skinTexture.GetPixels().Length];

        // The pixels of both original colors.
        Color32[] burnColors = burnTexture.GetPixels32();
        Color32[] skinColors = skinTexture.GetPixels32();

        // Contains pixels considered burned.
        bool[] burnedPixel = new bool[skinColors.Length];
        // Contains the alpha value of the pixels connecting to the burned pixel.
        float[] semiBurnedPixel = new float[skinColors.Length];
        //Debug.Log(triangles.kLength);
        // Checks if the pixels is inside a burned triangle / area.
        //int loops = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            // Calculates the severity of the wound.
            // The position of the triangle
            Vector3 worldPosition = transform.TransformPoint((vertices[triangles[i]] + vertices[triangles[i + 1]] + vertices[triangles[i + 2]]) / 3);
            // The direction towards the burnLocation from the triangle.
            Vector3 direction = (burnLocation - worldPosition).normalized;
            // The normal direction of the Triangle.
            Vector3 facing = new Vector3(
                (normals[triangles[i]].x + normals[triangles[i + 1]].x + normals[triangles[i + 2]].x),
                (normals[triangles[i]].y + normals[triangles[i + 1]].y + normals[triangles[i + 2]].y),
                (normals[triangles[i]].z + normals[triangles[i + 1]].z + normals[triangles[i + 2]].z)).normalized;

            // The difference in angle between the normal and the direction.
            Vector3 angle = direction - facing;


            // Checks if the current triangle faces the same (+ deviation) direction as the burn.
            if ( /*i <= 10500 && i > 10499*/ Mathf.Abs(angle.x) < maxFacingDeviation && Mathf.Abs(angle.y) < maxFacingDeviation && Mathf.Abs(angle.z) < maxFacingDeviation /*true*/ )
            {
                //Debug.DrawLine(worldPosition, worldPosition + facing * 0.1f, Color.red,10);
                //Debug.DrawLine(worldPosition, worldPosition + direction * 0.1f, Color.red, 10);

                //loops++;
                // The three coordinates of the triangle.
                Vector2 p1 = uv[triangles[i]];
                Vector2 p2 = uv[triangles[i + 1]];
                Vector2 p3 = uv[triangles[i + 2]];
                Vector2 avg = (p1 + p2 + p3) / 3; // The Centre of the triangle.
                
                // The three coordinates of the non-extisting triangle
                Vector2 sP1 = p1 * semiBurnAreaMultiplier;
                Vector2 sP2 = p2 * semiBurnAreaMultiplier;
                Vector2 sP3 = p3 * semiBurnAreaMultiplier;
                Vector2 savg = (sP1 + sP2 + sP3) / 3; // The centre of the non existent triangle.

                Vector2 offSet = avg - savg; // The difference between the two centres.

                // re-Sets the non-existing points to overlap with the existing.
                sP1 += offSet; 
                sP2 += offSet;
                sP3 += offSet;

                // Creates parameters of the square.
                Vector2 horizontal = new Vector2(
                    Mathf.Max(new float[3]
                    {
                        sP1.x,
                        sP2.x,
                        sP3.x
                    }), // Top.
                    Mathf.Min(new float[3]
                    {
                        sP1.x,
                        sP2.x,
                        sP3.x
                    })); // Bottom
                
                Vector2 vertical = new Vector2(
                    Mathf.Max(new float[3]
                    {
                        sP1.y,
                        sP2.y,
                        sP3.y
                    }), // Right
                    Mathf.Min(new float[3]
                    {
                        sP1.y,
                        sP2.y,
                        sP3.y
                    })); // Left

                // Converts both values from uv values to actual pixel location.
                // This was preferably done when creating the original points for the triangle, though this does not seem to work for some reason.
                float multiplier = Mathf.Sqrt(burnedPixel.Length);
                horizontal *= multiplier;
                vertical *= multiplier;
                p1 *= multiplier;
                p2 *= multiplier;
                p3 *= multiplier;
                sP1 *= multiplier;
                sP2 *= multiplier;
                sP3 *= multiplier;
                //Debug.Log("");
                // Goes through the square inside the texture.
                for (int w = (int)horizontal.y; w < horizontal.x /*&& (horizontal != Vector2.zero && vertical != Vector2.zero)*/; w++)
                {
                    for (int h = (int)vertical.y; h < vertical.x; h++)
                    {
                        int index = (int)(w + h * Mathf.Sqrt(newTexture.Length)); // The index of the pixel inside a linear Array. (Due to this, the code will only work with a square texture though)
                        Vector2 pixel = new Vector2(w, h); // The pixel we are assessing at this moment.
                        if (index >= 0 && index < burnedPixel.Length) // Safety measures (to ensure there will not be an 'out of bounds' error)
                        {
                            if (PointInTriangle(p1, p2, p3, pixel)) // Checks if the pixel is in the inner triangle (completely burned).
                            {
                                burnedPixel[index] = true;
                            }
                            // Checks if semi-burned area.
                            else if (PointInTriangle(sP1, sP2, sP3, pixel)) // Checks if the pixel is in the outer triangle (semi-burned).
                            {
                                // This value is divided by the width of the texture. This is the maximum distance between two pixels, and thus will always return a value between 0 and 1.
                                float alpha = 1 - (Vector2.Distance(pixel, savg) / Mathf.Sqrt(skinColors.Length));
                                if (alpha > minBurnDistance)
                                {
                                    alpha -= minBurnDistance;
                                    alpha *= 1 / minBurnDistance;
                                    semiBurnedPixel[index] = alpha;
                                }
                            }
                        }
                    }
                }
            }
        }
        
        float bp = 0;
        float sbp = 0;
        // Calculates the burned percentage area.
        for (int i = 0; i < burnedPixel.Length; i++)
        {
            if (burnedPixel[i]) bp++;
            else if (semiBurnedPixel[i] > 0) sbp++;
        }
        semiBurnedSkinIndex = sbp / burnedPixel.Length;
        burnedSkinIndex = bp / burnedPixel.Length;
        totalSkinIndex = burnedPixel.Length;
        
        for (int i = 0; i < burnedPixel.Length; i++)
        {
            // Adjusts the alpha ratio between burn and skin.
            float burnAlpha = (burnedPixel[i]) ? _burnAlpha : ((semiBurnedPixel[i] > 0.2f) ? semiBurnedPixel[i] : 0) * _burnAlpha;
            float skinAlpha = 1 - burnAlpha;

            if (skinAlpha <= 0.8f)
            {
                // The BurnIndex.
                int bi = (((i / skinTexture.width) % burnTexture.height) * burnTexture.width) + ((i % skinTexture.width) % burnTexture.width);
            
                // Converts the skin and burn texture into one.
                Vector4 newColor = new Vector4(
                (burnColors[bi].r * burnAlpha) +
                    (skinColors[i].r * skinAlpha),
                (burnColors[bi].g * burnAlpha) +
                    (skinColors[i].g * skinAlpha),
                (burnColors[bi].b * burnAlpha) +
                    (skinColors[i].g * skinAlpha),
                1);

                // vector4 is converted to actual colour.
                newTexture[i] = new Color32(
                    System.Convert.ToByte((newColor.x < byte.MinValue) ? byte.MinValue : ((newColor.x > byte.MaxValue) ? byte.MaxValue : newColor.x)), // Boundaries are checked to prevent it from potentially providing a value that is out of range.
                    System.Convert.ToByte((newColor.y < byte.MinValue) ? byte.MinValue : ((newColor.y > byte.MaxValue) ? byte.MaxValue : newColor.y)),
                    System.Convert.ToByte((newColor.z < byte.MinValue) ? byte.MinValue : ((newColor.z > byte.MaxValue) ? byte.MaxValue : newColor.z)),
                    System.Convert.ToByte((newColor.w < byte.MinValue) ? byte.MinValue : ((newColor.w > byte.MaxValue) ? byte.MaxValue : newColor.w)));
            }
            else
            {
                newTexture[i] = skinColors[i];
            }
        }

        // The new texture is set.
        skinTexture.SetPixels32(newTexture);
        skinTexture.Apply();
        
        // New material is applied to character.
        GetComponent<SkinnedMeshRenderer>().materials[tIndex].SetTexture("_MainTex", skinTexture);
        Calculate(skinTexture, burnedPixel, semiBurnedPixel);

        //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.GetComponent<SkinnedMeshRenderer>().material = GetComponent<SkinnedSkinnedMeshRenderer>().materials[tIndex];

    }
    
    /// <summary>
    /// Calculates the burnpercentage index.
    /// </summary>
    private void Calculate(Texture2D txtr, bool[] burnedPixel, float[] semiBurnedPixel)
    {
        Color32[] clr = (GetComponent<SkinnedMeshRenderer>().materials[tIndex].GetTexture("_MainTex") as Texture2D).GetPixels32();
        for (int i = 0; i < clr.Length; i++)
        {
            //if (clr[i].a > 0)
            //{
                if (burnedPixel[i])
                {
                    burnedSkinIndex++;
                }
                else if (semiBurnedPixel[i] > 0)
                {
                    semiBurnedSkinIndex += semiBurnedPixel[i];
                }
            //}
        }
        

        burnedSkinIndex /= burnedPixel.Length;
        semiBurnedSkinIndex /= burnedPixel.Length;

        burnedSkinIndex *= 100;
        semiBurnedSkinIndex *= 100;
        patient.setBurnArea(burnedSkinIndex);
      
    }

    private Vector2 Highest(Vector2 first, Vector2 second)
    {
        if (first.y > second.y)
            return first;
        else
            return second;
    }
    private Vector2 Lowest(Vector2 first, Vector2 second)
    {
        if (first.y < second.y)
            return first;
        else
            return second;
    }

    /// <summary>
    /// Calculates the surface size of the triangle provided.
    /// </summary>
    private float GetSurfaceSize(Vector2 p1, Vector2 p2, Vector2 p3)
    {

        // Stuff to calculate burned area.
        Vector2[] trianglepoints = new Vector2[3];
        trianglepoints[0] = Highest(Highest(p1, p2), p3);
        if (trianglepoints[0] == p1)
        {
            trianglepoints[1] = Highest(p2, p3);
            trianglepoints[2] = Lowest(p2, p3);
        }
        else if (trianglepoints[0] == p2)
        {
            trianglepoints[1] = Highest(p1, p3);
            trianglepoints[2] = Lowest(p1, p3);
        }
        else
        {
            trianglepoints[1] = Highest(p1, p2);
            trianglepoints[2] = Lowest(p1, p2);
        }

        float[] triangleBoundaries = new float[4]; // minx, maxx, miny, maxy
        triangleBoundaries[0] = Mathf.Min(trianglepoints[0].x, trianglepoints[1].x, trianglepoints[2].x);
        triangleBoundaries[1] = Mathf.Max(trianglepoints[0].x, trianglepoints[1].x, trianglepoints[2].x);
        triangleBoundaries[2] = Mathf.Min(trianglepoints[0].y, trianglepoints[1].y, trianglepoints[2].y);
        triangleBoundaries[3] = Mathf.Max(trianglepoints[0].y, trianglepoints[1].y, trianglepoints[2].y);

        //// The Surface size of total used area, and outside of triangle.
        float sq = Mathf.Abs(triangleBoundaries[0] - triangleBoundaries[1]) * Mathf.Abs(triangleBoundaries[2] - triangleBoundaries[3]);
        float t12 = (Mathf.Abs(trianglepoints[0].x - trianglepoints[1].x) * Mathf.Abs(trianglepoints[0].y - trianglepoints[1].y)) / 2;
        float t23 = (Mathf.Abs(trianglepoints[1].x - trianglepoints[2].x) * Mathf.Abs(trianglepoints[1].y - trianglepoints[2].y)) / 2;
        float t13 = (Mathf.Abs(trianglepoints[0].x - trianglepoints[2].x) * Mathf.Abs(trianglepoints[0].y - trianglepoints[2].y)) / 2;

        return sq - t12 - t23 - t13;
    }
    /// <summary>
    /// Returns true if the pixel is inside the proveded triangle.
    /// Using the 'Parametric Equations System'.
    /// </summary>
    private bool PointInTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 pixel)
    {
        float denominator = (p1.x * (p2.y - p3.y) + p1.y * (p3.x - p2.x) + p2.x * p3.y - p2.y * p3.x);
        float t1 = (pixel.x * (p3.y - p1.y) + pixel.y * (p1.x - p3.x) - p1.x * p3.y + p1.y * p3.x) / denominator;
        float t2 = (pixel.x * (p2.y - p1.y) + pixel.y * (p1.x - p2.x) - p1.x * p2.y + p1.y * p2.x) / -denominator;
        float s = t1 + t2;

        if (0 <= t1 && t1 <= 1 && 0 <= t2 && t2 <= 1 && s <= 1)
        {
            return true;
        }
        return false;
    }
}