using UnityEngine;

namespace DustScrapper.Vision
{
    /// <summary>
    /// 플레이어 시야 밖을 어둡게 덮는다. 플레이어를 중심으로 사방에 광선을 쏴서 각 방향으로 보이는 거리
    /// (시야 모양 또는 벽까지)를 구하고, 그 안쪽은 투명·바깥은 어두운 메시를 매 프레임 만든다.
    /// 무엇이 보이는지 판정하지는 않는다. 거리 계산은 PlayerVision을 그대로 따른다.
    /// 어둠은 완전히 검지 않게 해서, 시야 밖이라도 맵의 생김새는 희미하게 보인다.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [DisallowMultipleComponent]
    public sealed class VisionShade : MonoBehaviour
    {
        // 광선마다 점 4개: 중심, 밝은 끝, 어둠 시작, 화면 밖 끝.
        const int PointsPerRay = 4;

        [SerializeField] PlayerVision vision;
        [Tooltip("화면을 다 덮었는지 확인할 카메라. 비워 두면 Main Camera.")]
        [SerializeField] Camera viewCamera;

        [Tooltip("광선 수. 많을수록 벽 모서리의 그림자가 매끈하다.")]
        [SerializeField, Range(32, 720)] int rayCount = 240;

        [Tooltip("시야 밖의 어둡기 (0 = 투명, 1 = 완전히 검정)")]
        [SerializeField, Range(0f, 1f)] float darkness = 0.85f;
        [SerializeField] Color shadeColor = new(0.02f, 0.02f, 0.04f, 1f);

        [Tooltip("시야 끝이 어두워지는 폭 (칸). 탁 끊기지 않고 서서히 어두워진다.")]
        [SerializeField, Min(0f)] float edgeSoftness = 0.8f;

        [Tooltip("광선이 벽에 닿았을 때 벽 안쪽으로 더 밝히는 깊이 (칸). 벽의 앞면이 보이게 한다. 벽 두께보다 얇게 둔다.")]
        [SerializeField, Min(0f)] float wallReveal = 0.35f;

        Mesh mesh;
        Vector3[] vertices;
        Color[] colors;

        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;

            mesh = new Mesh { name = "Vision Shade" };
            mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = mesh;
            BuildTopology();
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
        }

        void LateUpdate()
        {
            if (vision == null || vision.Profile == null) return;
            if (vertices.Length != (rayCount + 1) * PointsPerRay) BuildTopology();

            Vector2 origin = vision.Origin;
            float outer = OuterRadius(origin);
            // 메시 꼭짓점 색은 스프라이트 색과 달리 자동으로 변환되지 않는다.
            // 프로젝트가 선형 색 공간이면 직접 바꿔 줘야 Inspector에서 고른 색 그대로 보인다.
            Color color = QualitySettings.activeColorSpace == ColorSpace.Linear ? shadeColor.linear : shadeColor;
            Color clear = color;
            clear.a = 0f;
            Color dark = color;
            dark.a = darkness;
            LayerMask blockers = vision.SightBlockers;

            for (int ray = 0; ray <= rayCount; ray++)
            {
                float angle = ray * Mathf.PI * 2f / rayCount;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float radius = vision.SightRadius(direction);

                float clearEnd = radius;
                float darkStart = radius + edgeSoftness;
                RaycastHit2D hit = Physics2D.Raycast(origin, direction, radius + edgeSoftness, blockers);
                if (hit.collider != null)
                {
                    // 벽에 막히면 벽 앞면까지만 밝히고 그 뒤는 바로 어둡게 한다.
                    clearEnd = Mathf.Min(radius, hit.distance + wallReveal);
                    darkStart = Mathf.Min(darkStart, hit.distance + wallReveal + 0.05f);
                }

                int i = ray * PointsPerRay;
                vertices[i] = Vector3.zero;
                vertices[i + 1] = direction * clearEnd;
                vertices[i + 2] = direction * darkStart;
                vertices[i + 3] = direction * Mathf.Max(outer, darkStart);
                colors[i] = clear;
                colors[i + 1] = clear;
                colors[i + 2] = dark;
                colors[i + 3] = dark;
            }

            transform.SetPositionAndRotation(origin, Quaternion.identity);
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.RecalculateBounds();
        }

        /// <summary>광선 수에 맞춰 메시의 삼각형 연결을 한 번 만든다. 이후에는 점 위치·색만 바꾼다.</summary>
        void BuildTopology()
        {
            vertices = new Vector3[(rayCount + 1) * PointsPerRay];
            colors = new Color[vertices.Length];
            var triangles = new int[rayCount * (PointsPerRay - 1) * 6];

            int t = 0;
            for (int ray = 0; ray < rayCount; ray++)
            {
                for (int ring = 0; ring < PointsPerRay - 1; ring++)
                {
                    int a = ray * PointsPerRay + ring;
                    int b = (ray + 1) * PointsPerRay + ring;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                    triangles[t++] = a + 1;
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
        }

        /// <summary>화면 네 귀퉁이까지 덮을 수 있는 거리.</summary>
        float OuterRadius(Vector2 origin)
        {
            float farthest = vision.Profile.ForwardRadius + edgeSoftness + 1f;
            if (viewCamera == null) return farthest + 20f;

            float halfHeight = viewCamera.orthographicSize;
            float halfWidth = halfHeight * viewCamera.aspect;
            Vector2 center = viewCamera.transform.position;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Vector2 corner = center + new Vector2(halfWidth * x, halfHeight * y);
                    farthest = Mathf.Max(farthest, Vector2.Distance(origin, corner) + 1f);
                }
            }
            return farthest;
        }
    }
}
