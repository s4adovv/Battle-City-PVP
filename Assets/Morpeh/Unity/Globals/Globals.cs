namespace Morpeh.Globals {
    namespace ECS {
        using System;
        using System.Collections.Generic;
        using UnityEngine;
        
        [Serializable]
        public struct GlobalEventMarker : IComponent {
        }

        internal abstract class GlobalEventComponentUpdater : IDisposable {
            internal static Dictionary<int, List<GlobalEventComponentUpdater>> updaters = new Dictionary<int, List<GlobalEventComponentUpdater>>();

            protected Filter filterPublishedWithoutNextFrame;
            protected Filter filterPublishedNextFrame;
            protected Filter filterNextFrame;

            internal abstract void Awake(World world);

            internal abstract void Update();

            public abstract void Dispose();
        }

        internal sealed class GlobalEventComponentUpdater<T> : GlobalEventComponentUpdater {
            public static Dictionary<int, bool> initialized = new Dictionary<int, bool>();

            public int worldId;
            
            internal override void Awake(World world) {
                this.worldId = world.id;

                if (initialized.ContainsKey(this.worldId)) {
                    initialized[this.worldId] = true;
                }
                else {
                    initialized.Add(this.worldId, true);
                }
                
                var common = world.Filter.With<GlobalEventMarker>().With<GlobalEventComponent<T>>();
                this.filterPublishedWithoutNextFrame = common.With<GlobalEventPublished>().Without<GlobalEventNextFrame>();
                this.filterPublishedNextFrame = common.With<GlobalEventPublished>().With<GlobalEventNextFrame>();
                this.filterNextFrame = common.With<GlobalEventNextFrame>();
            }

            internal override void Update() {
                foreach (var entity in this.filterPublishedWithoutNextFrame) {
                    ref var evnt = ref entity.GetComponent<GlobalEventComponent<T>>(out _);
                    evnt.Action?.Invoke(evnt.Data);
                    evnt.Data.Clear();
                    evnt.Global.isPublished = false;
                    entity.RemoveComponent<GlobalEventPublished>();
                }
                foreach (var entity in this.filterPublishedNextFrame) {
                    ref var evnt = ref entity.GetComponent<GlobalEventComponent<T>>(out _);
                    evnt.Action?.Invoke(evnt.Data);
                }
                foreach (var entity in this.filterNextFrame) {
                    ref var evnt = ref entity.GetComponent<GlobalEventComponent<T>>(out _);
                    evnt.Global.isPublished = true;
                    entity.SetComponent(new GlobalEventPublished ());
                    entity.RemoveComponent<GlobalEventNextFrame>();
                }
            }

            public override void Dispose() {
                initialized[this.worldId] = false;
            }
        }


        [Serializable]
        public struct GlobalEventComponent<TData> : IComponent {
            public BaseGlobal Global;

            public Action<IEnumerable<TData>> Action;
            public Stack<TData>               Data;
        }
        [Serializable]
        public struct GlobalEventLastToString : IComponent {
            public Func<string> LastToString;
        }

        [Serializable]
        public struct GlobalEventPublished : IComponent {
        }

        [Serializable]
        public struct GlobalEventNextFrame : IComponent {
        }

        internal sealed class ProcessEventsSystem : ILateSystem {
            public World World { get; set; }
            public int worldId;

            public void OnAwake() {
                this.worldId = this.World.id;
            }

            public void OnUpdate(float deltaTime) {
                if (GlobalEventComponentUpdater.updaters.TryGetValue(this.worldId, out var updaters)) {
                    foreach (var updater in updaters) {
                        updater.Update();
                    }
                }
            }

            public void Dispose() {
                if (GlobalEventComponentUpdater.updaters.TryGetValue(this.worldId, out var updaters)) {
                    foreach (var updater in updaters) {
                        updater.Dispose();
                    }
                    updaters.Clear();
                }
            }
        }
    }
}

namespace Morpeh {
    partial class World {
        partial void InitializeGlobals() {
            var sg = this.CreateSystemsGroup();
            sg.AddSystem(new Morpeh.Globals.ECS.ProcessEventsSystem());
            this.AddSystemsGroup(99999, sg);
        }
    }
}