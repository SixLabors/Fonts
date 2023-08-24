// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

#nullable enable


namespace UnicodeTrieGenerator.StateAutomation;

internal class SymbolTable
{
    public SymbolTable(IList<INode> statements, Dictionary<string, int> externalSymbols)
    {
        this.Size = 0;
        this.AddExternalSymbols(externalSymbols);
        this.Process(statements);
    }

    public Dictionary<string, ILogicalNode> Variables { get; set; } = new();

    public Dictionary<string, int> Symbols { get; set; } = new();

    public int Size { get; set; }

    public ILogicalNode Main()
    {
        if (!this.Variables.TryGetValue(nameof(this.Main), out ILogicalNode? main))
        {
            throw new InvalidOperationException("No 'Main' variable declaration found");
        }

        return main;
    }

    private void AddExternalSymbols(Dictionary<string, int> externalSymbols)
    {
        foreach (string key in externalSymbols.Keys)
        {
            int symbol = externalSymbols[key];
            this.Variables[key] = new Literal(symbol);
            this.Symbols[key] = symbol;
            this.Size++;
        }
    }

    private void Process(IList<INode> statements)
    {
        foreach (INode statement in statements)
        {
            if (statement is Assignment assignment)
            {
                this.Variables[assignment.Variable.Name] = (ILogicalNode)this.ProcessExpression(assignment.Expression);

                if (assignment.Expression is Literal literal)
                {
                    this.Symbols[assignment.Variable.Name] = literal.Value;
                    this.Size++;
                }
            }
        }
    }

    private INode ProcessExpression(INode expression)
    {
        // Process children
        for (int i = 0; i < expression.Count; i++)
        {
            expression[i] = this.ProcessExpression(expression[i]);
        }

        // Replace variable references with their values
        if (expression is Variable variable)
        {
            ILogicalNode value = this.Variables[variable.Name];
            expression = this.ProcessExpression(value.Copy());
        }

        return expression;
    }
}
