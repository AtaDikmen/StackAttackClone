using UnityEngine;

namespace Gameplay
{
    public class ScrollingGround : MonoBehaviour
    {
        [SerializeField] private Renderer groundRenderer;
        [SerializeField] private Vector2  scrollSpeed = new Vector2(0f, -0.35f);

        private                 Material _groundMaterial;
        private readonly static int      MainTex = Shader.PropertyToID("_MainTex");
        private readonly static int      BaseMap = Shader.PropertyToID("_BaseMap");

        private void Awake()
        {
            if(groundRenderer == null) groundRenderer  = GetComponent<Renderer>();
            if(groundRenderer != null) _groundMaterial = groundRenderer.material;
        }

        private void Update()
        {
            if(_groundMaterial == null) return;

            Vector2 offset = scrollSpeed * Time.deltaTime;

            if(_groundMaterial.HasProperty(BaseMap))
                _groundMaterial.SetTextureOffset(BaseMap, _groundMaterial.GetTextureOffset(BaseMap) + offset);
            else if(_groundMaterial.HasProperty(MainTex))
                _groundMaterial.SetTextureOffset(MainTex, _groundMaterial.GetTextureOffset(MainTex) + offset);
        }
    }
}
