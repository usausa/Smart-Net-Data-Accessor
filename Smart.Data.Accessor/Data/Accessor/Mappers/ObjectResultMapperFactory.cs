namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;

    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Selectors;
    using Smart.Reflection.Emit;

    public sealed class ObjectResultMapperFactory : IResultMapperFactory
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

        public static ObjectResultMapperFactory Instance { get; } = new ObjectResultMapperFactory();

        private int typeNo;

        private AssemblyBuilder assemblyBuilder;

        private ModuleBuilder moduleBuilder;

        private ModuleBuilder ModuleBuilder
        {
            get
            {
                if (moduleBuilder == null)
                {
                    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("ObjectResultMapperFactoryAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "ObjectResultMapperFactoryModule");
                }

                return moduleBuilder;
            }
        }

        public bool IsMatch(Type type, MethodInfo mi) => true;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
        {
            var type = typeof(T);
            var selector = (IMappingSelector)context.ServiceProvider.GetService(typeof(IMappingSelector));
            var typeMap = selector.Select(mi, type, columns);

            var converters = typeMap.Constructor.Parameters
                .Select(x => new { x.Index, Converter = context.GetConverter(columns[x.Index].Type, x.Info.ParameterType, x.Info) })
                .Concat(typeMap.Properties
                    .Select(x => new { x.Index, Converter = context.GetConverter(columns[x.Index].Type, x.Info.PropertyType, x.Info) }))
                .Where(x => x.Converter != null)
                .ToDictionary(x => x.Index, x => x.Converter);

            var holder = CreateHolder(typeMap, converters);
            var holderType = holder.GetType();

            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { holderType, typeof(IDataRecord) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Variables
            var objectLocal = converters.Count > 0 ? ilGenerator.DeclareLocal(typeof(object)) : null;
            var valueTypeLocals = typeMap.Constructor.Parameters
                .Select(x => x.Info.ParameterType)
                .Concat(typeMap.Properties.Select(x => x.Info.PropertyType))
                .Distinct()
                .Where(x => x.IsValueType && (x.IsNullableType() || !LdcDictionary.ContainsKey(x)))
                .ToDictionary(x => x, x => ilGenerator.DeclareLocal(x));
            var isShort = valueTypeLocals.Count + (objectLocal != null ? 1 : 0) <= 256;

            // --------------------------------------------------------------------------------
            // Constructor
            // --------------------------------------------------------------------------------

            foreach (var parameterMap in typeMap.Constructor.Parameters)
            {
                var hasValueLabel = ilGenerator.DefineLabel();
                var next = ilGenerator.DefineLabel();

                EmitStackColumnValue(ilGenerator, parameterMap.Index);

                EmitCheckDbNull(ilGenerator);
                ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                // Null
                ilGenerator.Emit(OpCodes.Pop);

                EmitStackDefault(ilGenerator, parameterMap.Info.ParameterType, valueTypeLocals, isShort);

                ilGenerator.Emit(OpCodes.Br_S, next);

                // Value
                ilGenerator.MarkLabel(hasValueLabel);

                if (converters.ContainsKey(parameterMap.Index))
                {
                    EmitConvertByField(ilGenerator, holderType.GetField($"parser{parameterMap.Index}"), objectLocal, isShort);
                }

                EmitTypeConversion(ilGenerator, parameterMap.Info.ParameterType);

                // Next

                ilGenerator.MarkLabel(next);
            }

            // New
            ilGenerator.Emit(OpCodes.Newobj, typeMap.Constructor.Info);

            // --------------------------------------------------------------------------------
            // Property
            // --------------------------------------------------------------------------------

            foreach (var propertyMap in typeMap.Properties)
            {
                var hasValueLabel = ilGenerator.DefineLabel();
                var next = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Dup);

                EmitStackColumnValue(ilGenerator, propertyMap.Index);

                EmitCheckDbNull(ilGenerator);
                ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                // Null
                ilGenerator.Emit(OpCodes.Pop);

                EmitStackDefault(ilGenerator, propertyMap.Info.PropertyType, valueTypeLocals, isShort);

                ilGenerator.Emit(OpCodes.Br_S, next);

                // Value
                ilGenerator.MarkLabel(hasValueLabel);

                if (converters.ContainsKey(propertyMap.Index))
                {
                    EmitConvertByField(ilGenerator, holderType.GetField($"parser{propertyMap.Index}"), objectLocal, isShort);
                }

                EmitTypeConversion(ilGenerator, propertyMap.Info.PropertyType);

                // Next
                ilGenerator.MarkLabel(next);

                // Set
                ilGenerator.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, propertyMap.Info.SetMethod);
            }

            ilGenerator.Emit(OpCodes.Ret);

            var funcType = typeof(Func<,>).MakeGenericType(typeof(IDataRecord), type);
            return (Func<IDataRecord, T>)dynamicMethod.CreateDelegate(funcType, holder);
        }

        private object CreateHolder(TypeMapInfo typeMap, Dictionary<int, Func<object, object>> converters)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            foreach (var parameterMap in typeMap.Constructor.Parameters)
            {
                if (converters.ContainsKey(parameterMap.Index))
                {
                    typeBuilder.DefineField(
                        $"parser{parameterMap.Index}",
                        typeof(Func<object, object>),
                        FieldAttributes.Public);
                }
            }

            foreach (var propertyMap in typeMap.Properties)
            {
                if (converters.ContainsKey(propertyMap.Index))
                {
                    typeBuilder.DefineField(
                        $"parser{propertyMap.Index}",
                        typeof(Func<object, object>),
                        FieldAttributes.Public);
                }
            }

            var typeInfo = typeBuilder.CreateTypeInfo();
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType);

            foreach (var parameterMap in typeMap.Constructor.Parameters)
            {
                if (converters.TryGetValue(parameterMap.Index, out var converter))
                {
                    var field = holderType.GetField($"parser{parameterMap.Index}");
                    field.SetValue(holder, converter);
                }
            }

            foreach (var propertyMap in typeMap.Properties)
            {
                if (converters.TryGetValue(propertyMap.Index, out var converter))
                {
                    var field = holderType.GetField($"parser{propertyMap.Index}");
                    field.SetValue(holder, converter);
                }
            }

            return holder;
        }

        private static void EmitStackColumnValue(ILGenerator ilGenerator, int index)
        {
            ilGenerator.Emit(OpCodes.Ldarg_1);                 // [IDataRecord]
            ilGenerator.EmitLdcI4(index);                       // [IDataRecord][index]
            ilGenerator.Emit(OpCodes.Callvirt, GetValue);     // [Value]
        }

        private static void EmitCheckDbNull(ILGenerator ilGenerator)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Isinst, typeof(DBNull));
        }

        private static void EmitStackDefault(ILGenerator ilGenerator, Type type, Dictionary<Type, LocalBuilder> valueTypeLocals, bool isShort)
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

        private static void EmitConvertByField(ILGenerator ilGenerator, FieldInfo field, LocalBuilder objectLocal, bool isShort)
        {
            ilGenerator.Emit(isShort ? OpCodes.Stloc_S : OpCodes.Stloc, objectLocal);  // [Value] :

            ilGenerator.Emit(OpCodes.Ldarg_0);                                          // [Value] : [Holder]
            ilGenerator.Emit(OpCodes.Ldfld, field);                                     // [Value] : [Converter]

            ilGenerator.Emit(isShort ? OpCodes.Ldloc_S : OpCodes.Ldloc, objectLocal);  // [Converter][Value]

            var method = typeof(Func<object, object>).GetMethod("Invoke");
            ilGenerator.Emit(OpCodes.Callvirt, method);                                 // [Value(Converted)]
        }

        private static void EmitTypeConversion(ILGenerator ilGenerator, Type type)
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
    }
}
