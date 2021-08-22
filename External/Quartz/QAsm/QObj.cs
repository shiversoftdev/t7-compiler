using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Quartz.QAsm
{
    public class QObj
    {
        private HashSet<QVariable> ReadOnlyData;
        private HashSet<QVariable> ExternCache;

        private Dictionary<string, QContextEntry> ContextualEntries;
        private QContextEntry RootContext;
        private Dictionary<string, QType> EnvTypes;

        public QObj()
        {
            ContextualEntries = new Dictionary<string, QContextEntry>();
            ReadOnlyData = new HashSet<QVariable>();
            ExternCache = new HashSet<QVariable>();
            EnvTypes = new Dictionary<string, QType>();
            RegisterBuiltinTypes();
        }

        private void RegisterBuiltinTypes()
        {
            EnvTypes["qword"] = new QType("qword", typeof(ulong));
            EnvTypes["dword"] = new QType("dword", typeof(uint));
            EnvTypes["word"] = new QType("word", typeof(ushort));
            EnvTypes["byte"] = new QType("byte", typeof(byte));
            EnvTypes["bool"] = new QType("bool", typeof(bool));
            EnvTypes["string"] = new QType("string", typeof(string), -1, true);
        }

        public QType LocateType(string typename)
        {
            if(EnvTypes.TryGetValue(typename, out QType value))
            {
                return value;
            }
            throw new KeyNotFoundException($"Unknown type '{typename}'");
        }

        /// <summary>
        /// Register a context entry, later used for lookups
        /// </summary>
        /// <param name="fullContext"></param>
        /// <param name="entry"></param>
        public void PutContext(string fullContext, QContextEntry entry)
        {
            if(fullContext is null)
            {
                if(RootContext != null)
                {
                    throw new InvalidOperationException("Cannot put a root context when a root context already exists");
                }
                RootContext = entry;
                return;
            }
            if(ContextualEntries.ContainsKey(fullContext))
            {
                throw new DuplicateNameException($"Name '{fullContext}' already exists.");
            }
            ContextualEntries[fullContext] = entry;
        }

        public void PutReadonly(QVariable variable)
        {
            ReadOnlyData.Add(variable);
        }

        public void PutExtern(QVariable variable)
        {
            ExternCache.Add(variable);
        }
    }

    public class QContextEntry
    {
        public const string CONTEXT_ANONYMOUS = "anon_0";
        public readonly string Name;
        public readonly string ParentContext;
        public string FullContext => ParentContext is null ? Name : $"{ParentContext}.{Name}";
        public object Value { private set; get; }

        public QContextEntry(object value, string context, string parentContext)
        {
            Value = value;
            ParentContext = parentContext;
            Name = context;
        }
    }
}
