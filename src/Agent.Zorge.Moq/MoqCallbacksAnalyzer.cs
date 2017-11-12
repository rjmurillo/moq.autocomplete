using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Agent.Zorge.Moq
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MoqCallbacksAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString MoqRightNumberOfParametersRuleTitle = new LocalizableResourceString(nameof(Resources.MoqRightNumberOfParametersRuleTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MoqRightNumberOfParametersRuleMessageFormat = new LocalizableResourceString(nameof(Resources.MoqRightNumberOfParametersRuleMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MoqRightNumberOfParametersRuleDescription = new LocalizableResourceString(nameof(Resources.MoqRightNumberOfParametersRuleDescription), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MoqCompatibleArgumentTypeRuleTitle = new LocalizableResourceString(nameof(Resources.MoqCompatibleArgumentTypeRuleTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MoqCompatibleArgumentTypeRuleDescription = new LocalizableResourceString(nameof(Resources.MoqCompatibleArgumentTypeRuleDescription), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MoqCompatibleArgumentTypeRuleMessageFormat = new LocalizableResourceString(nameof(Resources.MoqCompatibleArgumentTypeRuleMessageFormat), Resources.ResourceManager, typeof(Resources));

        private const string Category = "Moq";

        private static DiagnosticDescriptor CallbackArgumentsNumberRule = new DiagnosticDescriptor("AZM0001", MoqRightNumberOfParametersRuleTitle, MoqRightNumberOfParametersRuleMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);
        private static DiagnosticDescriptor CallbackArgumentTypesRule = new DiagnosticDescriptor("AZM0002", MoqCompatibleArgumentTypeRuleTitle, MoqCompatibleArgumentTypeRuleMessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(CallbackArgumentsNumberRule, CallbackArgumentTypesRule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(MoqDetectWrongCallbackParameters, SyntaxKind.InvocationExpression);
        }

        private static void MoqDetectWrongCallbackParameters(SyntaxNodeAnalysisContext context)
        {

            var callbackOrReturnsInvocation = (InvocationExpressionSyntax)context.Node;
            // Ignoring Callback() and Return() calls without lambda arguments
            if (callbackOrReturnsInvocation.ArgumentList.Arguments.Count == 0) return;

            var callbackOrReturnsMethod = callbackOrReturnsInvocation.Expression as MemberAccessExpressionSyntax;
            var methodName = callbackOrReturnsMethod?.Name.ToString();
            if (methodName != "Callback" && methodName != "Returns") return;

            // Ignoring calls with no arguments because those are valid in Moq
            var callbackOrReturnsArgument = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;
            if (callbackOrReturnsArgument == null) return;
            if (callbackOrReturnsArgument.ParameterList.Parameters.Count == 0) return;

            var callbackOrReturnsMethodSymbol = context.SemanticModel.GetSymbolInfo(callbackOrReturnsMethod).Symbol as IMethodSymbol;
            if (callbackOrReturnsMethodSymbol == null) return;
            var callbackOrReturnsMethodName = callbackOrReturnsMethodSymbol.ToString();
            if (!callbackOrReturnsMethodName.StartsWith("Moq.Language.ICallback") && !callbackOrReturnsMethodName.StartsWith("Moq.Language.IReturns")) return; // TODO: Make more elegant
            var callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;

            var setupInvocation = FindSetupMethod(callbackOrReturnsMethod.Expression);
            if (setupInvocation == null) return;

            var setupLambdaArgument = setupInvocation.ArgumentList.Arguments[0]?.Expression as LambdaExpressionSyntax;
            if (setupLambdaArgument == null) return;

            var mockedMethodInvocation = setupLambdaArgument.Body as InvocationExpressionSyntax;
            var mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

            if (mockedMethodArguments.Count != callbackOrReturnsMethodArguments.Count)
            {
                var diagnostic = Diagnostic.Create(CallbackArgumentsNumberRule, callbackOrReturnsInvocation.ArgumentList.GetLocation(), mockedMethodInvocation.ArgumentList.Arguments.Count, callbackOrReturnsMethodArguments.Count);
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                for (int i = 0; i < mockedMethodArguments.Count; i++)
                {
                    var mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[i].Expression);
                    var callbackMethodArgumentType = context.SemanticModel.GetTypeInfo(callbackOrReturnsMethodArguments[i].Expression);
                    if (mockedMethodArgumentType.ConvertedType != callbackMethodArgumentType.ConvertedType)
                    {
                        var diagnostic = Diagnostic.Create(CallbackArgumentTypesRule, callbackOrReturnsMethodArguments[i].Expression.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static InvocationExpressionSyntax FindSetupMethod(ExpressionSyntax expression)
        {
            var invocation = expression as InvocationExpressionSyntax;
            var method = invocation?.Expression as MemberAccessExpressionSyntax;
            if (method == null) return null;
            var methodName = method?.Name.ToString();
            if (methodName == "Setup") return invocation;
            return FindSetupMethod(method.Expression);
        }

    }
}
