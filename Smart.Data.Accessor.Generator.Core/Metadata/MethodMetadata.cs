namespace Smart.Data.Accessor.Generator.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator.Helpers;
    using Smart.Data.Accessor.Helpers;
    using Smart.Data.Accessor.Nodes;

    internal sealed class MethodMetadata
    {
        public int No { get; }

        public bool Optimize { get; }

        public MethodInfo MethodInfo { get; }

        public CommandType CommandType { get; }

        public MethodType MethodType { get; }

        public bool ReturnValueAsResult { get; }

        public IReadOnlyList<INode> Nodes { get; }

        public IReadOnlyList<ParameterEntry> Parameters { get; }

        public IReadOnlyList<DynamicParameterEntry> DynamicParameters { get; }

        // Helper

        public bool IsAsync { get; }

        public Type EngineResultType { get; }

        // Method attribute

        public ProviderAttribute? Provider { get; }

        public CommandTimeoutAttribute? Timeout { get; }

        public SqlSizeAttribute? SqlSize { get; }

        // Parameter

        public ParameterInfo? TimeoutParameter { get; }

        public ParameterInfo? CancelParameter { get; }

        public ParameterInfo? ConnectionParameter { get; }

        public ParameterInfo? TransactionParameter { get; }

        // Helper

        public bool HasConnectionParameter => ConnectionParameter is not null || TransactionParameter is not null;

        public MethodMetadata(
            int no,
            MethodInfo mi,
            CommandType commandType,
            MethodType memberType,
            bool returnValueAsResult,
            IReadOnlyList<INode> nodes,
            IReadOnlyList<ParameterEntry> parameters,
            IReadOnlyList<DynamicParameterEntry> dynamicParameters)
        {
            No = no;
            MethodInfo = mi;
            CommandType = commandType;
            MethodType = memberType;
            ReturnValueAsResult = returnValueAsResult;
            Nodes = nodes;
            Parameters = parameters;
            DynamicParameters = dynamicParameters;

            Optimize = mi.GetCustomAttribute<OptimizeAttribute>()?.Value ??
                       mi.DeclaringType!.GetCustomAttribute<OptimizeAttribute>()?.Value ??
                       mi.DeclaringType!.Assembly.GetCustomAttribute<OptimizeAttribute>()?.Value ??
                       false;

            var isAsyncEnumerable = GeneratorHelper.IsAsyncEnumerable(mi.ReturnType);
            IsAsync = mi.ReturnType.GetMethod(nameof(Task.GetAwaiter)) is not null || isAsyncEnumerable;
            EngineResultType = !IsAsync || isAsyncEnumerable
                ? mi.ReturnType
                : (mi.ReturnType.IsGenericType ? mi.ReturnType.GetGenericArguments()[0] : typeof(void));

            Provider = mi.GetCustomAttribute<ProviderAttribute>();
            Timeout = mi.GetCustomAttribute<CommandTimeoutAttribute>();
            SqlSize = mi.GetCustomAttribute<SqlSizeAttribute>();

            foreach (var pmi in mi.GetParameters())
            {
                if (ParameterHelper.IsTimeoutParameter(pmi))
                {
                    TimeoutParameter = pmi;
                }

                if (ParameterHelper.IsCancellationTokenParameter(pmi))
                {
                    CancelParameter = pmi;
                }

                if (ParameterHelper.IsConnectionParameter(pmi))
                {
                    ConnectionParameter = pmi;
                }

                if (ParameterHelper.IsTransactionParameter(pmi))
                {
                    TransactionParameter = pmi;
                }
            }
        }

        public ParameterEntry? FindParameterByName(string name)
        {
            return Parameters.FirstOrDefault(x => x.Name == name);
        }

        public DynamicParameterEntry? FindDynamicParameterByName(string name)
        {
            return DynamicParameters.FirstOrDefault(x => x.Name == name);
        }
    }
}
