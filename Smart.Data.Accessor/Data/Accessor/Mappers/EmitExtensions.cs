namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Reflection.Emit;

    internal static class EmitExtensions
    {
        private static readonly Dictionary<Type, Action<ILGenerator>> LdcDictionary = new Dictionary<Type, Action<ILGenerator>>
        {
            { typeof(bool), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(byte), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(char), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(short), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(int), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(sbyte), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(ushort), il => il.Emit(OpCodes.Ldc_I4_0) },
            { typeof(uint), il => il.Emit(OpCodes.Ldc_I4_0) },      // Simplicity
            { typeof(long), il => il.Emit(OpCodes.Ldc_I8, 0L) },
            { typeof(ulong), il => il.Emit(OpCodes.Ldc_I8, 0L) },   // Simplicity
            { typeof(float), il => il.Emit(OpCodes.Ldc_R4, 0f) },
            { typeof(double), il => il.Emit(OpCodes.Ldc_R8, 0d) },
            { typeof(IntPtr), il => il.Emit(OpCodes.Ldc_I4_0) },    // Simplicity
            { typeof(UIntPtr), il => il.Emit(OpCodes.Ldc_I4_0) },   // Simplicity
        };

        private static readonly MethodInfo GetValue = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue));

        public static Dictionary<Type, LocalBuilder> DeclareValueTypeLocals(this ILGenerator ilGenerator, IEnumerable<Type> types)
        {
            return types
                .Distinct()
                .Where(x => x.IsValueType && (x.IsNullableType() || !LdcDictionary.ContainsKey(x)))
                .ToDictionary(x => x, ilGenerator.DeclareLocal);
        }

        public static void EmitStackColumnValue(this ILGenerator ilGenerator, int index)
        {
            ilGenerator.Emit(OpCodes.Ldarg_1);                 // [IDataRecord]
            ilGenerator.EmitLdcI4(index);                       // [IDataRecord][index]
            ilGenerator.Emit(OpCodes.Callvirt, GetValue);     // [Value]
        }

        public static void EmitCheckDbNull(this ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Isinst, typeof(DBNull));
        }

        public static void EmitStackDefault(this ILGenerator ilGenerator, Type type, Dictionary<Type, LocalBuilder> valueTypeLocals, bool isShort)
        {
            if (type.IsValueType)
            {
                if (LdcDictionary.TryGetValue(type.IsEnum ? type.GetEnumUnderlyingType() : type, out var action))
                {
                    action(ilGenerator);
                }
                else
                {
                    var local = valueTypeLocals[type];

                    ilGenerator.Emit(isShort ? OpCodes.Ldloca_S : OpCodes.Ldloca, local);
                    ilGenerator.Emit(OpCodes.Initobj, type);
                    ilGenerator.Emit(isShort ? OpCodes.Ldloc_S : OpCodes.Ldloc, local);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
        }

        public static void EmitConvertByField(this ILGenerator ilGenerator, FieldInfo field, LocalBuilder objectLocal, bool isShort)
        {
            ilGenerator.Emit(isShort ? OpCodes.Stloc_S : OpCodes.Stloc, objectLocal);  // [Value] :

            ilGenerator.Emit(OpCodes.Ldarg_0);                                          // [Value] : [Holder]
            ilGenerator.Emit(OpCodes.Ldfld, field);                                     // [Value] : [Converter]

            ilGenerator.Emit(isShort ? OpCodes.Ldloc_S : OpCodes.Ldloc, objectLocal);  // [Converter][Value]

            var method = typeof(Func<object, object>).GetMethod("Invoke");
            ilGenerator.Emit(OpCodes.Callvirt, method);                                 // [Value(Converted)]
        }

        public static void EmitTypeConversion(this ILGenerator ilGenerator, Type type)
        {
            if (type.IsValueType)
            {
                if (type.IsNullableType())
                {
                    var underlyingType = Nullable.GetUnderlyingType(type);
                    var nullableCtor = type.GetConstructor(new[] { underlyingType });

                    ilGenerator.Emit(OpCodes.Unbox_Any, underlyingType);
                    ilGenerator.Emit(OpCodes.Newobj, nullableCtor);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, type);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Castclass, type);
            }
        }

        public static void EmitSetter(this ILGenerator ilGenerator, PropertyInfo pi)
        {
            ilGenerator.Emit(pi.DeclaringType.IsValueType ? OpCodes.Call : OpCodes.Callvirt, pi.SetMethod);
        }
    }
}
