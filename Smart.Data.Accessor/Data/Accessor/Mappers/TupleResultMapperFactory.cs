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

    public sealed class TupleResultMapperFactory : IResultMapperFactory
    {
        public static TupleResultMapperFactory Instance { get; } = new TupleResultMapperFactory();

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
                        new AssemblyName("TupleResultMapperFactoryAssembly"),
                        AssemblyBuilderAccess.Run);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(
                        "TupleResultMapperFactoryModule");
                }

                return moduleBuilder;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public bool IsMatch(Type type, MethodInfo mi)
        {
            return type.IsGenericType && !type.IsNullableType() && (type.GetConstructor(type.GetGenericArguments()) != null);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, MethodInfo mi, ColumnInfo[] columns)
        {
            var type = typeof(T);
            var types = type.GetGenericArguments();
            var isNullableTypes = types.Select(x => x.IsValueType && x.IsNullableType()).ToArray();
            var targetTypes = types.Select((x, i) => isNullableTypes[i] ? Nullable.GetUnderlyingType(x) : x).ToArray();
            var selector = (IMultiMappingSelector)context.ServiceProvider.GetService(typeof(IMultiMappingSelector));
            var typeMaps = selector.Select(mi, targetTypes, columns);
            if (typeMaps is null)
            {
                throw new InvalidOperationException($"Type is not supported for mapper. type=[{type}]");
            }

            var converters = new Dictionary<int, Func<object, object>>();
            foreach (var typeMap in typeMaps)
            {
                TypeMapInfoHelper.BuildConverterMap(typeMap, context, columns, converters);
            }

            var holder = CreateHolder(converters);
            var holderType = holder.GetType();

            var dynamicMethod = new DynamicMethod(string.Empty, type, new[] { holderType, typeof(IDataRecord) }, true);
            var ilGenerator = dynamicMethod.GetILGenerator();

            // Variables
            var objectLocal = converters.Count > 0 ? ilGenerator.DeclareLocal(typeof(object)) : null;
            var valueTypeLocals = ilGenerator.DeclareValueTypeLocals(
                typeMaps.Where(x => x.Constructor is null).Select(x => x.Type).Concat(
                    typeMaps.SelectMany(TypeMapInfoHelper.EnumerateTypes)));

            for (var i = 0; i < typeMaps.Length; i++)
            {
                var originalType = types[i];
                var isNullableType = isNullableTypes[i];
                var typeMap = typeMaps[i];
                var ctorLocal = valueTypeLocals.GetValueOrDefault(typeMap.Type);

                var nullLabel = i > 0 ? ilGenerator.DefineLabel() : default;
                var nextTypeLabel = i > 0 ? ilGenerator.DefineLabel() : default;

                // --------------------------------------------------------------------------------
                // Constructor
                // --------------------------------------------------------------------------------

                if (ctorLocal != null)
                {
                    ilGenerator.Emit(OpCodes.Ldloca, ctorLocal);
                }

                if (typeMap.Constructor != null)
                {
                    foreach (var parameterMap in typeMap.Constructor.Parameters)
                    {
                        var hasValueLabel = ilGenerator.DefineLabel();
                        var next = ilGenerator.DefineLabel();

                        // Stack
                        ilGenerator.EmitStackColumnValue(parameterMap.Index);

                        ilGenerator.EmitCheckDbNull();
                        ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                        // Null
                        // TODO
                        ilGenerator.Emit(OpCodes.Pop);

                        ilGenerator.EmitStackDefault(parameterMap.Info.ParameterType, valueTypeLocals);

                        ilGenerator.Emit(OpCodes.Br_S, next);

                        // Value:
                        ilGenerator.MarkLabel(hasValueLabel);

                        if (converters.ContainsKey(parameterMap.Index))
                        {
                            ilGenerator.EmitConvertByField(holderType.GetField($"parser{parameterMap.Index}"), objectLocal);
                        }

                        ilGenerator.EmitTypeConversion(parameterMap.Info.ParameterType);

                        // Next:
                        ilGenerator.MarkLabel(next);
                    }

                    if (ctorLocal != null)
                    {
                        // Struct call constructor
                        ilGenerator.Emit(OpCodes.Call, typeMap.Constructor.Info);
                    }
                    else
                    {
                        // Class new
                        ilGenerator.Emit(OpCodes.Newobj, typeMap.Constructor.Info);
                    }
                }
                else
                {
                    // Struct init
                    ilGenerator.Emit(OpCodes.Initobj, typeMap.Type);
                }

                if (ctorLocal != null)
                {
                    ilGenerator.Emit(OpCodes.Ldloca, ctorLocal);
                }

                // --------------------------------------------------------------------------------
                // Property
                // --------------------------------------------------------------------------------

                foreach (var propertyMap in typeMap.Properties)
                {
                    var hasValueLabel = ilGenerator.DefineLabel();
                    var next = ilGenerator.DefineLabel();

                    ilGenerator.Emit(OpCodes.Dup);

                    // Stack
                    ilGenerator.EmitStackColumnValue(propertyMap.Index);

                    ilGenerator.EmitCheckDbNull();
                    ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                    // Null
                    // TODO
                    ilGenerator.Emit(OpCodes.Pop);

                    ilGenerator.EmitStackDefault(propertyMap.Info.PropertyType, valueTypeLocals);

                    ilGenerator.Emit(OpCodes.Br_S, next);

                    // Value:
                    ilGenerator.MarkLabel(hasValueLabel);

                    if (converters.ContainsKey(propertyMap.Index))
                    {
                        ilGenerator.EmitConvertByField(holderType.GetField($"parser{propertyMap.Index}"), objectLocal);
                    }

                    ilGenerator.EmitTypeConversion(propertyMap.Info.PropertyType);

                    // Next:
                    ilGenerator.MarkLabel(next);

                    // Set
                    ilGenerator.EmitSetter(propertyMap.Info);
                }

                // --------------------------------------------------------------------------------
                // Entity
                // --------------------------------------------------------------------------------

                if (ctorLocal != null)
                {
                    ilGenerator.Emit(OpCodes.Ldobj, typeMap.Type);
                }

                if (isNullableType)
                {
                    ilGenerator.EmitChangeToNullable(type);
                }

                if (i > 0)
                {
                    // Next
                    ilGenerator.Emit(OpCodes.Br_S, nextTypeLabel);

                    // Null:
                    ilGenerator.MarkLabel(nullLabel);

                    ilGenerator.EmitStackDefault(originalType, valueTypeLocals);

                    // Next:
                    ilGenerator.MarkLabel(nextTypeLabel);
                }
            }

            ilGenerator.Emit(OpCodes.Newobj, type.GetConstructor(types));

            ilGenerator.Emit(OpCodes.Ret);

            return (Func<IDataRecord, T>)dynamicMethod.CreateDelegate(typeof(Func<IDataRecord, T>), holder);
        }

        private object CreateHolder(Dictionary<int, Func<object, object>> converters)
        {
            var typeBuilder = ModuleBuilder.DefineType(
                $"Holder_{typeNo}",
                TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit);
            typeNo++;

            var indexes = converters.Select(x => x.Key).OrderBy(x => x).ToList();

            foreach (var index in indexes)
            {
                if (converters.ContainsKey(index))
                {
                    typeBuilder.DefineField(
                        $"parser{index}",
                        typeof(Func<object, object>),
                        FieldAttributes.Public);
                }
            }

            var typeInfo = typeBuilder.CreateTypeInfo();
            var holderType = typeInfo.AsType();
            var holder = Activator.CreateInstance(holderType);

            foreach (var index in indexes)
            {
                if (converters.TryGetValue(index, out var converter))
                {
                    var field = holderType.GetField($"parser{index}");
                    field.SetValue(holder, converter);
                }
            }

            return holder;
        }
    }
}
