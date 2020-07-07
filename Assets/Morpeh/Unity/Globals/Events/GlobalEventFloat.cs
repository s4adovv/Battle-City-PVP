namespace Morpeh.Globals {
    using System.Globalization;
    using UnityEngine;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    [CreateAssetMenu(menuName = "ECS/Globals/Events/Event Float")]
    public class GlobalEventFloat : BaseGlobalEvent<float> {
        public override string LastToString() => this.BatchedChanges.Peek().ToString(CultureInfo.InvariantCulture);
    }
}