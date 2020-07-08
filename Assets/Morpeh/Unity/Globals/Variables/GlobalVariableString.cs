namespace Morpeh.Globals {
    using UnityEngine;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    [CreateAssetMenu(menuName = "ECS/Globals/Variables/Variable String")]
    public class GlobalVariableString : BaseGlobalVariable<string> {
        public override DataWrapper Wrapper {
            get => new StringWrapper {value = this.value};
            set => this.Value = ((StringWrapper) value).value;
        }
        
        protected override string Load(string serializedData) => serializedData;

        protected override string Save() => this.value;
        
        public override string LastToString() => this.BatchedChanges.Peek();
        
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        [System.Serializable]
        private class StringWrapper : DataWrapper {
            public string value;
            
            public override string ToString() => this.value;
        }
    }
}