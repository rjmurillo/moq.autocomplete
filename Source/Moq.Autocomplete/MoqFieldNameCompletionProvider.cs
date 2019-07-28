using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Threading.Tasks;

namespace Agent.Zorge.Moq
{
    [ExportCompletionProvider(nameof(MoqFieldNameCompletionProvider), LanguageNames.CSharp)]
    public class MoqFieldNameCompletionProvider : CompletionProvider
    {
        private CompletionItemRules _standardCompletionRules;
        private CompletionItemRules _preselectCompletionRules;
        // private Mock<List<string>>  

        public MoqFieldNameCompletionProvider()
        {
            _preselectCompletionRules = CompletionItemRules.Default.WithMatchPriority(MatchPriority.Preselect).WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
            _standardCompletionRules = CompletionItemRules.Default.WithSelectionBehavior(CompletionItemSelectionBehavior.SoftSelection);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            // wrap it in try catch, because exception for some reason sometimes crash entire VisualStudio
            try
            {
                if (!context.Document.SupportsSemanticModel) return;

                var syntaxRoot = await context.Document.GetSyntaxRootAsync();
                var semanticModel = await context.Document.GetSemanticModelAsync();

                var tokenAtCursor = Helpers.GetCurrentTypeArgumentList(syntaxRoot, context.Position);
                if (!tokenAtCursor.IsKind(SyntaxKind.GreaterThanToken)) return;
                var typeArgumentList = tokenAtCursor.Parent as TypeArgumentListSyntax;
                // We are aiming Mock<T>, so can do shortcut exit here for mismatches
                if (typeArgumentList.Arguments.Count != 1) return;
                var genericName = typeArgumentList?.Parent as GenericNameSyntax;
                // TODO: Yuk! Sometimes additional parent appears.
                var memberSyntax = genericName?.Parent as IncompleteMemberSyntax ?? genericName?.Parent?.Parent as IncompleteMemberSyntax;
                var classDeclaration = memberSyntax?.Parent as ClassDeclarationSyntax;
                // Ignore if not a field/method declaration
                if (classDeclaration == null) return;

                var typeInfo = semanticModel.GetTypeInfo(genericName);
                var type = typeInfo.Type as INamedTypeSymbol;
                if (type == null || type.ConstructedFrom == null) return;
                if (type.ConstructedFrom.ToString() != "Moq.Mock<T>") return;

                var mockedType = typeArgumentList.Arguments[0];
                var mockedTypeInfo = semanticModel.GetTypeInfo(mockedType);
                if (mockedTypeInfo.Type == null) return;
                var mockedTypeName = mockedTypeInfo.Type.Name;
                if (mockedTypeName.Length > 1 && mockedTypeName[0] == 'I' && Char.IsUpper(mockedTypeName[1])) // ISomething
                {
                    mockedTypeName = mockedTypeName.Substring(1);
                }
                context.AddItem(CompletionItem.Create(mockedTypeName.Substring(0, 1).ToLower() + mockedTypeName.Substring(1) + "Mock", rules: _preselectCompletionRules));
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
