using MirDB;
using Server.Envir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Server
{
    public static class ImportReferenceResolver
    {
        private sealed class ReferenceContext
        {
            public DBObject Owner { get; init; }
            public PropertyInfo Property { get; init; }
        }

        private sealed class DeferredReference
        {
            public DBObject Owner { get; init; }
            public PropertyInfo Property { get; init; }
            public Type ReferenceType { get; init; }
            public List<string> IdentityValues { get; init; }
        }

        [ThreadStatic]
        private static ReferenceContext _currentContext;

        private static readonly List<DeferredReference> PendingReferences = new();

        private static bool EnableDeferredResolution { get; set; } = true;

        public static void SetDeferredResolution(bool enabled)
        {
            EnableDeferredResolution = enabled;
            PendingReferences.Clear();
        }

        public static void SetContext(DBObject owner, PropertyInfo property)
        {
            _currentContext = new ReferenceContext
            {
                Owner = owner,
                Property = property
            };
        }

        public static void ClearContext()
        {
            _currentContext = null;
        }

        public static bool TryAddMissingReference(Type referenceType, List<string> identityValues)
        {
            if (!EnableDeferredResolution || _currentContext == null)
            {
                return false;
            }

            lock (PendingReferences)
            {
                PendingReferences.Add(new DeferredReference
                {
                    Owner = _currentContext.Owner,
                    Property = _currentContext.Property,
                    ReferenceType = referenceType,
                    IdentityValues = identityValues.ToList()
                });
            }

            SEnvir.Log($"已延后处理缺失的引用：类型“{referenceType.Name}”，标识值“{string.Join('/', identityValues)}”，位置“{_currentContext.Owner?.GetType().Name}.{_currentContext.Property?.Name}”。");

            return true;
        }

        public static (int resolved, int remaining) ResolvePendingReferences(Session session)
        {
            List<DeferredReference> pendingSnapshot;

            lock (PendingReferences)
            {
                pendingSnapshot = PendingReferences.ToList();
            }

            if (pendingSnapshot.Count == 0)
            {
                return (0, 0);
            }

            List<DeferredReference> stillPending = new();
            int resolved = 0;

            foreach (var reference in pendingSnapshot)
            {
                DBObject resolvedReference = TryResolveReference(session, reference);

                if (resolvedReference != null)
                {
                    reference.Property.SetValue(reference.Owner, resolvedReference);
                    resolved++;

                    SEnvir.Log($"已解析延后的引用“{reference.Owner.GetType().Name}.{reference.Property.Name}”。");
                }
                else
                {
                    stillPending.Add(reference);

                    SEnvir.Log($"待处理的引用仍然缺失：类型“{reference.ReferenceType.Name}”，标识值“{string.Join('/', reference.IdentityValues)}”，位置“{reference.Owner.GetType().Name}.{reference.Property.Name}”。");
                }
            }

            lock (PendingReferences)
            {
                PendingReferences.Clear();
                PendingReferences.AddRange(stillPending);
            }

            return (resolved, stillPending.Count);
        }

        private static DBObject TryResolveReference(Session session, DeferredReference reference)
        {
            var converterType = typeof(DBObjectReferenceConverter<>).MakeGenericType(reference.ReferenceType);

            object converter = Activator.CreateInstance(converterType, session);

            MethodInfo getObjectMethod = converterType.GetMethod("GetObjectFromIdentity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            return getObjectMethod?.Invoke(converter, new object[] { reference.IdentityValues, false }) as DBObject;
        }
    }
}
