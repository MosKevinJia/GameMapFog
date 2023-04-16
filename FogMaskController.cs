using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class FogMaskController : MonoBehaviour
{ 
    public Image ui_fog;
    public Image ui_map;

    private Vector2 offset;
    public Vector2 map_size = new Vector2(100, 100);  // ��World�е�ʵ�ʴ�С   ��ͼ���ĵ�Ϊ(0,0,0)

    public Transform target;
    public Image player_mark;
    public bool auto_align_mark;

    public Color fogColor = Color.black;

    public float fogSmoothRadius = 10f;
    public int fogClearRadius = 50;
    public bool MouseScrollWheel = true;
    [Range(0.5f, 3)]
    public float ScaleMin = 0.5f;
    [Range(0.5f, 3)]
    public float ScaleMax = 3f;


    public string save_path = "/fog.png";

    private Texture2D fogMask;
    private int width = 512;
    private int height = 512;
    private Color maskcolorStart;
    private Color maskcolorEnd;
    private Vector2 previousPlayerPosition = Vector2.zero;
    private float ui_map_width;
    private float ui_map_height;
    private ScrollRect sr;
    

    private void Start()
    {
        InitializeFogMask();
        StartCoroutine(UpdateFogMask()); 
        
    }

    private void InitializeFogMask()
    {
        maskcolorStart = new Color(fogColor.r, fogColor.g, fogColor.b, 0);
        maskcolorEnd = new Color(fogColor.r, fogColor.g, fogColor.b, 1);
        ui_map_width = ui_map.rectTransform.rect.width;
        ui_map_height = ui_map.rectTransform.rect.height;
        sr = ui_map.GetComponentInParent<ScrollRect>();

        offset.x = map_size.x * 0.5f;
        offset.y = map_size.y * 0.5f;

        fogMask = new Texture2D(width, height, TextureFormat.RGBA32, false);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                fogMask.SetPixel(x, y, fogColor);
            }
        }
        fogMask.Apply(); 
        ui_fog.sprite = Sprite.Create(fogMask, new Rect(0, 0, width, height), Vector2.zero); 
         
    }

     

    /// <summary>
    /// ��һ��Բ�οհ�����[0 - 1]
    /// </summary>
    /// <param name="vPos_Pct"></param>
    private void DrawCircle(Vector2 vPos_Pct)
    {
        if (vPos_Pct == previousPlayerPosition)
            return;
         
        previousPlayerPosition = vPos_Pct;
        int xPos = (int)(vPos_Pct.x * width);
        int yPos = (int)(vPos_Pct.y * height);
         
        for (int y = yPos - fogClearRadius; y <= yPos + fogClearRadius; y++)
        {
            for (int x = xPos - fogClearRadius; x <= xPos + fogClearRadius; x++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(xPos, yPos));

                    if (distance <= fogClearRadius - fogSmoothRadius)
                    {
                        fogMask.SetPixel(x, y, maskcolorStart);
                    }
                    else if (distance <= fogClearRadius)
                    {
                        Color curcolor = fogMask.GetPixel(x, y);
                        // �����ֵ����
                        float t = (distance - (fogClearRadius - fogSmoothRadius)) / fogSmoothRadius;
                        // ���ݲ�ֵ���Ӽ�����ɫ
                        Color color = Color.Lerp(maskcolorStart, maskcolorEnd, t);
                        Color color2 = new Color(color.r, color.g, color.b, curcolor.a - (1 - color.a));
                        fogMask.SetPixel(x, y, color2);
                    } 

                }
            }
        } 
        fogMask.Apply(); 
    }

    private void LateUpdate()
    {
        if (MouseScrollWheel)
        {
            float mousewheel = Input.GetAxis("Mouse ScrollWheel");
            if (mousewheel != 0)
            {
                if (mousewheel < 0 && ui_map.transform.localScale.x > ScaleMin ||
                    mousewheel > 0 && ui_map.transform.localScale.x < ScaleMax)
                {
                    float s = ui_map.transform.localScale.x + mousewheel;
                    ui_map.transform.localScale = new Vector3(s, s, s);
                }
            }
        }
    }

    private IEnumerator UpdateFogMask()
    {
        while (true && target != null)
        { 
            Vector2 playerPosition = GetPlayerPositionOnMap();
            DrawCircle(playerPosition);
            if (auto_align_mark)
            {
                CenterPlayerMark();
            }
           yield return new WaitForSeconds(0.5f);
        }
    }

    private Vector2 GetPlayerPositionOnMap()
    {
        if (target == null)
            return Vector2.zero;
         
        float x = target.transform.position.x + offset.x;
        float y = target.transform.position.z + offset.y;

        x = x / map_size.x ;
        y = y / map_size.y ;
         
        float mx = x * ui_map_width  ;
        float my = y * ui_map_height  ;
        player_mark.rectTransform.anchoredPosition = new Vector2(mx, my);  
        return new Vector2(x, y); 
    }


    public void CenterPlayerMark()
    {   
        if (sr == null || ui_map == null || player_mark == null) return;

        RectTransform sr_rect = sr.GetComponent<RectTransform>();

        // 2. ����Ŀ��������Content�е����λ��
        Vector2 contentPos = (Vector2)sr.content.InverseTransformPoint(player_mark.rectTransform.position);
        float halfHeight = sr_rect.rect.height * 0.5f;
        float halfWidth = sr_rect.rect.width * 0.5f;

        // 3. ����Ŀ����������λ�ú�ScrollRect�Ĵ�С����Content��λ��
        float x = Mathf.Clamp(contentPos.x - halfWidth, sr.content.rect.xMin, sr.content.rect.xMax - sr_rect.rect.width);
        float y = Mathf.Clamp(contentPos.y - halfHeight, sr.content.rect.yMin, sr.content.rect.yMax - sr_rect.rect.height);

        sr.content.anchoredPosition = new Vector2(-x, -y); 

    }


    public void SaveFog(string file_path)
    {
        if (file_path == "")
        {
            Debug.Log("error file_path: " + file_path);
            return;
        }


        // ������ת��ΪPNG��ʽ
        byte[] pngData = fogMask.EncodeToPNG();

        // ���PNG�����Ƿ�Ϊ��
        if (pngData != null)
        {
            // ��ȡ�ļ�����·��
            string path = Path.Combine(Application.dataPath, file_path);

            // ��PNG����д���ļ�
            File.WriteAllBytes(path, pngData);
            Debug.Log("Texture saved to: " + path);
        }
        else
        {
            Debug.LogError("Failed to convert texture to PNG data.");
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="file_path"></param>
    public void LoadFog(string file_path)
    { 

        // ��ȡ�ļ�·��
        string path = Path.Combine(Application.dataPath, file_path);

        // ����ļ��Ƿ����
        if (File.Exists(path))
        {
            // ��ȡ�ļ��е��ֽ�����
            byte[] pngData = File.ReadAllBytes(path);

            // ����һ���µ�Texture2D����
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // ����PNG���ݵ�����
            if (texture.LoadImage(pngData))
            {
                Debug.Log("Texture loaded from: " + path);
                fogMask = texture;  
                fogMask.Apply();
                ui_fog.sprite = Sprite.Create(fogMask, new Rect(0, 0, width, height), Vector2.zero);
            }
            else
            {
                Debug.LogError("Failed to load texture from: " + path); 
            }
        }
        else
        {
            Debug.LogError("File not found: " + path); 
        }
    }

}
