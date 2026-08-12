using UnityEngine;

namespace TikTokLiveGame
{
    public static class ClubSceneBuilder
    {
        private const int FloorLightingLayer = 8;
        private const float DjBoothHeightOffset = 0.85f;

        public static void Build()
        {
            RenderSettings.ambientLight = new Color(0.15f, 0.02f, 0.05f);
            RenderSettings.fog = false;
            RenderSettings.fogColor = new Color(0.04f, 0.005f, 0.01f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.018f;

            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            camera.transform.position = new Vector3(0f, 7.2f, 14.2f);
            camera.transform.LookAt(new Vector3(0f, 1.25f, -1.2f));
            camera.fieldOfView = 45f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.01f, 0.005f, 0.025f);
            if (camera.GetComponent<ClubCameraController>() == null) camera.gameObject.AddComponent<ClubCameraController>();

            // CreateArchitecturalBackdrop(); // Hide acoustic wall and panels

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            string path = @"D:\TIKTOK_LIVE_BAR\LiveAssets\nenamphu.png";
            if (System.IO.File.Exists(path))
            {
                tex.LoadImage(System.IO.File.ReadAllBytes(path));
                tex.filterMode = FilterMode.Trilinear;
                tex.anisoLevel = 8;
                tex.wrapMode = TextureWrapMode.Mirror;
                tex.Apply();
            }
            Shader bgShader = Shader.Find("Unlit/Texture");
            if (bgShader == null) bgShader = Shader.Find("Sprites/Default");
            Material bgMat = new Material(bgShader);
            bgMat.mainTexture = tex;

            float texAspect = (float)tex.width / tex.height;
            float H = 40f;
            float W = H * texAspect;
            float D = W * 3f; // Depth for side walls

            Material centerMat = new Material(bgMat);
            centerMat.mainTextureScale = new Vector2(-1, 1);

            Material rightMat = new Material(bgMat);
            rightMat.mainTextureScale = new Vector2(-3, 1);
            rightMat.mainTextureOffset = new Vector2(-1, 0);

            Material leftMat = new Material(bgMat);
            leftMat.mainTextureScale = new Vector2(3, 1);
            leftMat.mainTextureOffset = new Vector2(-3, 0);
            
            Material ceilingMat = new Material(bgMat);
            ceilingMat.mainTextureScale = new Vector2(-1, -1); // Lật ngược để làm trần nhà phản chiếu
            ceilingMat.mainTextureOffset = new Vector2(0, 1);

            GameObject bgRoot = new GameObject("AmPhuBackdrop_Room");

            GameObject centerQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            centerQuad.name = "AmPhuBackdrop_Center";
            centerQuad.transform.SetParent(bgRoot.transform);
            centerQuad.transform.localPosition = new Vector3(0, -3.0f, -12.5f);
            centerQuad.transform.localScale = new Vector3(W, H, 1);
            centerQuad.transform.localRotation = Quaternion.Euler(0, 180, 0);
            centerQuad.GetComponent<Renderer>().sharedMaterial = centerMat;

            GameObject leftQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            leftQuad.name = "AmPhuBackdrop_Left";
            leftQuad.transform.SetParent(bgRoot.transform);
            leftQuad.transform.localPosition = new Vector3(W / 2f, -3.0f, -12.5f + D / 2f);
            leftQuad.transform.localScale = new Vector3(D, H, 1);
            leftQuad.transform.localRotation = Quaternion.Euler(0, -90, 0);
            leftQuad.GetComponent<Renderer>().sharedMaterial = leftMat;

            GameObject rightQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            rightQuad.name = "AmPhuBackdrop_Right";
            rightQuad.transform.SetParent(bgRoot.transform);
            rightQuad.transform.localPosition = new Vector3(-W / 2f, -3.0f, -12.5f + D / 2f);
            rightQuad.transform.localScale = new Vector3(D, H, 1);
            rightQuad.transform.localRotation = Quaternion.Euler(0, 90, 0);
            rightQuad.GetComponent<Renderer>().sharedMaterial = rightMat;
            
            // Trần nhà phản chiếu (Mirrored Ceiling) để chống hở mảng đen khi camera ngước lên quá cao
            GameObject ceilingQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ceilingQuad.name = "AmPhuBackdrop_Ceiling";
            ceilingQuad.transform.SetParent(bgRoot.transform);
            ceilingQuad.transform.localPosition = new Vector3(0, -3.0f + H / 2f, -12.5f + D / 2f);
            ceilingQuad.transform.localScale = new Vector3(W, D, 1);
            ceilingQuad.transform.localRotation = Quaternion.Euler(90, 180, 0); // Xoay 180 để mặt Quad hướng xuống dưới đất
            ceilingQuad.GetComponent<Renderer>().sharedMaterial = ceilingMat;

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Invisible Dance Floor";
            floor.transform.position = new Vector3(0f, -0.3f, -1f);
            floor.transform.localScale = new Vector3(100f, 0.5f, 100f);
            floor.GetComponent<Renderer>().enabled = false;

            CreateRaisedStage();
            // CreateDjVideoScreen();
            CreateDjBooth();
            CreateDjPerformer();
            // CreateArchitecturalAccentLights();
            CreateCircularTrussRig();
            // CreateLedBarRig();
            CreateStrobes();
            // CreateBackgroundQuad();
            // CreateSmokeMachines();

            // CreateSpeaker(-5.4f);
            // CreateSpeaker(5.4f);

            // CreateMirrorBall();

            // Group club elements and push them further back to create a huge dance space
            GameObject clubRoot = new GameObject("ClubRoot");
            foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.transform.parent == null && go.name != "Invisible Dance Floor" && !go.name.Contains("AmPhuBackdrop") && go.name != "Main Camera" && go != clubRoot)
                {
                    go.transform.SetParent(clubRoot.transform);
                }
            }
            clubRoot.transform.position = new Vector3(0, 0, -12f);

