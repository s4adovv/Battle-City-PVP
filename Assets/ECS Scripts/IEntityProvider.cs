public class IEntityProvider : UnityEngine.MonoBehaviour
{

   /// <summary>
   /// An Entity that can be attached to any object to avoid unnecessarily creating many providers for many components... Obviously, any of components should be attached in scripts.
   /// </summary>
   public Morpeh.IEntity Entity { get; set; }

}
