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

            var callbackOrReturnsMethodArguments = callbackOrReturnsInvocation.ArgumentList.Arguments;
            // Ignoring Callback() and Return() calls without lambda arguments
            if (callbackOrReturnsMethodArguments.Count == 0) return;

            if (!Helpers.IsCallbackOrReturnInvocation(context.SemanticModel, callbackOrReturnsInvocation)) return;

            var callbackLambda = callbackOrReturnsInvocation.ArgumentList.Arguments[0]?.Expression as ParenthesizedLambdaExpressionSyntax;
            
            // Ignoring callbacks without lambda
            if (callbackLambda == null) return;

            // Ignoring calls with no arguments because those are valid in Moq
            var lambdaParameters = callbackLambda.ParameterList.Parameters;
            if (lambdaParameters.Count == 0) return;

            var setupInvocation = Helpers.FindSetupMethodFromCallbackInvocation(context.SemanticModel, callbackOrReturnsInvocation);
            var mockedMethodInvocation = Helpers.FindMockedMethodInvocationFromSetupMethod(context.SemanticModel, setupInvocation);
            if (mockedMethodInvocation == null) return;

            var mockedMethodArguments = mockedMethodInvocation.ArgumentList.Arguments;

            if (mockedMethodArguments.Count != lambdaParameters.Count)
            {
                var diagnostic = Diagnostic.Create(CallbackArgumentsNumberRule, callbackOrReturnsInvocation.ArgumentList.GetLocation(), mockedMethodInvocation.ArgumentList.Arguments.Count, callbackOrReturnsMethodArguments.Count);
                context.ReportDiagnostic(diagnostic);
            }
            else
            {
                for (int i = 0; i < mockedMethodArguments.Count; i++)
                {
                    var mockedMethodArgumentType = context.SemanticModel.GetTypeInfo(mockedMethodArguments[i].Expression);
                    var lambdaParameterType = context.SemanticModel.GetTypeInfo(lambdaParameters[i].Type);
                    string mockedMethodTypeName = mockedMethodArgumentType.ConvertedType.ToString();
                    string lambdaParameterTypeName = lambdaParameterType.ConvertedType.ToString();
                    if (mockedMethodTypeName != lambdaParameterTypeName)
                    {
                        var diagnostic = Diagnostic.Create(CallbackArgumentTypesRule, lambdaParameters[i].Type.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}
