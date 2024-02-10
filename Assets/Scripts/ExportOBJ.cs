using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class ExportOBJ : MonoBehaviour
{
    public bool adjustUVsForSmoothFilterWrapping = true;
	public bool onlySelectedObjects = true;
	public bool applyPosition = true;
	public bool applyRotation = true;
	public bool applyScale = true;
	public bool generateMaterials = true;
	public bool exportTextures = true;
	public bool splitObjects = true;
	public bool autoMarkTexReadable = false;
	public bool objNameAddIdNum = false;
	string exportPath = "yourfilelocation/testobj.obj";
	private string lastExportFolder;
	private string versionString = "v2.0";

	string MaterialToString(Material m)
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("newmtl " + m.name);
		//add properties
		if (m.HasProperty("_Color"))
		{
			sb.AppendLine("Kd " + m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString());
			if (m.color.a < 1.0f)
			{
				//use both implementations of OBJ transparency
				sb.AppendLine("Tr " + (1f - m.color.a).ToString());
				sb.AppendLine("d " + m.color.a.ToString());
			}
		}

		if (m.HasProperty("_SpecColor"))
		{
			Color sc = m.GetColor("_SpecColor");
			sb.AppendLine("Ks " + sc.r.ToString() + " " + sc.g.ToString() + " " + sc.b.ToString());
		}

		if (exportTextures)
		{
			//diffuse
			string exResult = TryExportTexture("_MainTex", m);
			if (exResult != "false")
			{
				sb.AppendLine("map_Kd " + exResult);
			}
			else
            {               
                sb.AppendLine("map_Kd " + m.name + ".png");
                sb.AppendLine("map_d " + m.name + "_alpha.png");
            }

			//spec map
			exResult = TryExportTexture("_SpecMap", m);
			if (exResult != "false")
			{
				sb.AppendLine("map_Ks " + exResult);
			}

			//bump map
			exResult = TryExportTexture("_BumpMap", m);
			if (exResult != "false")
			{
				sb.AppendLine("map_Bump " + exResult);
			}
		}

		sb.AppendLine("illum 2");
		return sb.ToString();
	}

	string TryExportTexture(string propertyName, Material m)
	{
		if (m.HasProperty(propertyName))
		{
			Texture t = m.GetTexture(propertyName);
		}

		return "false";
	}

	Vector3 MultiplyVec3s(Vector3 v1, Vector3 v2)
	{
		return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
	}

	Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
	{
		return angle * (point - pivot) + pivot;
	}

	private string ConstructOBJString(int index)
	{
		string idxString = index.ToString();
		return idxString + "/" + idxString + "/" + idxString;
	}

	public void OnScriptLoaded()
	{

	}

    // Use this for initialization
    public void ExportScene(string outputPath, string tag)
    {
        //init stuff
        exportPath = Application.dataPath + "/../MapData/Maps/" + outputPath + ".obj";
        Debug.Log("exportPath = " + exportPath);
        Dictionary<string, bool> materialCache = new Dictionary<string, bool>();
        var exportFileInfo = new System.IO.FileInfo(exportPath);
        lastExportFolder = exportFileInfo.Directory.FullName;
        string baseFileName = System.IO.Path.GetFileNameWithoutExtension(exportPath);
        //get list of required export things
        MeshFilter[] sceneMeshes;
        if (onlySelectedObjects)
        {
            List<MeshFilter> tempMFList = new List<MeshFilter>();
            foreach (GameObject g in GameObject.FindGameObjectsWithTag(tag))
            {
                MeshFilter f = g.GetComponent<MeshFilter>();
                if (f != null)
                {
                    tempMFList.Add(f);
                }
            }

            sceneMeshes = tempMFList.ToArray();
        }
        else
        {
            sceneMeshes = FindObjectsOfType(typeof(MeshFilter)) as MeshFilter[];
        }

        if (Application.isPlaying)
        {
            foreach (MeshFilter mf in sceneMeshes)
            {
                MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer>();
            }
        }

        //work on export
        StringBuilder sb = new StringBuilder();
        StringBuilder sbMaterials = new StringBuilder();
        sb.AppendLine("# Export of " + Application.loadedLevelName);
        sb.AppendLine("# from Aaro4130 OBJ Exporter " + versionString);
        if (generateMaterials)
        {
            sb.AppendLine("mtllib " + baseFileName + ".mtl");
        }

        float maxExportProgress = (float)(sceneMeshes.Length + 1);
        int lastIndex = 0;
        for (int i = 0; i < sceneMeshes.Length; i++)
        {
            if (sceneMeshes[i] != null && sceneMeshes[i].gameObject.GetComponent<MeshFilter>() != null && sceneMeshes[i].gameObject.GetComponent<MeshRenderer>() != null)
            {
                string meshName = sceneMeshes[i].gameObject.name;
            
            float progress = (float)(i + 1) / maxExportProgress;
            
            //Debug.Log((progress * 100) + "% Level : " + outputPath + " Export");
            MeshFilter mf = sceneMeshes[i];
            
                MeshRenderer mr = sceneMeshes[i].gameObject.GetComponent<MeshRenderer>();
                if (splitObjects)
                {
                    string exportName = meshName;
                    if (objNameAddIdNum)
                    {
                        exportName += "_" + i;
                    }

                    sb.AppendLine("g " + exportName);
                }

                if (mr != null && generateMaterials)
                {
                    Material[] mats = mr.sharedMaterials;
                    for (int j = 0; j < mats.Length; j++)
                    {
                        Material m = mats[j];
                        if (!materialCache.ContainsKey(m.name))
                        {
                            //Debug.Log("OBJ exporting material = " + m.name);
                            materialCache[m.name] = true;
                            sbMaterials.Append(MaterialToString(m));
                            sbMaterials.AppendLine();
                        }
                    }
                }

                //export the meshhh :3
                Mesh msh = mf.sharedMesh;
                int faceOrder = (int)Mathf.Clamp((mf.gameObject.transform.lossyScale.x * mf.gameObject.transform.lossyScale.z), -1, 1);
                //export vector data (FUN :D)!
                foreach (Vector3 vx in msh.vertices)
                {
                    Vector3 v = vx;
                    if (applyScale)
                    {
                        v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale);
                    }

                    if (applyRotation)
                    {
                        v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
                    }

                    if (applyPosition)
                    {
                        v += mf.gameObject.transform.position;
                    }

                    v.z *= -1;
                    sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);
                }

                foreach (Vector3 vx in msh.normals)
                {
                    Vector3 v = vx;
                    if (applyScale)
                    {
                        v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale.normalized);
                    }

                    if (applyRotation)
                    {
                        v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
                    }

                    v.z *= -1;
                    sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);
                }

                foreach (Vector2 v in msh.uv)
                {
                    if (adjustUVsForSmoothFilterWrapping)
                    {
                        sb.AppendLine("vt " + ((v.x * 0.97f) + 0.015f) + " " + ((v.y * 0.97f) + 0.015f));
                    }
                    else
                    {
                        sb.AppendLine("vt " + v.x + " " + v.y);
                    }
                }

                for (int j = 0; j < msh.subMeshCount; j++)
                {
                    if (mr != null && j < mr.sharedMaterials.Length)
                    {
                        string matName = mr.sharedMaterials[j].name;
                        sb.AppendLine("usemtl " + matName);
                    }
                    else
                    {
                        sb.AppendLine("usemtl " + meshName + "_sm" + j);
                    }

                    int[] tris = msh.GetTriangles(j);
                    for (int t = 0; t < tris.Length; t += 3)
                    {
                        int idx2 = tris[t] + 1 + lastIndex;
                        int idx1 = tris[t + 1] + 1 + lastIndex;
                        int idx0 = tris[t + 2] + 1 + lastIndex;
                        if (faceOrder < 0)
                        {
                            sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
                        }
                        else
                        {
                            sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
                        }
                    }
                }

                lastIndex += msh.vertices.Length;
            }
        }

        //write to disk
        System.IO.File.WriteAllText(exportPath, sb.ToString());
        if (generateMaterials)
        {
            System.IO.File.WriteAllText(exportFileInfo.Directory.FullName + "\\" + baseFileName + ".mtl", sbMaterials.ToString());
        }
        Debug.Log("Done Exporting " + baseFileName + ".obj!");
    }
}