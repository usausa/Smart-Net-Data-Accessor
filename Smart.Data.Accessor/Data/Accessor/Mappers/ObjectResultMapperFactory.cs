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
            var holder = CreateHolder(typeMap);
            var holderType = holder.GetType();

            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { holderType, typeof(IDataRecord) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Variables
            var objectLocal = typeMap.Constructor.Parameters.Any(x => x.Converter != null) || typeMap.Properties.Any(x => x.Converter != null)
                ? ilGenerator.DeclareLocal(typeof(object))
                : null;
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

                if (parameterMap.Converter != null)
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
                var propertyType = propertyMap.Info.PropertyType;

                var hasValueLabel = ilGenerator.DefineLabel();
                var next = ilGenerator.DefineLabel();

                ilGenerator.Emit(OpCodes.Dup);  // [T][T]

                ilGenerator.Emit(OpCodes.Ldarg_1); // [T][T][IDataRecord]
                ilGenerator.EmitLdcI4(propertyMap.Index); // [T][T][IDataRecord][index]

                ilGenerator.Emit(OpCodes.Callvirt, GetValue);   // [T][T][Value]

                // Check DBNull
                ilGenerator.Emit(OpCodes.Dup);  // [T][T][Value][Value]
                ilGenerator.Emit(OpCodes.Isinst, typeof(DBNull));   // [T][T][Value]
                ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                // ----------------------------------------
                // Null
                // ----------------------------------------

                // [T][T][Value]

                ilGenerator.Emit(OpCodes.Pop);  // [T][T]
                if (propertyType.IsValueType)
                {
                    if (LdcDictionary.TryGetValue(propertyType.IsEnum ? propertyType.GetEnumUnderlyingType() : propertyType, out var action))
                    {
                        action(ilGenerator);
                    }
                    else
                    {
                        var local = valueTypeLocals[propertyType];

                        ilGenerator.Emit(isShort ? OpCodes.Ldloca_S : OpCodes.Ldloca, local);
                        ilGenerator.Emit(OpCodes.Initobj, propertyType);
                        ilGenerator.Emit(isShort ? OpCodes.Ldloc_S : OpCodes.Ldloc, local);
                    }
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldnull);
                }

                ilGenerator.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, propertyMap.Info.SetMethod);

                ilGenerator.Emit(OpCodes.Br_S, next);

                // ----------------------------------------
                // Value
                // ----------------------------------------

                // [T][T][Value]

                ilGenerator.MarkLabel(hasValueLabel);

                if (propertyMap.Converter != null)
                {
                    ilGenerator.Emit(isShort ? OpCodes.Stloc_S : OpCodes.Stloc, objectLocal);  // [Value] : [T][T]

                    var field = holderType.GetField($"parser{propertyMap.Index}");
                    ilGenerator.Emit(OpCodes.Ldarg_0);                                          // [Value] : [T][T][Holder]
                    ilGenerator.Emit(OpCodes.Ldfld, field);                                     // [Value] : [T][T][Converter]

                    ilGenerator.Emit(isShort ? OpCodes.Ldloc_S : OpCodes.Ldloc, objectLocal);  // [T][T][Converter][Value]

                    var method = typeof(Func<object, object>).GetMethod("Invoke");
                    ilGenerator.Emit(OpCodes.Callvirt, method); // [T][T][Value(Converted)]
                }

                // [MEMO] 最適化Converterがある場合は以下の共通ではなくなる
                if (propertyType.IsValueType)
                {
                    if (propertyType.IsNullableType())
                    {
                        var underlyingType = Nullable.GetUnderlyingType(propertyType);
                        var nullableCtor = propertyType.GetConstructor(new[] { underlyingType });

                        ilGenerator.Emit(OpCodes.Unbox_Any, underlyingType);
                        ilGenerator.Emit(OpCodes.Newobj, nullableCtor);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, propertyType);
                    }
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Castclass, propertyType);
                }

                ilGenerator.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, propertyMap.Info.SetMethod);

                // ----------------------------------------
                // Next
                // ----------------------------------------

                ilGenerator.MarkLabel(next);

                //var hasValueLabel = ilGenerator.DefineLabel();
                //var next = ilGenerator.DefineLabel();

                //ilGenerator.Emit(OpCodes.Dup);

                //EmitStackColumnValue(ilGenerator, propertyMap.Index);

                //EmitCheckDbNull(ilGenerator);
                //ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                //// Null
                //ilGenerator.Emit(OpCodes.Pop);

                //EmitStackDefault(ilGenerator, propertyMap.Info.PropertyType, valueTypeLocals, isShort);

                //ilGenerator.Emit(OpCodes.Br_S, next);

                //// Value
                //ilGenerator.MarkLabel(hasValueLabel);

                //if (propertyMap.Converter != null)
                //{
                //    EmitConvertByField(ilGenerator, holderType.GetField($"parser{propertyMap.Index}"), objectLocal, isShort);
                //}

                //EmitTypeConversion(ilGenerator, propertyMap.Info.PropertyType);

                //// Next
                //ilGenerator.MarkLabel(next);

                //// Set
                //ilGenerator.Emit(type.IsValueType ? OpCodes.Call : OpCodes.Callvirt, propertyMap.Info.SetMethod);
            }

            ilGenerator.Emit(OpCodes.Ret);

            var funcType = typeof(Func<,>).MakeGenericType(typeof(IDataRecord), type);
            return (Func<IDataRecord, T>)dynamicMethod.CreateDelegate(funcType, holder);
        }

        private object CreateHolder(TypeMapInfo typeMap)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            foreach (var parameterMap in typeMap.Constructor.Parameters.Where(x => x.Converter != null))
            {
                typeBuilder.DefineField(
                    $"parser{parameterMap.Index}",
                    typeof(Func<object, object>),
                    FieldAttributes.Public);
            }

            foreach (var propertyMap in typeMap.Properties.Where(x => x.Converter != null))
            {
                typeBuilder.DefineField(
                    $"parser{propertyMap.Index}",
                    typeof(Func<object, object>),
                    FieldAttributes.Public);
            }

            var typeInfo = typeBuilder.CreateTypeInfo();
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType);

            foreach (var parameterMap in typeMap.Constructor.Parameters.Where(x => x.Converter != null))
            {
                var field = holderType.GetField($"parser{parameterMap.Index}");
                field.SetValue(holder, parameterMap.Converter);
            }

            foreach (var propertyMap in typeMap.Properties.Where(x => x.Converter != null))
            {
                var field = holderType.GetField($"parser{propertyMap.Index}");
                field.SetValue(holder, propertyMap.Converter);
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
