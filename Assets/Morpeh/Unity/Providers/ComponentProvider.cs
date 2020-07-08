﻿namespace Morpeh {
    using UnityEngine.Assertions;
 
    public abstract class ComponentProvider<T0, T1> : MonoProvider<T1>
        where T0 : UnityEngine.Component
        where T1 : struct, IMonoComponent<T0> {
            private void OnValidate() {
            ref var data = ref GetData(out _);
            if (data.monoComponent == null) {
                data.monoComponent = this.gameObject.GetComponent<T0>();
                Assert.IsNotNull(data.monoComponent, $"Missing {typeof(T0)} component.");
            }
        }
    }
}