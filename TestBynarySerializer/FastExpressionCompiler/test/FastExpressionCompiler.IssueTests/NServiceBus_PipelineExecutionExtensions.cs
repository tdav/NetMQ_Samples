namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Pipeline;

    using FastExpressionCompiler;
    using Expr = System.Linq.Expressions.Expression;
    using static System.Linq.Expressions.Expression;

    static class PipelineExecutionExtensions
    {
        // ReSharper disable once SuggestBaseTypeForParameter
        public static Func<TRootContext, Task> CreatePipelineExecutionFuncFor<TRootContext>(this IBehavior[] behaviors)
            where TRootContext : IBehaviorContext
        {
            return (Func<TRootContext, Task>)behaviors.CreatePipelineExecutionExpression();
        }

        /// <code>
        /// rootContext
        ///    => behavior1.Invoke(rootContext,
        ///       context1 => behavior2.Invoke(context1,
        ///        ...
        ///          context{N} => behavior{N}.Invoke(context{N},
        ///             context{N+1} => TaskEx.Completed))
        /// </code>
        public static Delegate CreatePipelineExecutionExpression(this IBehavior[] behaviors, List<Expr> expressions = null)
        {
            Delegate lambdaExpression = null;
            var behaviorCount = behaviors.Length - 1;
            // We start from the end of the list know the lambda expressions deeper in the call stack in advance
            for (var i = behaviorCount; i >= 0; i--)
            {
                var currentBehavior = behaviors[i];
                var behaviorInterfaceType = currentBehavior.GetType().GetBehaviorInterface();
                if (behaviorInterfaceType == null)
                {
                    throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext>");
                }
                // Select the method on the type which was implemented from the behavior interface.
                var methodInfo = currentBehavior.GetType().GetInterfaceMap(behaviorInterfaceType).TargetMethods.FirstOrDefault();
                if (methodInfo == null)
                {
                    throw new InvalidOperationException("Behaviors must implement IBehavior<TInContext, TOutContext> and provide an invocation method.");
                }

                var genericArguments = behaviorInterfaceType.GetGenericArguments();
                var inContextType = genericArguments[0];

                var inContextParameter = Parameter(inContextType, $"context{i}");

                if (i == behaviorCount)
                {
                    if (currentBehavior is IPipelineTerminator)
                    {
                        inContextType = typeof(PipelineTerminator<>.ITerminatingContext).MakeGenericType(inContextType);
                    }
                    var doneDelegate = CreateDoneDelegate(inContextType, i);
                    lambdaExpression = CreateBehaviorCallDelegate(currentBehavior, methodInfo, inContextParameter, doneDelegate, expressions);
                    continue;
                }

                lambdaExpression = CreateBehaviorCallDelegate(currentBehavior, methodInfo, inContextParameter, lambdaExpression, expressions);
            }

            return lambdaExpression;
        }

        // ReSharper disable once SuggestBaseTypeForParameter

        /// <code>
        /// context{i} => behavior.Invoke(context{i}, context{i+1} => previous)
        /// </code>>
        static Delegate CreateBehaviorCallDelegate(IBehavior currentBehavior, MethodInfo methodInfo, ParameterExpression outerContextParam, Delegate previous, List<Expr> expressions = null)
        {
            var body = Call(Constant(currentBehavior), methodInfo, outerContextParam, Constant(previous));
            var lambdaExpression = Lambda(body, outerContextParam);
            expressions?.Add(lambdaExpression);
            return lambdaExpression.CompileFast();
        }

        /// <code>
        /// context{i} => return TaskEx.CompletedTask;
        /// </code>>
        static Delegate CreateDoneDelegate(Type inContextType, int i)
        {
            var innerContextParam = Parameter(inContextType, $"context{i + 1}");
            return Lambda(Constant(Task.FromResult(0)), innerContextParam).CompileFast();
        }
    }
}
