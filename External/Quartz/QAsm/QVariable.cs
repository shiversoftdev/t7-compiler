using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Quartz.QAsm
{
    public class QVariable
    {
        /// <summary>
        /// Type info for this variable
        /// </summary>
        public readonly QType TypeInfo;
        public bool IsConst;
        public object Value { get; private set; }

        private QVariable(QType typeinfo, bool isConst)
        {
            TypeInfo = typeinfo;
            IsConst = isConst;
        }

        public static QVariable AllocConst(string typename, QObj environment, string contextName, string rootContext)
        {
            var type = environment.LocateType(typename);
            var variable = new QVariable(type, true);
            QContextEntry context = new QContextEntry(variable, contextName, rootContext);
            environment.PutContext(context.FullContext, context);
            return variable;
        }

        public void SetValue(object value)
        {
            if(!IsConst)
            {
                throw new InvalidOperationException("Cannot set the value of a variable which is not constant.");
            }
            if(value is null)
            {
                throw new NullReferenceException("Tried to call SetValue with a null value");
            }
            if(!TypeInfo.CanBox(value.GetType()))
            {
                throw new InvalidCastException($"Type mismatch for const declaration: {value.GetType()} is not a {TypeInfo.UnderlyingType}");
            }
            Value = value;
        }
    }

    public class QType
    {
        public readonly string TypeName;
        public readonly Type UnderlyingType;
        public readonly bool IsReferenceType;
        private readonly int __sizeoverride;
        public int Size => IsReferenceType ? 8 : (__sizeoverride < 0 ? Marshal.SizeOf(UnderlyingType) : __sizeoverride);

        public bool CanBox(Type type) // compare type against our underlying type. If we can box it, return true.
        {
            return UnderlyingType == type; // TODO handle int to uint conversion, etc.
        }

        public QType(string typename, Type underlying, int sizeOverride = -1, bool isByReference = false)
        {
            UnderlyingType = underlying;
            TypeName = typename;
            __sizeoverride = sizeOverride;
            IsReferenceType = isByReference;
        }
    }
}
