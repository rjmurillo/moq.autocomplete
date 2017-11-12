using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Agent.Zorge.Moq
{
    [ExportCompletionProvider(nameof(MoqSuggestMocksForArguments), LanguageNames.CSharp)]
    public class MoqSuggestMocksForArguments : CompletionProvider
    {
        private CompletionItemRules _standardCompletionRules;
        private CompletionItemRules _preselectCompletionRules;
        // private Mock<List<string>>  

        public MoqSuggestMocksForArguments()
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

                var tokenAtCursor = Helpers.GetCurrentArgumentListSyntaxToken(syntaxRoot, context.Position);
                if (!tokenAtCursor.IsKind(SyntaxKind.OpenParenToken) && !tokenAtCursor.IsKind(SyntaxKind.CommaToken)) return;
                var argumentList = tokenAtCursor.Parent as ArgumentListSyntax;

                var allCommaTokens = argumentList.ChildTokens().Where(t => t.IsKind(SyntaxKind.CommaToken)).ToList();
                int argumentIdx = tokenAtCursor.IsKind(SyntaxKind.CommaToken) ? allCommaTokens.IndexOf(tokenAtCursor) + 1 : 0;
                var constructorCall = argumentList.Parent as ObjectCreationExpressionSyntax;
                var matchingConstructorSymbols = Helpers.GetAllMatchingSymbols<IMethodSymbol>(semanticModel, constructorCall);
                if (matchingConstructorSymbols == null) return;

                var matchingSymbols = new HashSet<ISymbol>();

                var symbols = semanticModel.LookupSymbols(context.Position);
                foreach (var symbol in symbols)
                {
                    INamedTypeSymbol symbolType;
                    if (symbol is IFieldSymbol)
                    {
                        var fieldSymbol = symbol as IFieldSymbol;
                        symbolType = fieldSymbol?.Type as INamedTypeSymbol;
                    }
                    else if (symbol is ILocalSymbol)
                    {
                        var localSymbol = symbol as ILocalSymbol;
                        symbolType = localSymbol.Type as INamedTypeSymbol;
                    }
                    else
                    {
                        continue;
                    }

                    if (symbolType == null || symbolType.ConstructedFrom == null) continue;
                    if (symbolType.ConstructedFrom.ToString() != "Moq.Mock<T>" || symbolType.TypeArguments.Length != 1) continue;
                    var typeArgument = symbolType.TypeArguments[0];
                    foreach (var methodSymbol in matchingConstructorSymbols.Where(m => m.Parameters.Length > argumentIdx))
                    {
                        if (methodSymbol.Parameters[argumentIdx].Type.ToString() == typeArgument.ToString())
                        {
                            matchingSymbols.Add(symbol);
                        }
                    }
                }

                foreach (var symbol in matchingSymbols)
                {
                    context.AddItem(CompletionItem.Create(symbol.Name + ".Object", rules: _preselectCompletionRules));
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }
}
