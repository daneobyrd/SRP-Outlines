namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Custom Pass that draws Object IDs
    /// </summary>
    [System.Serializable]
    public class ObjectIDCustomPass : MonoBehaviour
    {
        /// <summary>
        /// Used to assign an ObjectID (in the form of a color) to every renderer in the scene.
        /// If a scene uses dynamic objects or procedural object placement, then the user script should call
        /// this function to assign Object IDs to the new objects.
        /// </summary>
        public virtual void AssignObjectIDs()
        {
            var rendererList = Resources.FindObjectsOfTypeAll(typeof(Renderer));

            int index = 0;
            foreach (Renderer renderer in rendererList)
            {
                MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                float                 hue           = (float)index / rendererList.Length;
                propertyBlock.SetColor("ObjectColor", Color.HSVToRGB(hue, 0.7f, 1.0f));
                renderer.SetPropertyBlock(propertyBlock);
                index++;
            }
        }
        
        public class RandomObjectIDs : ObjectIDCustomPass
        {
            public override void AssignObjectIDs()
            {
                var           rendererList = Resources.FindObjectsOfTypeAll(typeof(Renderer));
                System.Random rand         = new System.Random();

                int index = 0;
                foreach (Renderer renderer in rendererList)
                {
                    MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
                    float                 hue           = (float)rand.NextDouble();
                    propertyBlock.SetColor("ObjectColor", Color.HSVToRGB(hue, 0.7f, 1.0f));
                    renderer.SetPropertyBlock(propertyBlock);
                    index++;
                }
            }
        }

        public struct PerObjectIDs
        {
            public float objID;
            
            public static int GetSize()
            {
                return sizeof(float);
            }
        }

    }
}