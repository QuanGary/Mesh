using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class AirplaneTransformation : MonoBehaviour
{
    // Public variables
    public GameObject camera;
    public GameObject AirplaneSmooth;
    public GameObject AirplaneFlat;

    public GameObject cameraIcon;
    public GameObject lightIcon;
    
    public Material AirplaneDiffuseMat;
    public Material AirplaneMetalMat;
    public Material WireFrameMat;
    public Material GrayDiffuse;
    public Material GrayMetal;
    
    
    public LineRenderer NormalLine;
    public LineRenderer NormalTip;
    public LineRenderer InterpolatedNormalLine;
    public LineRenderer InterpolatedNormalTip;
    public LineRenderer EdgeLine1;
    public LineRenderer EdgeTip1;
    public LineRenderer EdgeLine2;
    public LineRenderer EdgeTip2;

    public AnimationCurve planeAnimationCurve;
    
    public Animator CameraAnimator;
    public Animator LightAnimator;

    
    // Private variables
    private Vector3 iniRotation;
    private List<GameObject> currentAirplaneComponents;
    private List<Mesh> smoothComponents;
    private List<GameObject> flatComponents;
    private LinkedList<GameObject> wireframeComponents;
    
    private bool playPlaneAnimation;
    private float animTime;


    // Start is called before the first frame update
    void Start()
    {
        // Initialize private variables
        iniRotation = AirplaneSmooth.transform.rotation.eulerAngles;
        playPlaneAnimation = false;
        currentAirplaneComponents = new List<GameObject>();
        smoothComponents = new List<Mesh>();
        flatComponents = new List<GameObject>();
        wireframeComponents = new LinkedList<GameObject>();
        foreach (Transform child in AirplaneSmooth.transform)
        {
            currentAirplaneComponents.Add(child.gameObject);
            Mesh curMesh = child.gameObject.GetComponent<MeshFilter>().mesh;
            smoothComponents.Add(deepCopy(curMesh));
            // wireframeComponents.Add(new GameObject("Wireframe"));
        }
        
        foreach (Transform child in AirplaneFlat.transform)
        {
            flatComponents.Add(child.gameObject);
        }
        
        
        //TODO: You can set up the airplane animation here
        // _ First parameter: the time it takes to do 1 full tilt to the left (or right).
        // This means 6 seconds to do both left and right tilts.
        // _ Second parameter: plane tilt angle (in degrees)
        setPlaneAnimation(3, 15); 
        

        
        initialize();
    }

    // Don't have to modify this
    void Update()
    {
        if (playPlaneAnimation)
        {
            AirplaneSmooth.transform.rotation = Quaternion.Euler(AirplaneSmooth.transform.eulerAngles.x, 
                AirplaneSmooth.transform.eulerAngles.y,
                planeAnimationCurve.Evaluate(animTime));
            animTime += Time.deltaTime;
        }

        var input = Input.inputString;
        switch (input)
        {
            case "1": // Smooth Airplane Mat + Wireframe on top
                useSmoothShading();
                applyMaterial(AirplaneMetalMat, WireFrameMat);
                break;
            case "2": // Only Wireframe
                useSmoothShading();
                applyMaterial(null, WireFrameMat);
                break;
            case "3":  // Flat Diffuse Gray
                useFlatShading();
                applyMaterial(GrayDiffuse, null);
                break;
            case "4": // Flat Metal Gray
                useFlatShading();
                applyMaterial(GrayMetal, null);
                break;
            case "5": // Smooth Metal Gray
                useSmoothShading();
                applyMaterial(GrayMetal, null);
                break;
            case "6": // Smooth Airplane Mat
                useSmoothShading();
                applyMaterial(AirplaneMetalMat, AirplaneMetalMat);
                break;
            case " ": // Play/Stop airplane animation
                playPlaneAnimation = !playPlaneAnimation;
                break;
            case "e": // SEGMENT 2: ZOOM IN 1 TRIANGLE, SHOW VECTORS, and LIGHTING CHANGES
                StartCoroutine(showSegment2(3));
                break;
            case "r": // SEGMENT 3: ZOOM IN 2 TRIANGLES, SHOW INTERPOLATED NORMALS.
                StartCoroutine(showSegment3(3));
                break;
        }
    }
    

    // TODO: Modify segment 2's animation
    /// <summary>
    /// Show animation for segment 2: ZOOM IN 1 TRIANGLE, SHOW VECTORS, and LIGHTING CHANGES
    /// </summary>
    /// <param name="iniWaitTime">The time to wait before an action begins</param>
    /// <returns></returns>
    private IEnumerator showSegment2(float iniWaitTime)
    {
        // Reset plane to initial position
        playPlaneAnimation = false;
        AirplaneSmooth.transform.rotation = Quaternion.Euler(iniRotation.x, iniRotation.y, iniRotation.z);
        
        //////// Display FLAT diffuse gray
        useFlatShading();
        applyMaterial(GrayDiffuse, null);
        
        //////// Animation: Zoom in 1 triangle
        yield return new WaitForSeconds(iniWaitTime);
        CameraAnimator.Play("ZoomInTriangleAnim");
        yield return new WaitForSeconds(CameraAnimator.runtimeAnimatorController.animationClips[0].length);
        
        //////// Fade other triangles. Show only 1 triangle
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        foreach (var comp in currentAirplaneComponents)
        {
            Mesh m = comp.GetComponent<MeshFilter>().mesh;
            var updatedVertices = m.triangles;
            for (int i = 0; i < m.triangles.Length; i++)
            {
                if (m.name.StartsWith("Chassis.002"))
                {
                    if (i >= 66 && i <= 68) continue;
                }
                updatedVertices[i] = 0;
            }
            m.triangles = updatedVertices;
        }
        
        // Get the necessary vertices position of the triangle
        Mesh mesh = currentAirplaneComponents[0].GetComponent<MeshFilter>().mesh;
        var vertices = mesh.vertices;
        var p1 = currentAirplaneComponents[0].transform.TransformPoint(vertices[62]);
        var p2 = currentAirplaneComponents[0].transform.TransformPoint(vertices[65]);
        var p3 = currentAirplaneComponents[0].transform.TransformPoint(vertices[66]);
        var center = (p1 + p2 + p3) / 3;
        
        var ba = p2 - p1;
        var ca = p3 - p1;
        var crossUnnormalized = Vector3.Cross(ba, ca);
        var crossNormalized = Vector3.Normalize(crossUnnormalized);
        
        //////// Draw surface normal
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        List<LineRenderer> surfaceNormal = drawVector(center, crossNormalized, NormalLine, NormalTip);

        //////// Draw edges from vertex
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        disableVector(surfaceNormal);
        
        EdgeLine1.SetPosition(0, p1);
        EdgeLine1.SetPosition(1, p2);
        EdgeTip1.SetPosition(0, p2 + (p2-p1)*0.01f);
        EdgeTip1.SetPosition(1, p2 - (p2-p1)*0.1f);
        
        EdgeLine2.SetPosition(0, p1);
        EdgeLine2.SetPosition(1, p3);
        EdgeTip2.SetPosition(0, p3 + (p3-p1)*0.01f);
        EdgeTip2.SetPosition(1, p3 - (p3-p1)*0.1f);
        
        
        
        //////// Draw unnormalized normal at vertex
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        List<LineRenderer> unnormalizedNormal = drawVector(p1, crossUnnormalized * 0.7f, NormalLine, NormalTip);        
        
        
        //////// Draw normalized normal at vertex
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        disableVector(unnormalizedNormal);
        List<LineRenderer> normalizedNormal = drawVector(p1, crossNormalized, NormalLine, NormalTip);
        
        
        //////// Draw the View vector to show N . V > 0
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        disableEdgeVector(); // remove edge vectors
        CameraAnimator.Play("ShowViewVector"); // change camera view to show View vector
        
        // draw view vector
        var dir = (camera.transform.position - p1).normalized;
        List<LineRenderer> viewVector = drawVector(p1, dir, EdgeLine1, EdgeTip1);

        // Show camera icon
        cameraIcon.SetActive(true);
        cameraIcon.transform.position = p1 + dir * 1.1f;
        
        //////// Move Light
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        lightIcon.SetActive(true);
        LightAnimator.Play("MoveLight2"); // animation is 10s long
        
        //////// Change material (from diffuse gray) to metal gray after 5 seconds (half of "MoveLight2" animation)
        yield return new WaitForSeconds(5);
        applyMaterial(GrayMetal, GrayMetal);
        
        //////// // Zoom out, after 5 more seconds (end of "MoveLight2")
        yield return new WaitForSeconds(5); 
        CameraAnimator.Play("ZoomOut");
        //  Disable vectors and icons
        LightAnimator.enabled = false;
        lightIcon.SetActive(false);
        cameraIcon.SetActive(false);
        disableVector(normalizedNormal);
        disableVector(viewVector);
        
        ////////  Fade in airplane
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        useFlatShading();
    }
    
    // TODO: Modify segment 3's animation
    /// <summary>
    /// Show animation for segment 3: ZOOM IN 2 TRIANGLES, SHOW INTERPOLATED NORMALS.
    /// </summary>
    /// <param name="iniWaitTime">The time to wait before an action begins</param>
    /// <returns></returns>
    private IEnumerator showSegment3(float iniWaitTime)
    {
        // Reset plane to initial position
        playPlaneAnimation = false;
        AirplaneSmooth.transform.rotation = Quaternion.Euler(iniRotation.x, iniRotation.y, iniRotation.z);
        // Use Flat Metal Gray
        useFlatShading();
        applyMaterial(GrayMetal, null);

        //////// Animation: Zoom in 2 triangles
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        CameraAnimator.Play("ZoomInTriangleAnim2");
        yield return new WaitForSeconds(CameraAnimator.runtimeAnimatorController.animationClips[0].length);
        
        //////// Fade out other triangles. Show 2 triangles.
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        foreach (var comp in currentAirplaneComponents)
        {
            Mesh m = comp.GetComponent<MeshFilter>().mesh;
            var updatedVertices = m.triangles;
            for (int i = 0; i < m.triangles.Length; i++)
            {
                if (m.name.StartsWith("Chassis.002"))
                {
                    if (i >= 66 && i <= 68 || i >= 192 && i <= 194) continue;

                }
                updatedVertices[i] = 0;
            }
            m.triangles = updatedVertices;
        }
        
        //////// Draw the vertex normals
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        Mesh mesh = currentAirplaneComponents[0].GetComponent<MeshFilter>().mesh;
        var vertices = mesh.vertices;
        var leftFaceNormal = getFaceNormal(getVertexPos(vertices[62]), getVertexPos(vertices[65]),
            getVertexPos(vertices[66]));
        var rightFaceNormal = getFaceNormal(getVertexPos(vertices[182]), getVertexPos(vertices[183]),
            getVertexPos(vertices[184]));
        
        // vertex 1
        var p1 = getVertexPos(vertices[65]);
        var p1Normal = leftFaceNormal;
        drawVector(p1, p1Normal, NormalLine, NormalTip);
        
        // vertex 2
        var p2 = getVertexPos(vertices[66]);
        var p2Normal = ((leftFaceNormal + rightFaceNormal) / 2).normalized;
        drawVector(p2, p2Normal, NormalLine, NormalTip);
        
        // vertex 3
        var p3 = getVertexPos(vertices[62]);
        var p3Normal = p2Normal;
        drawVector(p3, p3Normal, NormalLine, NormalTip);
        
        // vertex 4
        var p4 = getVertexPos(vertices[184]);
        var p4Normal = rightFaceNormal;
        drawVector(p4, p4Normal, NormalLine, NormalTip);
        

        //////// Draw interpolated normals
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        InterpolatedNormalLine.gameObject.SetActive(true);

        // left interpolated normal
        var interpolatedNormal1Pos = (p1 + p2 + p3) / 3;
        var interpolatedNormal1Dir = ((p1Normal + p2Normal + p3Normal) / 3).normalized;
        drawVector(interpolatedNormal1Pos, interpolatedNormal1Dir, InterpolatedNormalLine, InterpolatedNormalTip);

        // right interpolated normal
        var interpolatedNormal2Pos = (p4 + p2 + p3) / 3;
        var interpolatedNormal2Dir = ((p4Normal + p2Normal + p3Normal) / 3).normalized;
        drawVector(interpolatedNormal2Pos, interpolatedNormal2Dir, InterpolatedNormalLine, InterpolatedNormalTip);
        
        
        //////// Turn on smoother look
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        useSmoothShading();
        foreach (var comp in currentAirplaneComponents)
        {
            Mesh m = comp.GetComponent<MeshFilter>().mesh;
            var updatedVertices = m.triangles;
            for (int i = 0; i < m.triangles.Length; i++)
            {
                if (m.name.StartsWith("Chassis.001"))
                {
                    if (i >= 243 && i <= 245 || i >= 255 && i <= 257) continue;

                }
                updatedVertices[i] = 0;
            }
            m.triangles = updatedVertices;
        }
        
        //////// Move light. Animation is 10s long
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        lightIcon.SetActive(true);
        LightAnimator.enabled = true;
        LightAnimator.Play("MoveLight3");
        
        //////// Disable light icon and normal vectors (after 10 seconds since "MoveLight3" anim is 10s long)
        yield return new WaitForSeconds(10);
        lightIcon.SetActive(false);
        LightAnimator.enabled = false;
        InterpolatedNormalLine.gameObject.SetActive(true);
        foreach(var normal in GameObject.FindGameObjectsWithTag("Normal"))
        {
            normal.SetActive(false);
        }
        
        //////// Fade in rest of model. Current material is smooth metal gray
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        useSmoothShading();
        
        //////// Zoom out
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action 
        CameraAnimator.Play("ZoomOut2");
        
        
        //////// Change current material (smooth metal gray) to default airplane material.
        yield return new WaitForSeconds(iniWaitTime); //TODO: Change initial wait time to begin action
        applyMaterial(AirplaneMetalMat, AirplaneMetalMat);
    }

    

 


    
    
    /////////////////////////////////////////////////////////////////////////////
    /// HELPER FUNCTIONS (don't have to modify these)
    /////////////////////////////////////////////////////////////////////////////
    
    private void initialize()
    {
        lightIcon.SetActive(false);
        cameraIcon.SetActive(false);
        InterpolatedNormalLine.gameObject.SetActive(false);
        disableNormalVector();
        disableEdgeVector();
    }
    private void applyMaterial(Material mat1, Material mat2)
    {
        int i = 0;
        foreach (var component in currentAirplaneComponents)
        {
            Material[] compMats = component.GetComponent<MeshRenderer>().materials;
            if (mat1 == null)
            {
                component.GetComponent<MeshRenderer>().enabled = false;
            }
            else
            {
                component.GetComponent<MeshRenderer>().enabled = true;
            }
            
            if (mat2 != null && mat2.name == "Wireframe")
            {
                if (wireframeComponents.Count >= 10)
                {
                    GameObject toRemove = wireframeComponents.Last.Value;
                    Destroy(toRemove);
                    wireframeComponents.RemoveLast();
                }
                wireframeComponents.AddFirst(new GameObject("Wireframe"));
                GameObject wireframeObject = wireframeComponents.First.Value;
                wireframeObject.transform.SetParent(component.transform);
                wireframeObject.transform.localPosition = Vector3.zero;
                wireframeObject.transform.localScale = new Vector3(1, 1, 1);
                wireframeObject.transform.localRotation = Quaternion.identity;
                Mesh bakedMesh = BakeMesh(component.GetComponent<MeshFilter>().sharedMesh);
                if (wireframeObject.GetComponent<MeshRenderer>() == null)
                {
                    wireframeObject.AddComponent<MeshRenderer>();
                    wireframeObject.AddComponent<MeshFilter>();
                    wireframeObject.GetComponent<MeshFilter>().sharedMesh = deepCopy(bakedMesh);
                }
               
                Material[] mats = wireframeObject.GetComponent<MeshRenderer>().materials;
                mats[0] = mat2;
                wireframeObject.GetComponent<MeshRenderer>().materials = mats;
                
            }
            else
            {
                if (wireframeComponents.Count != 0)
                {
                    GameObject wireframeObject = wireframeComponents.First.Value;
                    Destroy(wireframeObject);
                    wireframeComponents.Remove(wireframeObject);
                }
            }
            compMats[0] = mat1;
            compMats[1] = mat2;
            component.GetComponent<MeshRenderer>().materials = compMats;
        }
    }
    
    private void useSmoothShading()
    {
        for (int i = 0; i < smoothComponents.Count; i++)
        {
            currentAirplaneComponents[i].GetComponent<MeshFilter>().mesh = deepCopy(smoothComponents[i]);
        }
    }
    
    private void useFlatShading()
    {
        for (int i = 0; i < flatComponents.Count; i++)
        {
            var flatComponent = flatComponents[i];
            currentAirplaneComponents[i].GetComponent<MeshFilter>().mesh = flatComponent.GetComponent<MeshFilter>().mesh;
        }
    }
    
    private Vector3 getFaceNormal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        var ba = p2 - p1;
        var ca = p3 - p1;
        return Vector3.Cross(ba, ca).normalized;
    }


    private Vector3 getVertexPos(Vector3 pos)
    {
        return currentAirplaneComponents[0].transform.TransformPoint(pos);
    }


    private List<LineRenderer> drawVector(Vector3 pos, Vector3 normal, LineRenderer normalLine, LineRenderer normalTip)
    {
        var vertexNormal = Instantiate(normalLine);
        var vertexNormalTip = Instantiate(normalTip);
        vertexNormal.SetPosition(0, pos);
        var endPos = pos + normal;
        vertexNormal.SetPosition(1, endPos);
        
        vertexNormalTip.SetPosition(0, endPos + normal*0.02f);
        vertexNormalTip.SetPosition(1, endPos - normal*0.1f);
        return new List<LineRenderer> { vertexNormal, vertexNormalTip };
    }
    
    private void disableVector(List<LineRenderer> vector)
    {
        vector[0].gameObject.SetActive(false);
        vector[1].gameObject.SetActive(false);
    }

    ///  Set the plane animation
    /// 
    /// </summary>
    /// <param name="time">time for 1 full tilt (to the left or to the right)</param>
    /// <param name="tiltAngleDeg">degree to tilt</param>
    private void setPlaneAnimation(float time, float tiltAngleDeg)
    {
        planeAnimationCurve.AddKey(0, 0);
        planeAnimationCurve.AddKey(time / 2f, -tiltAngleDeg);
        planeAnimationCurve.AddKey(time, 0);
        planeAnimationCurve.AddKey(time * 3/2f, tiltAngleDeg);
        planeAnimationCurve.AddKey(time * 2f, 0);
        planeAnimationCurve.postWrapMode = WrapMode.Loop;
    }

    private Mesh deepCopy(Mesh m)
    {
        Mesh newMesh = new Mesh();
        newMesh.name = m.name;
        newMesh.vertices = m.vertices;
        newMesh.normals = m.normals;
        newMesh.uv = m.uv;
        newMesh.triangles = m.triangles;
        newMesh.tangents = m.tangents;
        return newMesh;
    }

    private void disableNormalVector()
    {
        NormalLine.SetPosition(0, new Vector3(-1000,-1000,-1000));
        NormalLine.SetPosition(1, new Vector3(-1000,-1000,-1000));
        NormalTip.SetPosition(0, new Vector3(-1000,-1000,-1000));
        NormalTip.SetPosition(1, new Vector3(-1000,-1000,-1000));
    }
    
    private void disableEdgeVector()
    {
        EdgeLine1.SetPosition(0, new Vector3(-1000,-1000,-1000));
        EdgeLine1.SetPosition(1, new Vector3(-1000,-1000,-1000));
        EdgeTip1.SetPosition(0, new Vector3(-1000,-1000,-1000));
        EdgeTip1.SetPosition(1, new Vector3(-1000,-1000,-1000));
        
        EdgeLine2.SetPosition(0, new Vector3(-1000,-1000,-1000));
        EdgeLine2.SetPosition(1, new Vector3(-1000,-1000,-1000));
        EdgeTip2.SetPosition(0, new Vector3(-1000,-1000,-1000));
        EdgeTip2.SetPosition(1, new Vector3(-1000,-1000,-1000));
    }
    
    private Mesh BakeMesh(Mesh originalMesh)
    {
        var maxVerts = 2147483647;
        var meshNor = originalMesh.normals;
        var meshTris = originalMesh.triangles;
        var meshVerts = originalMesh.vertices;		
        var boneW = originalMesh.boneWeights;		
        var vertsNeeded = meshTris.Length;
        if (vertsNeeded > maxVerts)
        {	
            Debug.LogError("The mesh has so many vertices that Unity could not create it!");
            return null;
        }

        var resultMesh = new Mesh();
        resultMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;		
        var resultVerts = new Vector3[vertsNeeded];
        var resultUVs = new Vector2[vertsNeeded];
        var resultTris = new int[meshTris.Length];
        var resultNor = new Vector3[vertsNeeded];
        var boneWLen = (boneW.Length > 0) ? vertsNeeded : 0;
        var resultBW = new BoneWeight[boneWLen];

        for (var i = 0; i < meshTris.Length; i+=3)
        {
            resultVerts[i] = meshVerts[meshTris[i]];
            resultVerts[i+1] = meshVerts[meshTris[i+1]];
            resultVerts[i+2] = meshVerts[meshTris[i+2]];		
            resultUVs[i] = new Vector2(0f,0f);
            resultUVs[i+1] = new Vector2(1f,0f);
            resultUVs[i+2] = new Vector2(0f,1f);
            resultTris[i] = i;
            resultTris[i+1] = i+1;
            resultTris[i+2] = i+2;
            resultNor[i] = meshNor[meshTris[i]];
            resultNor[i+1] = meshNor[meshTris[i+1]];
            resultNor[i+2] = meshNor[meshTris[i+2]];

            if (resultBW.Length > 0)
            {
                resultBW[i] = boneW[meshTris[i]];
                resultBW[i+1] = boneW[meshTris[i+1]];
                resultBW[i+2] = boneW[meshTris[i+2]];
            }
        }

        resultMesh.vertices = resultVerts;
        resultMesh.uv = resultUVs;
        resultMesh.triangles = resultTris;
        resultMesh.normals = resultNor;
        resultMesh.bindposes = originalMesh.bindposes;
        resultMesh.boneWeights = resultBW;

        return resultMesh;
    }
}
