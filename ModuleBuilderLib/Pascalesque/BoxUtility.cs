using System;
using System.Collections.Immutable;
using System.Threading;

namespace Sunlighter.ModuleBuilderLib.Pascalesque
{
    public sealed class Box<T>
    {
#if NET9_0_OR_GREATER
        private static readonly Lock syncRoot = new Lock();
#else
        private static readonly object syncRoot = new object();
#endif
        private static ulong nextId;

        private readonly ulong id;
        private T value;

        public Box(T initialValue)
        {
            lock(syncRoot)
            {
                id = nextId;
                ++nextId;
            }

            value = initialValue;
        }

        public ulong Id => id;

        public T Value
        {
            get => value;
            set => this.value = value;
        }
    }

    public interface ITrailTraits<TTrail>
    {
        bool HasHost(TTrail trail);
        TTrail GetHost(TTrail trail);

        bool HasUndoRecordBox(TTrail trail);
        Box<ImmutableSortedDictionary<ulong, Action<TTrail>>> GetUndoRecordBox(TTrail trail);
    }

    public static partial class BoxUtility
    {
        public static void SetValue<TTrail, T>(ITrailTraits<TTrail> trailTraits, TTrail trail, Box<T> box, T value)
        {
            if (trailTraits.HasUndoRecordBox(trail))
            {
                Box<ImmutableSortedDictionary<ulong, Action<TTrail>>> undoRecordBox = trailTraits.GetUndoRecordBox(trail);

                T oldValue = box.Value;

                void restore(TTrail hostTrail)
                {
                    SetValue(trailTraits, hostTrail, box, oldValue);
                }

                ImmutableSortedDictionary<ulong, Action<TTrail>> addToDictionary(ImmutableSortedDictionary<ulong, Action<TTrail>> dict, ulong id, Action<TTrail> action)
                {
                    if (dict.ContainsKey(id))
                    {
                        return dict;
                    }
                    else
                    {
                        return dict.Add(id, action);
                    }
                }

                if (trailTraits.HasHost(trail))
                {
                    TTrail hostTrail = trailTraits.GetHost(trail);

                    // F# wouldn't allow me to write this function because this recursive call has a different value for the generic argument T
                    SetValue(trailTraits, hostTrail, undoRecordBox, addToDictionary(undoRecordBox.Value, box.Id, restore));
                }
                else
                {
                    undoRecordBox.Value = addToDictionary(undoRecordBox.Value, box.Id, restore);
                }
            }
            
            box.Value = value;
        }
    }
}
