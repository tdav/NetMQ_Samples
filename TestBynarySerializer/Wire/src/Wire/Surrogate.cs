// -----------------------------------------------------------------------
//   <copyright file="Surrogate.cs" company="Asynkron HB">
//       Copyright (C) 2015-2017 Asynkron HB All rights reserved
//   </copyright>
// -----------------------------------------------------------------------

using System;

namespace Wire
{
    public class Surrogate<TSource, TSurrogate> : Surrogate
    {
        public Surrogate(Func<TSource, TSurrogate> toSurrogate, Func<TSurrogate, TSource> fromSurrogate)
        {
            ToSurrogate = from => toSurrogate((TSource) from)!;
            FromSurrogate = to => fromSurrogate((TSurrogate) to)!;
            From = typeof(TSource);
            To = typeof(TSurrogate);
        }
    }

    public class Surrogate
    {
        public Type From { get; protected set; } = null!;
        public Type To { get; protected set; } = null!;
        public Func<object, object> FromSurrogate { get; protected set; } = null!;
        public Func<object, object> ToSurrogate { get; protected set; } = null!;

        public static Surrogate Create<TSource, TSurrogate>(Func<TSource, TSurrogate> toSurrogate,
            Func<TSurrogate, TSource> fromSurrogate)
        {
            return new Surrogate<TSource, TSurrogate>(toSurrogate, fromSurrogate);
        }

        public bool IsSurrogateFor(Type type)
        {
            return From.IsAssignableFrom(type);
        }
    }
}