            new GameObject("Club Beat").AddComponent<ClubPulseController>();

            AddLight("Hell Red Light", new Vector3(-6f, 7f, 1f), new Color(1f, 0.1f, 0.1f), 4.8f, 20f);
            AddLight("Hell Orange Light", new Vector3(6f, 7f, 0f), new Color(1f, 0.4f, 0f), 4.6f, 20f);
            AddLight("Stage Demon Light", new Vector3(0f, 8f, -7f), new Color(0.8f, 0f, 0.2f), 5.8f, 23f);
        }

        private static void CreateRaisedStage()
        {
            Material deck = GameMaterials.Lit(new Color(0.04f, 0.01f, 0.015f), 0.22f, 0.32f, new Color(0.012f, 0.003f, 0.004f), 0.045f, 9f);
            Material fascia = GameMaterials.Lit(new Color(0.022f, 0.008f, 0.01f), 0.62f, 0.46f, Color.black);
            CreateLitBox("DJ Stage Deck", new Vector3(0f, 0.2f, -7.3f), new Vector3(13f, 1.1f, 4f), deck);
            CreateLitBox("DJ Stage Front", new Vector3(0f, 0.32f, -5.22f), new Vector3(13.2f, 0.86f, 0.22f), fascia);

            Color cyan = new(1f, 0.1f, 0.1f);
            Color violet = new(0.6f, 0f, 0.2f);
            CreateBox("Stage Fascia Top Strip", new Vector3(0f, 0.7f, -5.085f), new Vector3(12.7f, 0.035f, 0.035f), cyan * 0.08f, cyan * 0.55f);
            CreateBox("Stage Fascia Bottom Strip", new Vector3(0f, -0.02f, -5.085f), new Vector3(12.7f, 0.025f, 0.035f), violet * 0.06f, violet * 0.35f);
            for (int index = 0; index < 18; index++)
            {
                float x = Mathf.Lerp(-6.05f, 6.05f, index / 17f);
                Color glow = index % 2 == 0 ? cyan : violet;
                CreateGlowBulb($"Stage Fascia Pixel {index}", new Vector3(x, 0.34f, -5.075f), 0.035f, glow * 0.72f);
            }

            CreateLitBox("Stage Step High", new Vector3(0f, 0.57f, -4.92f), new Vector3(2.8f, 0.36f, 0.48f), deck);
            CreateLitBox("Stage Step Middle", new Vector3(0f, 0.38f, -4.52f), new Vector3(3.35f, 0.3f, 0.46f), deck);
            CreateLitBox("Stage Step Low", new Vector3(0f, 0.2f, -4.14f), new Vector3(3.9f, 0.25f, 0.44f), deck);
            CreateBox("Step Edge High", new Vector3(0f, 0.76f, -4.67f), new Vector3(2.65f, 0.025f, 0.025f), cyan * 0.08f, cyan * 0.42f);
            CreateBox("Step Edge Middle", new Vector3(0f, 0.54f, -4.28f), new Vector3(3.18f, 0.022f, 0.025f), violet * 0.07f, violet * 0.34f);
        }

        private static void CreateArchitecturalBackdrop()
        {
            Material wall = GameMaterials.Lit(new Color(0.012f, 0.015f, 0.022f), 0.15f, 0.2f, new Color(0.003f, 0.004f, 0.008f), 0.035f, 5f);
            Material metal = GameMaterials.Lit(new Color(0.055f, 0.06f, 0.075f), 0.78f, 0.5f, new Color(0.008f, 0.015f, 0.025f));
            CreateLitBox("Rear Acoustic Wall", new Vector3(0f, 4.65f, -10.35f), new Vector3(15.2f, 9.2f, 0.38f), wall);

            for (int side = -1; side <= 1; side += 2)
            for (int row = 0; row < 4; row++)
            {
                float y = 2.05f + row * 1.55f;
                CreateLitBox($"Acoustic Panel {side}-{row}", new Vector3(side * 5.55f, y, -10.08f), new Vector3(2.2f, 1.18f, 0.14f), GameMaterials.Lit(new Color(0.022f, 0.026f, 0.038f), 0.08f, 0.18f, Color.black, 0.045f, 10f));
                CreateLitBox($"Acoustic Panel Trim {side}-{row}", new Vector3(side * 5.55f, y, -9.99f), new Vector3(2.28f, 1.26f, 0.025f), metal);
                CreateLitBox($"Acoustic Panel Face {side}-{row}", new Vector3(side * 5.55f, y, -9.96f), new Vector3(2.12f, 1.1f, 0.025f), GameMaterials.Lit(new Color(0.018f, 0.021f, 0.032f), 0.04f, 0.12f, Color.black, 0.06f, 14f));
            }

            CreateLitBox("Backdrop Header Beam", new Vector3(0f, 8.35f, -9.95f), new Vector3(14.2f, 0.18f, 0.18f), metal);
            CreateLitBox("Backdrop Left Column", new Vector3(-7.05f, 4.6f, -9.96f), new Vector3(0.22f, 7.55f, 0.22f), metal);
            CreateLitBox("Backdrop Right Column", new Vector3(7.05f, 4.6f, -9.96f), new Vector3(0.22f, 7.55f, 0.22f), metal);
        }

        private static void CreateDjBooth()
        {
            Material chassis = GameMaterials.Lit(new Color(0.028f, 0.01f, 0.01f), 0.72f, 0.5f, new Color(0.007f, 0.002f, 0.002f));
            Material glass = GameMaterials.Lit(new Color(0.035f, 0.01f, 0.02f), 0.25f, 0.78f, new Color(0.035f, 0.006f, 0.018f));
            CreateLitBox("DJ Booth Core", new Vector3(0f, 1.2f + DjBoothHeightOffset, -7.65f), new Vector3(6.5f, 1.35f, 1.25f), chassis);
            CreateLitBox("DJ Booth Inset", new Vector3(0f, 1.18f + DjBoothHeightOffset, -6.99f), new Vector3(5.55f, 0.76f, 0.055f), glass);
            CreateLitBox("DJ Booth Top Cap", new Vector3(0f, 1.92f + DjBoothHeightOffset, -7.55f), new Vector3(6.72f, 0.13f, 1.42f), chassis);

            Color cyan = new(1f, 0.1f, 0.1f);
            Color violet = new(0.6f, 0f, 0.2f);
            CreateBox("DJ Booth Cyan Rail", new Vector3(0f, 1.87f + DjBoothHeightOffset, -6.95f), new Vector3(6.12f, 0.035f, 0.035f), cyan * 0.06f, cyan * 0.52f);
            CreateBox("DJ Booth Violet Rail", new Vector3(0f, 0.54f + DjBoothHeightOffset, -6.95f), new Vector3(5.62f, 0.028f, 0.03f), violet * 0.05f, violet * 0.38f);

            for (int index = 0; index < 17; index++)
            {
                float x = Mathf.Lerp(-2.45f, 2.45f, index / 16f);
                float height = 0.12f + Mathf.Abs(Mathf.Sin(index * 1.37f)) * 0.38f;
                Color glow = index % 3 == 0 ? violet : cyan;
                CreateBox($"Booth Equalizer {index}", new Vector3(x, 1.02f + DjBoothHeightOffset, -6.945f), new Vector3(0.12f, height, 0.025f), glow * 0.035f, glow * 0.3f);
            }

            CreateLitBox("DJ Console", new Vector3(0f, 2.06f + DjBoothHeightOffset, -7.55f), new Vector3(4.9f, 0.16f, 0.86f), chassis);

            CreateTurntable(-1.55f);
            CreateTurntable(1.55f);
            CreateMixer();

            for (int side = -1; side <= 1; side += 2)
            {
                CreateLitBox($"Booth Side Pillar {side}", new Vector3(side * 3.14f, 1.2f + DjBoothHeightOffset, -6.96f), new Vector3(0.13f, 1.2f, 0.08f), chassis);
                CreateBox($"Booth Angle Light {side}", new Vector3(side * 2.62f, 1.18f + DjBoothHeightOffset, -6.92f), new Vector3(0.035f, 0.72f, 0.035f), cyan * 0.05f, cyan * 0.42f);
            }
        }

        private static void CreateDjPerformer()
        {
            // Thay thế DJ 3D bằng Nhân vật chính từ GIF (đã trích xuất thành mảng PNG)
            GameObject performer = GameObject.CreatePrimitive(PrimitiveType.Quad);
            performer.name = "Main Character GIF";
            
            // Xóa Collider
            Object.Destroy(performer.GetComponent<Collider>());
            
            // Đặt vị trí ngay giữa sân khấu DJ
            performer.transform.position = new Vector3(0f, 3.4f, -7.5f); // Đặt lên sàn DJ
            performer.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            performer.transform.localScale = new Vector3(4.5f, 4.5f, 1f); // Kích thước của nhân vật GIF
            
            // Thêm Script GifPlayer để tự động phát
            GifPlayer gifPlayer = performer.AddComponent<GifPlayer>();
            
            string frameDir = System.IO.Path.Combine(Application.dataPath, "../../LiveAssets/DJ_GIF");
            if (!System.IO.Directory.Exists(frameDir))
                frameDir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "LiveAssets/DJ_GIF");
            
            gifPlayer.FrameDirectory = frameDir;
            gifPlayer.FrameRate = 30f; // Tốc độ khung hình mượt mà

            // Ánh sáng chiếu vào nhân vật
            AddDjKeyLight("DJ Warm Key", new Vector3(-2.2f, 4.25f, -5.8f), new Color(1f, 0.55f, 0.38f), 3.2f);
            AddDjKeyLight("DJ Cool Rim", new Vector3(2.4f, 4.55f, -8.7f), new Color(0.22f, 0.55f, 1f), 2.7f);
        }

        private static void AddDjKeyLight(string name, Vector3 position, Color color, float intensity)
        {
            GameObject lightObject = new(name);
            lightObject.transform.position = position;
            lightObject.transform.LookAt(new Vector3(0f, 2.45f, -8.05f));
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.intensity = intensity;
            light.range = 9f;
            light.spotAngle = 46f;
            
            light.innerSpotAngle = 24f;
            light.shadows = LightShadows.Soft;
            light.cullingMask = ~(1 << FloorLightingLayer);
            
            for (int i = 0; i < 4; i++)
            {
                GameObject obj = new($"Disco Light {i}");
                obj.transform.position = new Vector3(-6f + i * 4f, 6.5f, 5f);
                obj.transform.rotation = Quaternion.Euler(50f, 180f, 0f);
                Light l = obj.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 25f;
                l.spotAngle = 60f;
                l.innerSpotAngle = 30f;
                l.intensity = 0f;
                l.shadows = LightShadows.Soft;
                l.cullingMask = -1; 
                DiscoLight disco = obj.AddComponent<DiscoLight>();
                disco.index = i;
                discoLights.Add(disco);
            }
        }

        private static void CreateDjVideoScreen()
        {
            Material bezel = GameMaterials.Lit(new Color(0.018f, 0.02f, 0.028f), 0.82f, 0.56f, new Color(0.004f, 0.007f, 0.012f));
            CreateLitBox("DJ Video Frame", new Vector3(0f, 4.65f, -9.62f), new Vector3(7.38f, 4.28f, 0.2f), bezel);
            GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screen.name = "DJ Video Screen";
            screen.transform.position = new Vector3(0f, 4.65f, -9.48f);
            screen.transform.localScale = new Vector3(6.85f, 3.85f, 0.06f);
            screen.GetComponent<Renderer>().sharedMaterial = GameMaterials.Club(new Color(0.012f, 0.025f, 0.055f), new Color(0.04f, 0.18f, 0.35f));
            screen.AddComponent<DjVideoScreen>();

            Color cyan = new(0.02f, 0.68f, 1f);
            Color violet = new(0.55f, 0.12f, 0.92f);
            CreateBox("Video Screen Top Rail", new Vector3(0f, 6.82f, -9.42f), new Vector3(7.45f, 0.035f, 0.035f), cyan * 0.07f, cyan * 0.5f);
            CreateBox("Video Screen Bottom Rail", new Vector3(0f, 2.48f, -9.42f), new Vector3(7.45f, 0.03f, 0.035f), violet * 0.06f, violet * 0.42f);
            CreateLitBox("Video Screen Left Bezel", new Vector3(-3.52f, 4.65f, -9.4f), new Vector3(0.08f, 4.05f, 0.08f), bezel);
            CreateLitBox("Video Screen Right Bezel", new Vector3(3.52f, 4.65f, -9.4f), new Vector3(0.08f, 4.05f, 0.08f), bezel);
        }

        private static void CreateLedSideArrays()
        {
            Material frame = GameMaterials.Lit(new Color(0.014f, 0.016f, 0.023f), 0.68f, 0.42f, Color.black);
            for (int side = -1; side <= 1; side += 2)
            {
                float panelX = side * 4.72f;
                CreateLitBox($"LED Wall Frame {side}", new Vector3(panelX, 4.7f, -9.28f), new Vector3(1.55f, 4.65f, 0.18f), frame);
                CreateLitBox($"LED Wall Diffuser {side}", new Vector3(panelX, 4.7f, -9.16f), new Vector3(1.38f, 4.48f, 0.035f), GameMaterials.Lit(new Color(0.008f, 0.014f, 0.025f), 0.08f, 0.62f, new Color(0.004f, 0.012f, 0.024f)));
                for (int row = 0; row < 10; row++)
                for (int column = 0; column < 4; column++)
                {
                    Color glow = (row + column) % 3 == 0
                        ? new Color(0.52f, 0.16f, 0.95f)
                        : new Color(0.04f, 0.62f, 1f);
                    float x = panelX + (column - 1.5f) * 0.29f;
                    float y = 2.78f + row * 0.43f;
                    CreateGlowBulb($"LED Wall Diode {side}-{row}-{column}", new Vector3(x, y, -9.12f), 0.055f, glow * 0.62f);
                }
            }
        }

        private static void CreateArchitecturalAccentLights()
        {
            Color cool = new(0.12f, 0.58f, 1f);
            for (int index = 0; index < 12; index++)
            {
                float x = Mathf.Lerp(-6.25f, 6.25f, index / 11f);
                CreateGlowBulb($"Backdrop Header Pin {index}", new Vector3(x, 8.18f, -9.72f), 0.045f, cool * 0.55f);
            }
            for (int index = 0; index < 14; index++)
            {
                float x = Mathf.Lerp(-5.95f, 5.95f, index / 13f);
                Color glow = index % 2 == 0 ? cool : new Color(0.52f, 0.14f, 0.92f);
                CreateGlowBulb($"Stage Footlight {index}", new Vector3(x, 0.8f, -5.02f), 0.05f, glow * 0.58f);
            }
        }

        private static void CreateGlowBulb(string name, Vector3 position, float size, Color glow)
        {
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = name;
            bulb.transform.position = position;
            bulb.transform.localScale = Vector3.one * size;
            bulb.GetComponent<Renderer>().sharedMaterial = GameMaterials.Club(glow * 0.15f, glow * 1.2f);
        }

        private static void CreateMirrorBall()
        {
            GameObject root = new("Disco Ball");
            root.transform.position = new Vector3(0f, 8.05f, -6.5f);
            Material darkCore = GameMaterials.Lit(new Color(0.025f, 0.03f, 0.045f), 0.88f, 0.72f, new Color(0.005f, 0.008f, 0.015f));
            Material mirror = GameMaterials.Lit(new Color(0.32f, 0.38f, 0.5f), 1f, 0.96f, new Color(0.035f, 0.055f, 0.1f));

            GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.name = "Mirror Ball Core";
            core.transform.SetParent(root.transform, false);
            core.transform.localScale = Vector3.one * 0.96f;
            core.GetComponent<Renderer>().sharedMaterial = darkCore;

            const int longitudeCount = 16;
            const int latitudeCount = 7;
            for (int latitude = 0; latitude < latitudeCount; latitude++)
            {
                float latitudeAngle = Mathf.Lerp(-62f, 62f, latitude / (float)(latitudeCount - 1)) * Mathf.Deg2Rad;
                for (int longitude = 0; longitude < longitudeCount; longitude++)
                {
                    float longitudeAngle = longitude * Mathf.PI * 2f / longitudeCount + (latitude % 2) * 0.08f;
                    Vector3 normal = new(
                        Mathf.Cos(latitudeAngle) * Mathf.Sin(longitudeAngle),
                        Mathf.Sin(latitudeAngle),
                        Mathf.Cos(latitudeAngle) * Mathf.Cos(longitudeAngle)
                    );
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Mirror Tile {latitude}-{longitude}";
                    tile.transform.SetParent(root.transform, false);
                    tile.transform.localPosition = normal * 0.51f;
                    tile.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, normal);
                    tile.transform.localScale = new Vector3(0.075f, 0.075f, 0.018f);
                    tile.GetComponent<Renderer>().sharedMaterial = mirror;
                }
            }

            Material cable = GameMaterials.Lit(new Color(0.01f, 0.012f, 0.016f), 0.7f, 0.3f, Color.black);
            CreateTubeBetween("Mirror Ball Cable", new Vector3(0f, 8.55f, -6.5f), new Vector3(0f, 9.4f, -6.5f), cable, 0.018f);
        }

        private static void CreateLaserShow()
        {
            Color[] colors =
            {
                new(0.02f, 0.9f, 1f), new(1f, 0.03f, 0.55f), new(0.5f, 0.18f, 1f),
                new(0.2f, 1f, 0.45f), new(1f, 0.18f, 0.08f)
            };
            for (int index = 0; index < 8; index++)
            {
                Vector3 origin = new(Mathf.Lerp(-5.5f, 5.5f, index / 7f), 6.8f + (index % 2) * 0.35f, -8.3f);
                GameObject laser = new($"Laser Beam {index}");
                LineRenderer line = laser.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.028f;
                line.endWidth = 0.058f;
                line.numCapVertices = 3;
                line.sharedMaterial = GameMaterials.BackgroundSprite();
                Color color = colors[index % colors.Length];
                line.startColor = new Color(color.r, color.g, color.b, 0.44f);
                line.endColor = new Color(color.r, color.g, color.b, 0.035f);
                laser.AddComponent<ClubLaserBeam>().Initialize(line, origin, index * 0.73f);
            }

            for (int fan = 0; fan < 4; fan++)
            for (int ray = 0; ray < 4; ray++)
            {
                float side = fan < 2 ? -1f : 1f;
                Vector3 origin = new(side * (4.6f + (fan % 2) * 0.65f), 4.8f + (fan % 2) * 1.1f, -8.7f);
                GameObject laser = new($"Laser Fan {fan}-{ray}");
                LineRenderer line = laser.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.016f;
                line.endWidth = 0.035f;
                line.numCapVertices = 2;
                line.sharedMaterial = GameMaterials.BackgroundSprite();
                Color color = colors[(fan + ray) % colors.Length];
                line.startColor = new Color(color.r, color.g, color.b, 0.5f);
                line.endColor = new Color(color.r, color.g, color.b, 0.025f);
                laser.AddComponent<ClubLaserBeam>().Initialize(line, origin, fan * 1.9f + ray * 0.22f);
            }
        }

        private static void CreateCircularTrussRig()
        {
            Vector3 center = new(0f, 6.35f, -2.25f);
            const float outerRadius = 3.7f;
            const float innerRadius = 3.34f;
            const int segments = 32;
            const float halfHeight = 0.2f;
            Quaternion rigTilt = Quaternion.Euler(10f, 0f, 0f);
            Material trussMaterial = GameMaterials.Lit(
                new Color(0.16f, 0.17f, 0.19f),
                0.82f,
                0.58f,
                new Color(0.012f, 0.02f, 0.03f)
            );

            Vector3 RingPoint(float radius, float angle, float height)
            {
                return center + rigTilt * new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
            }

            for (int index = 0; index < segments; index++)
            {
                float angle = index * Mathf.PI * 2f / segments;
                float nextAngle = (index + 1) * Mathf.PI * 2f / segments;

                CreateTrussBarBetween($"Circular Truss Outer Top {index}", RingPoint(outerRadius, angle, halfHeight), RingPoint(outerRadius, nextAngle, halfHeight), trussMaterial);
                CreateTrussBarBetween($"Circular Truss Outer Bottom {index}", RingPoint(outerRadius, angle, -halfHeight), RingPoint(outerRadius, nextAngle, -halfHeight), trussMaterial);
                CreateTrussBarBetween($"Circular Truss Inner Top {index}", RingPoint(innerRadius, angle, halfHeight), RingPoint(innerRadius, nextAngle, halfHeight), trussMaterial);
                CreateTrussBarBetween($"Circular Truss Inner Bottom {index}", RingPoint(innerRadius, angle, -halfHeight), RingPoint(innerRadius, nextAngle, -halfHeight), trussMaterial);

                if (index % 4 == 0)
                {
                    CreateTrussBarBetween($"Circular Truss Radial {index}", RingPoint(innerRadius, angle, 0f), RingPoint(outerRadius, angle, 0f), trussMaterial);
                    CreateTrussBarBetween($"Circular Truss Vertical Outer {index}", RingPoint(outerRadius, angle, -halfHeight), RingPoint(outerRadius, angle, halfHeight), trussMaterial);
                    CreateTrussBarBetween($"Circular Truss Vertical Inner {index}", RingPoint(innerRadius, angle, -halfHeight), RingPoint(innerRadius, angle, halfHeight), trussMaterial);
                }
            }

            Material cableMaterial = GameMaterials.Lit(new Color(0.008f, 0.009f, 0.012f), 0.65f, 0.28f, Color.black);
            for (int index = 0; index < 4; index++)
            {
                float angle = index * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                Vector3 clampPosition = RingPoint(outerRadius, angle, halfHeight);
                CreateTubeBetween($"Truss Suspension Cable {index}", clampPosition, new Vector3(clampPosition.x, 9.25f, clampPosition.z), cableMaterial, 0.018f);
                GameObject clamp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                clamp.name = $"Truss Clamp {index}";
                clamp.transform.position = clampPosition;
                clamp.transform.localScale = new Vector3(0.11f, 0.065f, 0.11f);
                clamp.GetComponent<Renderer>().sharedMaterial = trussMaterial;
            }

            Color[] beamColors =
            {
                new(0.05f, 0.56f, 1f),
                new(0.38f, 0.14f, 1f),
                new(0.02f, 0.88f, 1f),
                new(1f, 0.08f, 0.5f),
                new(1f, 0.5f, 0.06f),
                new(0.08f, 1f, 0.52f)
            };
            for (int index = 0; index < 12; index++)
            {
                float angle = index * Mathf.PI * 2f / 12f;
                Vector3 origin = RingPoint(3.52f, angle, -0.34f);
                MovingHeadStyle style = (index % 4) switch
                {
                    0 => MovingHeadStyle.Wash,
                    1 => MovingHeadStyle.Spot,
                    2 => MovingHeadStyle.GoboStar,
                    _ => MovingHeadStyle.Beam
                };
                bool castsFloorLight = index % 3 == 0;
                CreateMovingHead($"Moving Head Ring {index}", origin, beamColors[index % beamColors.Length], index * 0.82f, style, castsFloorLight);
            }

            for (int index = 0; index < 8; index++)
            {
                float angle = index * Mathf.PI * 2f / 8f + Mathf.PI / 8f;
                Vector3 origin = RingPoint(3.2f, angle, -0.47f);
                MovingHeadStyle style = index % 2 == 0 ? MovingHeadStyle.GoboDots : MovingHeadStyle.Beam;
                Color color = beamColors[(index + 1) % beamColors.Length];
                CreateMovingHead($"Moving Head Floor FX {index}", origin, color, index * 1.16f + 0.4f, style, false);
            }
        }

        private static void CreateTrussBarBetween(string name, Vector3 start, Vector3 end, Material material)
        {
            CreateTubeBetween(name, start, end, material, 0.045f);
        }

        private static void CreateTubeBetween(string name, Vector3 start, Vector3 end, Material material, float radius)
        {
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bar.name = name;
            bar.transform.position = (start + end) * 0.5f;
            Vector3 direction = end - start;
            bar.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            bar.transform.localScale = new Vector3(radius, direction.magnitude * 0.5f, radius);
            bar.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateMovingHead(string name, Vector3 origin, Color color, float phase, MovingHeadStyle style, bool castsLight)
        {
            GameObject fixture = new(name);
            fixture.transform.position = origin;
            Material metal = GameMaterials.Lit(new Color(0.025f, 0.028f, 0.035f), 0.72f, 0.48f, Color.black);
            bool wash = style == MovingHeadStyle.Wash;
            bool beam = style == MovingHeadStyle.Beam;
            Material lens = GameMaterials.Lit(color * 0.18f, 0.12f, 0.82f, color * (wash ? 1.25f : beam ? 2.1f : 1.8f));

            CreateFixturePart("Base", fixture.transform, PrimitiveType.Cylinder, new Vector3(0f, -0.12f, -0.08f), new Vector3(0.24f, 0.08f, 0.24f), metal);
            CreateFixturePart("Yoke Left", fixture.transform, PrimitiveType.Cube, new Vector3(-0.19f, 0f, 0.02f), new Vector3(0.055f, 0.34f, 0.085f), metal);
            CreateFixturePart("Yoke Right", fixture.transform, PrimitiveType.Cube, new Vector3(0.19f, 0f, 0.02f), new Vector3(0.055f, 0.34f, 0.085f), metal);
            CreateFixturePart("Lamp Body", fixture.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.08f, 0.08f), new Vector3(0.2f, 0.22f, 0.2f), metal, new Vector3(90f, 0f, 0f));
            CreateFixturePart("Lens", fixture.transform, PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0.29f), wash ? new Vector3(0.16f, 0.16f, 0.055f) : new Vector3(0.125f, 0.125f, 0.045f), lens);
            MovingHeadFixture head = fixture.AddComponent<MovingHeadFixture>();
            head.Initialize(origin, color, phase, style, castsLight);
            if (castsLight) head.GetComponent<Light>().cullingMask = -1;
        }

        private static void CreateFixturePart(string name, Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localEulerAngles = localEuler;
            part.transform.localScale = localScale;
            part.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLedBarRig()
        {
            Material housing = GameMaterials.Lit(new Color(0.018f, 0.02f, 0.028f), 0.78f, 0.42f, Color.black);
            for (int index = 0; index < 8; index++)
            {
                Color glow = index % 2 == 0 ? new Color(0.04f, 0.62f, 1f) : new Color(0.52f, 0.14f, 0.92f);
                float x = Mathf.Lerp(-5.8f, 5.8f, index / 7f);
                float z = -6.35f + Mathf.Sin(index * 0.78f) * 0.8f;
                CreateLitBox($"LED Batten Housing {index}", new Vector3(x, 7.58f, z), new Vector3(0.15f, 0.11f, 1.15f), housing);
                CreateBox($"LED Batten Diffuser {index}", new Vector3(x, 7.515f, z), new Vector3(0.08f, 0.025f, 1.02f), glow * 0.04f, glow * 0.36f);
            }
            for (int side = -1; side <= 1; side += 2)
            for (int index = 0; index < 4; index++)
            {
                Color glow = index % 2 == 0 ? new Color(0.04f, 0.62f, 1f) : new Color(0.52f, 0.14f, 0.92f);
                float y = 2.2f + index * 1.25f;
                CreateLitBox($"Wall Strip Housing {side}-{index}", new Vector3(side * 6.72f, y, -6.3f), new Vector3(0.12f, 0.92f, 0.18f), housing);
                CreateBox($"Wall Strip Diffuser {side}-{index}", new Vector3(side * 6.64f, y, -6.2f), new Vector3(0.025f, 0.78f, 0.06f), glow * 0.04f, glow * 0.3f);
            }
        }

        private static void CreateStrobes()
        {
            Material housing = GameMaterials.Lit(new Color(0.018f, 0.02f, 0.026f), 0.76f, 0.44f, Color.black);
            Material lens = GameMaterials.Lit(new Color(0.32f, 0.34f, 0.4f), 0.08f, 0.72f, new Color(0.09f, 0.1f, 0.14f));
            float[] positions = { -5.2f, -1.8f, 1.8f, 5.2f };
            for (int index = 0; index < positions.Length; index++)
            {
                float x = positions[index];
                GameObject strobeObject = new($"Strobe Light {index}");
                strobeObject.transform.position = new Vector3(x, index is 1 or 2 ? 7.15f : 6.3f, index is 1 or 2 ? -4.15f : -5.2f);
                CreateFixturePart("Strobe Housing", strobeObject.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.58f, 0.24f, 0.28f), housing);
                CreateFixturePart("Strobe Lens", strobeObject.transform, PrimitiveType.Cube, new Vector3(0f, 0f, 0.16f), new Vector3(0.46f, 0.14f, 0.045f), lens);
                Light strobe = strobeObject.AddComponent<Light>();
                strobe.type = LightType.Spot;
                strobe.color = Color.white;
                strobe.range = 22f;
                strobe.spotAngle = 72f;
                strobe.intensity = 0f;
                strobe.cullingMask = ~(1 << FloorLightingLayer);
                strobeObject.transform.LookAt(new Vector3(x * 0.3f, 0.7f, 0.8f));
                strobeObject.AddComponent<ClubStrobeLight>().Initialize(strobe, index * 0.055f);
            }
        }

        private static void CreateTurntable(float x)
        {
            Material chassis = GameMaterials.Lit(new Color(0.02f, 0.023f, 0.03f), 0.72f, 0.48f, Color.black);
            Material platterMaterial = GameMaterials.Lit(new Color(0.035f, 0.04f, 0.05f), 0.62f, 0.65f, new Color(0.004f, 0.008f, 0.014f));
            GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deck.name = $"DJ Turntable {x}";
            deck.transform.position = new Vector3(x, 2.16f + DjBoothHeightOffset, -7.45f);
            deck.transform.localScale = new Vector3(0.72f, 0.055f, 0.72f);
            deck.GetComponent<Renderer>().sharedMaterial = chassis;

            GameObject platter = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            platter.name = "Turntable Platter";
            platter.transform.position = new Vector3(x, 2.225f + DjBoothHeightOffset, -7.45f);
            platter.transform.localScale = new Vector3(0.52f, 0.022f, 0.52f);
            platter.GetComponent<Renderer>().sharedMaterial = platterMaterial;

            CreateOrientedCylinder("Turntable Label", new Vector3(x, 2.255f + DjBoothHeightOffset, -7.45f), new Vector3(0.17f, 0.012f, 0.17f), Vector3.zero, GameMaterials.Lit(new Color(0.16f, 0.18f, 0.22f), 0.45f, 0.56f, new Color(0.01f, 0.018f, 0.03f)));
            CreateOrientedCylinder("Turntable Spindle", new Vector3(x, 2.3f + DjBoothHeightOffset, -7.45f), new Vector3(0.025f, 0.065f, 0.025f), Vector3.zero, chassis);
        }

        private static void CreateMixer()
        {
            Material mixerMaterial = GameMaterials.Lit(new Color(0.018f, 0.021f, 0.028f), 0.68f, 0.46f, Color.black);
            CreateLitBox("DJ Mixer", new Vector3(0f, 2.15f + DjBoothHeightOffset, -7.45f), new Vector3(1.05f, 0.12f, 0.72f), mixerMaterial);
            for (int index = -3; index <= 3; index++)
            {
                Color color = index % 2 == 0 ? new Color(0.02f, 0.75f, 1f) : new Color(1f, 0.04f, 0.52f);
                CreateBox($"Mixer Fader {index}", new Vector3(index * 0.12f, 2.225f + DjBoothHeightOffset, -7.42f), new Vector3(0.018f, 0.012f, 0.32f), color * 0.08f, color * 0.32f);
                CreateOrientedCylinder($"Mixer Knob {index}", new Vector3(index * 0.12f, 2.255f + DjBoothHeightOffset, -7.55f), new Vector3(0.035f, 0.028f, 0.035f), Vector3.zero, mixerMaterial);
            }
        }

        private static void CreateBox(string name, Vector3 position, Vector3 scale, Color color, Color emission)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = GameMaterials.Club(color, emission);
        }

        private static void CreateLitBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.position = position;
            box.transform.localScale = scale;
            box.layer = FloorLightingLayer;
            box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateSpeaker(float x)
        {
            Material cabinet = GameMaterials.Lit(new Color(0.012f, 0.013f, 0.018f), 0.18f, 0.24f, Color.black, 0.06f, 16f);
            Material grille = GameMaterials.Lit(new Color(0.018f, 0.02f, 0.024f), 0.72f, 0.2f, new Color(0.002f, 0.003f, 0.004f), 0.08f, 22f);
            Material rubber = GameMaterials.Lit(new Color(0.008f, 0.009f, 0.012f), 0.12f, 0.16f, Color.black);
            Material coneMaterial = GameMaterials.Lit(new Color(0.035f, 0.04f, 0.052f), 0.35f, 0.3f, new Color(0.003f, 0.007f, 0.012f));
            CreateLitBox($"Speaker Cabinet {x}", new Vector3(x, 2f, -8.25f), new Vector3(1.45f, 4f, 1.1f), cabinet);
            CreateLitBox($"Speaker Grille {x}", new Vector3(x, 2f, -7.675f), new Vector3(1.27f, 3.78f, 0.045f), grille);
            for (int index = 0; index < 2; index++)
            {
                float y = 1.2f + index * 1.65f;
                CreateOrientedCylinder("Speaker Rubber Surround", new Vector3(x, y, -7.63f), new Vector3(0.61f, 0.055f, 0.61f), new Vector3(90f, 0f, 0f), rubber);
                CreateOrientedCylinder("Speaker Cone", new Vector3(x, y, -7.57f), new Vector3(0.48f, 0.04f, 0.48f), new Vector3(90f, 0f, 0f), coneMaterial);
                GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cap.name = "Speaker Dust Cap";
                cap.transform.position = new Vector3(x, y, -7.5f);
                cap.transform.localScale = new Vector3(0.2f, 0.2f, 0.065f);
                cap.GetComponent<Renderer>().sharedMaterial = rubber;
            }
            CreateOrientedCylinder("Speaker Tweeter", new Vector3(x, 3.62f, -7.58f), new Vector3(0.18f, 0.035f, 0.18f), new Vector3(90f, 0f, 0f), coneMaterial);
        }

        private static void CreateOrientedCylinder(string name, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.position = position;
            cylinder.transform.localEulerAngles = euler;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void AddLight(string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new(name);
            lightObject.transform.position = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.cullingMask = ~(1 << FloorLightingLayer);
        }

        private static void CreateBackgroundQuad()
        {
            GameObject sky = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            sky.name = "BackgroundSky";
            sky.transform.position = new Vector3(0f, 15f, 0f);
            sky.transform.localScale = new Vector3(200f, 100f, 200f);
            
            Shader shader = Resources.Load<Shader>("Shaders/ProceduralSmoke");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material mat = new Material(shader);
            sky.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static void CreateSmokeMachines()
        {
            GameObject smokeGroup = new GameObject("Low Lying Fog");
            smokeGroup.AddComponent<SmokeMachineController>();

            Shader pShader = Resources.Load<Shader>("Shaders/SimpleParticle");
            if (pShader == null) pShader = Shader.Find("Sprites/Default");
            Material particleMat = new Material(pShader);

            GameObject machine = new GameObject("FogEmitter");
            machine.transform.parent = smokeGroup.transform;
            machine.transform.position = new Vector3(0f, -0.2f, -1f);
            machine.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // UP

            ParticleSystem ps = machine.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 15f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startColor = new Color(0.9f, 0.95f, 1f, 0.12f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = 800;

            var emission = ps.emission;
            emission.rateOverTime = 60f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 14f, 0.2f); // Emits across the dance floor

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.3f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.2f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.2f), new GradientAlphaKey(1.0f, 0.8f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = grad;
            
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.5f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ParticleSystemRenderer renderer = machine.GetComponent<ParticleSystemRenderer>();
            renderer.material = particleMat;
            renderer.sortMode = ParticleSystemSortMode.Distance;
        }
    }
}
