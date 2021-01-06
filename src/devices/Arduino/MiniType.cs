﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Arduino
{
    [ArduinoReplacement(typeof(System.Type), true, true)]
    internal class MiniType
    {
#pragma warning disable 414, SX1309
        // This is used by firmware code directly. Do not reorder the members without checking the firmware
        // The member contains the token of the class declaration
        private Int32 m_handle;
#pragma warning restore 414

        [ArduinoImplementation(ArduinoImplementation.TypeCtor)]
        protected MiniType()
        {
            // This ctor is never executed - the variable values are pushed directly
            m_handle = 0;
        }

        public virtual bool IsGenericType
        {
            get
            {
                // All types that have some generics return true here, whether they're open or closed. Nullable also returns true
                return (m_handle & ExecutionSet.GenericTokenMask) != 0;
            }
        }

        public virtual bool IsEnum
        {
            [ArduinoImplementation(ArduinoImplementation.TypeIsEnum)]
            get
            {
                // Needs support in the backend
                return false;
            }
        }

        public Assembly? Assembly
        {
            get
            {
                return null;
            }
        }

        public virtual RuntimeTypeHandle TypeHandle
        {
            [ArduinoImplementation(ArduinoImplementation.TypeTypeHandle)]
            get
            {
                return default;
            }
        }

        public bool IsValueType
        {
            [ArduinoImplementation(ArduinoImplementation.TypeIsValueType)]
            get
            {
                return false;
            }
        }

        [ArduinoImplementation(ArduinoImplementation.TypeGetTypeFromHandle)]
        public static Type GetTypeFromHandle(RuntimeTypeHandle handle)
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(MiniType? a, MiniType? b)
        {
            if (ReferenceEquals(a, null))
            {
                return ReferenceEquals(b, null);
            }

            return a.Equals(b);
        }

        public static bool operator !=(MiniType? a, MiniType? b)
        {
            if (ReferenceEquals(a, null))
            {
                return !ReferenceEquals(b, null);
            }

            return !a.Equals(b);
        }

        internal virtual RuntimeTypeHandle GetTypeHandleInternal()
        {
            return TypeHandle;
        }

        [ArduinoImplementation(ArduinoImplementation.TypeEquals)]
        public override bool Equals(object? obj)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeGetHashCode)]
        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeMakeGenericType)]
        public virtual Type MakeGenericType(params Type[] types)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeIsAssignableTo)]
        public virtual bool IsAssignableTo(Type otherType)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeIsAssignableFrom)]
        public virtual bool IsAssignableFrom(Type c)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeIsSubclassOf)]
        public virtual bool IsSubclassOf(Type c)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeGetGenericTypeDefinition)]
        public virtual Type GetGenericTypeDefinition()
        {
            throw new InvalidOperationException();
        }

        [ArduinoImplementation(ArduinoImplementation.TypeGetGenericArguments)]
        public virtual Type[] GetGenericArguments()
        {
            return new Type[0];
        }

        public static TypeCode GetTypeCode(Type type)
        {
            return TypeCode.Empty;
        }

        public virtual Type GetEnumUnderlyingType()
        {
            return typeof(Int32);
        }

        [ArduinoImplementation(ArduinoImplementation.CreateInstanceForAnotherGenericParameter)]
        public static object? CreateInstanceForAnotherGenericParameter(Type? type1, Type? type2)
        {
            return null;
        }
    }
}