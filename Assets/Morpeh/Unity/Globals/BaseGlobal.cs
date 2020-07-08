namespace Morpeh.Globals {
    using System;
    using ECS;
    using JetBrains.Annotations;
#if ODIN_INSPECTOR
    using Sirenix.OdinInspector;
#endif
    using Unity.IL2CPP.CompilerServices;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;
    
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public abstract class BaseGlobal : ScriptableObject, IDisposable {
        [SerializeField]
        internal bool isPublished;
        
        [SerializeField]
#if ODIN_INSPECTOR
        [ReadOnly]
#endif
        private protected int internalEntityID = -1;

        private protected Entity internalEntity;
        
        [CanBeNull]
        private protected Entity InternalEntity {
            get {
                if (this.internalEntity == null) {
                    this.internalEntity = World.Default.entities[this.internalEntityID];
                }

                return this.internalEntity;
            }
        }

        public IEntity Entity {
            get {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    return default;
                }
                this.CheckIsInitialized();
#endif
                return this.InternalEntity;
            }
        }
        
        public bool IsPublished {
            get {
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    return default;
                }
                this.CheckIsInitialized();
#endif
                return this.isPublished;
            }
        }
        
#if UNITY_EDITOR
        public abstract Type GetValueType();
#endif
        internal virtual void OnEnable() {
            this.internalEntity = null;
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += this.OnEditorApplicationOnplayModeStateChanged;
#else
            CheckIsInitialized();
#endif
        }
        
#if UNITY_EDITOR
        internal virtual void OnEditorApplicationOnplayModeStateChanged(PlayModeStateChange state) {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredEditMode) {
                this.internalEntityID = -1;
                this.internalEntity = null;
            }
        }
#endif
        
        public abstract string LastToString();

        private protected abstract void CheckIsInitialized();

        public static implicit operator bool(BaseGlobal exists) => exists != null && exists.IsPublished;

        private protected class Unsubscriber : IDisposable {
            private readonly Action unsubscribe;
            public Unsubscriber(Action unsubscribe) => this.unsubscribe = unsubscribe;
            public void Dispose() => this.unsubscribe();
        }

        public virtual void Dispose() {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= this.OnEditorApplicationOnplayModeStateChanged;
#endif
            if (this.internalEntityID != -1) {
                var entity = this.InternalEntity;
                if (entity != null && !entity.IsDisposed()) {
                    World.Default.RemoveEntity(entity);
                }
                this.internalEntityID = -1;
                this.internalEntity = null;
            }
        }

        
        private void OnDestroy() {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= this.OnEditorApplicationOnplayModeStateChanged;
#endif
        }
    }
}