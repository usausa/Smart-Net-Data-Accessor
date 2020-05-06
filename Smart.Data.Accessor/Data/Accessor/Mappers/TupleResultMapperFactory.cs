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
            var objectLocal = ilGenerator.DeclareLocal(typeof(object));
            var valueTypeLocals = ilGenerator.DeclareValueTypeLocals(
                types.Concat(
                    typeMaps.Where(x => x.Constructor is null).Select(x => x.Type).Concat(
                        typeMaps.SelectMany(TypeMapInfoHelper.EnumerateTypes))));

            for (var i = 0; i < typeMaps.Length; i++)
            {
                var originalType = types[i];
                var isNullableType = isNullableTypes[i];
                var typeMap = typeMaps[i];
                var ctorLocal = valueTypeLocals.GetValueOrDefault(typeMap.Type);

                var nullCheck = i > 0;
                var nextTypeLabel = nullCheck ? ilGenerator.DefineLabel() : default;
                var constructorCalled = false;

                // --------------------------------------------------------------------------------
                // Constructor
                // --------------------------------------------------------------------------------

                if ((typeMap.Constructor != null) && ((i == 0) || (typeMap.Constructor.Parameters.Count > 0)))
                {
                    for (var j = 0; j < typeMap.Constructor.Parameters.Count; j++)
                    {
                        var parameterMap = typeMap.Constructor.Parameters[j];

                        var hasValueLabel = ilGenerator.DefineLabel();
                        var next = ilGenerator.DefineLabel();

                        // Stack column value
                        ilGenerator.EmitGetColumnValue(parameterMap.Index);

                        // Check value is NULL
                        ilGenerator.EmitCheckDbNull();
                        ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                        // Null
                        if ((j == 0) && nullCheck)
                        {
                            // Set entity to default

                            // Pop column value
                            ilGenerator.Emit(OpCodes.Pop);

                            ilGenerator.EmitStackDefaultValue(originalType, valueTypeLocals);

                            ilGenerator.Emit(OpCodes.Br, nextTypeLabel);
                        }
                        else
                        {
                            // Set default to entity property
                            ilGenerator.Emit(OpCodes.Pop);

                            ilGenerator.EmitStackDefaultValue(parameterMap.Info.ParameterType, valueTypeLocals);

                            ilGenerator.Emit(OpCodes.Br_S, next);
                        }

                        // Value:
                        ilGenerator.MarkLabel(hasValueLabel);

                        if (converters.ContainsKey(parameterMap.Index))
                        {
                            ilGenerator.EmitValueConvertByField(holderType.GetField($"parser{parameterMap.Index}"), objectLocal);
                        }

                        ilGenerator.EmitTypeConversionForProperty(parameterMap.Info.ParameterType);

                        // Next:
                        ilGenerator.MarkLabel(next);
                    }

                    // Class new
                    ilGenerator.Emit(OpCodes.Newobj, typeMap.Constructor.Info);

                    constructorCalled = true;
                }

                // --------------------------------------------------------------------------------
                // Property
                // --------------------------------------------------------------------------------

                for (var j = 0; j < typeMap.Properties.Count; j++)
                {
                    var propertyMap = typeMap.Properties[j];

                    var hasValueLabel = ilGenerator.DefineLabel();
                    var next = ilGenerator.DefineLabel();

                    // Stack entity
                    if (constructorCalled)
                    {
                        ilGenerator.Emit(OpCodes.Dup);
                    }

                    // Stack column value
                    ilGenerator.EmitGetColumnValue(propertyMap.Index);

                    // Check value is NULL
                    ilGenerator.EmitCheckDbNull();
                    ilGenerator.Emit(OpCodes.Brfalse_S, hasValueLabel);

                    // Null
                    if ((j == 0) && nullCheck)
                    {
                        // Set entity to default

                        // Pop column value
                        ilGenerator.Emit(OpCodes.Pop);

                        ilGenerator.EmitStackDefaultValue(originalType, valueTypeLocals);

                        ilGenerator.Emit(OpCodes.Br, nextTypeLabel);
                    }
                    else
                    {
                        // Set default to entity property
                        ilGenerator.Emit(OpCodes.Pop);

                        ilGenerator.EmitStackDefaultValue(propertyMap.Info.PropertyType, valueTypeLocals);

                        ilGenerator.Emit(OpCodes.Br_S, next);
                    }

                    // Value:
                    ilGenerator.MarkLabel(hasValueLabel);

                    if (!constructorCalled)
                    {
                        // Store column value
                        ilGenerator.EmitStloc(objectLocal);

                        if (ctorLocal is null)
                        {
                            // Class new
                            ilGenerator.Emit(OpCodes.Newobj, typeMap.Constructor.Info);
                        }
                        else
                        {
                            // Struct init
                            ilGenerator.EmitStackStruct(typeMap.Type, ctorLocal);
                        }

                        constructorCalled = true;

                        ilGenerator.Emit(OpCodes.Dup);

                        // Load column value
                        ilGenerator.EmitLdloc(objectLocal);
                    }

                    if (converters.ContainsKey(propertyMap.Index))
                    {
                        ilGenerator.EmitValueConvertByField(holderType.GetField($"parser{propertyMap.Index}"), objectLocal);
                    }

                    ilGenerator.EmitTypeConversionForProperty(propertyMap.Info.PropertyType);

                    // Next:
                    ilGenerator.MarkLabel(next);

                    // Set
                    ilGenerator.EmitCallMethod(propertyMap.Info.SetMethod);
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
                    ilGenerator.EmitValueToNullableType(type);
                }

                if (nullCheck)
                {
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
