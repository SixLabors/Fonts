// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnicodeTrieGenerator.Dfa
{
    /// <summary>
    /// Defines an AST node.
    /// </summary>
    internal interface INode
    {
        /// <summary>
        /// Gets the following position.
        /// </summary>
        HashSet<INode> FollowPos { get; }

        /// <summary>
        /// Gets a value indicating whether this node is nullable.
        /// </summary>
        bool Nullable { get; }

        /// <summary>
        /// Calculates the follow position for this instance.
        /// </summary>
        void CalcFollowPos();

        /// <summary>
        /// Returns a copy of the node.
        /// </summary>
        /// <returns>The <see cref="INode"/>.</returns>
        INode Copy();
    }

    /// <summary>
    /// Defines a logical AST node.
    /// </summary>
    internal interface ILogicalNode : INode
    {
        /// <summary>
        /// Gets the collection of nodes as the first position.
        /// </summary>
        HashSet<INode> FirstPos { get; }

        /// <summary>
        /// Gets the collection of nodes at the last position.
        /// </summary>
        HashSet<INode> LastPos { get; }
    }

    /// <summary>
    /// The base AST node.
    /// </summary>
    internal abstract class Node : INode
    {
        /// <inheritdoc/>
        public HashSet<INode> FollowPos { get; } = new();

        /// <inheritdoc/>
        public virtual bool Nullable => false;

        /// <inheritdoc/>
        public abstract void CalcFollowPos();

        /// <inheritdoc/>
        public abstract INode Copy();
    }

    /// <summary>
    /// Represents a variable reference.
    /// </summary>
    internal class Variable : Node, ILogicalNode
    {
        public Variable(string name) => this.Name = name;

        public string Name { get; }

        /// <inheritdoc/>
        HashSet<INode> ILogicalNode.FirstPos { get; } = new HashSet<INode>();

        /// <inheritdoc/>
        HashSet<INode> ILogicalNode.LastPos { get; } = new HashSet<INode>();

        /// <inheritdoc/>
        public override void CalcFollowPos() => throw new NotImplementedException();

        /// <inheritdoc/>
        public override INode Copy() => new Variable(this.Name);
    }

    /// <summary>
    /// Represents a comment.
    /// </summary>
    internal class Comment : Node
    {
        public Comment(string value) => this.Value = value;

        public string Value { get; }

        /// <inheritdoc/>
        public override void CalcFollowPos() => throw new NotImplementedException();

        /// <inheritdoc/>
        public override INode Copy() => new Comment(this.Value);
    }

    /// <summary>
    /// Represents an assignment statement. e.g. `variable = expression;`
    /// </summary>
    internal class Assignment : Node
    {
        public Assignment(Variable variable, INode expression)
        {
            this.Variable = variable;
            this.Expression = expression;
        }

        public Variable Variable { get; }

        public INode Expression { get; }

        /// <inheritdoc/>
        public override void CalcFollowPos()
        {
            this.Variable.CalcFollowPos();
            this.Expression.CalcFollowPos();
        }

        /// <inheritdoc/>
        public override INode Copy() => new Assignment(this.Variable, this.Expression);
    }

    /// <summary>
    /// Represents an alternation. e.g. `a | b`
    /// </summary>
    internal class Alternation : Node, ILogicalNode
    {
        public Alternation(ILogicalNode a, ILogicalNode b)
        {
            this.A = a;
            this.B = b;
        }

        public ILogicalNode A { get; }

        public ILogicalNode B { get; }

        /// <inheritdoc/>
        public override bool Nullable => this.A.Nullable || this.B.Nullable;

        /// <inheritdoc/>
        public HashSet<INode> FirstPos => NodeUtilities.Union(this.A.FirstPos, this.B.FirstPos);

        /// <inheritdoc/>
        public HashSet<INode> LastPos => NodeUtilities.Union(this.A.LastPos, this.B.LastPos);

        /// <inheritdoc/>
        public override void CalcFollowPos()
        {
            this.A.CalcFollowPos();
            this.B.CalcFollowPos();
        }

        /// <inheritdoc/>
        public override INode Copy()
            => new Alternation((ILogicalNode)this.A.Copy(), (ILogicalNode)this.B.Copy());
    }

    /// <summary>
    /// Represents a concatenation, or chain. e.g. `a b c`
    /// </summary>
    internal class Concatenation : Node, ILogicalNode
    {
        public Concatenation(ILogicalNode a, ILogicalNode b)
        {
            this.A = a;
            this.B = b;
        }

        public ILogicalNode A { get; }

        public ILogicalNode B { get; }

        /// <inheritdoc/>
        public override bool Nullable => this.A.Nullable && this.B.Nullable;

        /// <inheritdoc/>
        public HashSet<INode> FirstPos
        {
            get
            {
                HashSet<INode> s = this.A.FirstPos;
                if (this.A.Nullable)
                {
                    s = NodeUtilities.Union(s, this.B.FirstPos);
                }

                return s;
            }
        }

        /// <inheritdoc/>
        public HashSet<INode> LastPos
        {
            get
            {
                HashSet<INode> s = this.B.LastPos;
                if (this.B.Nullable)
                {
                    s = NodeUtilities.Union(s, this.A.LastPos);
                }

                return s;
            }
        }

        /// <inheritdoc/>
        public override void CalcFollowPos()
        {
            foreach (INode n in this.A.LastPos)
            {
                NodeUtilities.AddAll(n.FollowPos, this.B.FirstPos);
            }
        }

        /// <inheritdoc/>
        public override INode Copy()
            => new Concatenation((ILogicalNode)this.A.Copy(), (ILogicalNode)this.B.Copy());
    }

    /// <summary>
    /// Represents a repetition. e.g. `a+`, `b*`, or `c?`
    /// </summary>
    internal class Repeat : Node, ILogicalNode
    {
        public Repeat(ILogicalNode expression, string op)
        {
            this.Expression = expression;
            this.Op = op;
        }

        public ILogicalNode Expression { get; }

        public string Op { get; }

        /// <inheritdoc/>
        public override bool Nullable => this.Op is "*" or "?";

        /// <inheritdoc/>
        public HashSet<INode> FirstPos => this.Expression.FirstPos;

        /// <inheritdoc/>
        public HashSet<INode> LastPos => this.Expression.LastPos;

        /// <inheritdoc/>
        public override void CalcFollowPos()
        {
            if (this.Op is "*" or "+")
            {
                foreach (INode n in this.LastPos)
                {
                    NodeUtilities.AddAll(n.FollowPos, this.FirstPos);
                }
            }
        }

        /// <inheritdoc/>
        public override INode Copy()
            => new Repeat((ILogicalNode)this.Expression.Copy(), this.Op);
    }

    /// <summary>
    /// Base class for leaf nodes.
    /// </summary>
    internal abstract class Leaf : Node, ILogicalNode
    {
        protected Leaf()
        {
            this.FirstPos = new HashSet<INode> { this };
            this.LastPos = new HashSet<INode> { this };
        }

        /// <inheritdoc/>
        public HashSet<INode> FirstPos { get; }

        /// <inheritdoc/>
        public HashSet<INode> LastPos { get; }

        /// <inheritdoc/>
        public override void CalcFollowPos() => throw new NotImplementedException();
    }

    /// <summary>
    /// Represents a literal value, e.g. a number.
    /// </summary>
    internal class Literal : Leaf
    {
        public Literal(int value) => this.Value = value;

        public int Value { get; }

        /// <inheritdoc/>
        public override INode Copy() => new Literal(this.Value);
    }

    /// <summary>
    /// Marks the end of an expression.
    /// </summary>
    internal class EndMarker : Leaf
    {
        /// <inheritdoc/>
        public override INode Copy() => throw new NotImplementedException();
    }

    /// <summary>
    /// Represents a tag e.g. `a:(a b)`.
    /// </summary>
    internal class Tag : Leaf
    {
        public Tag(string value) => this.Name = value;

        public string Name { get; }

        /// <inheritdoc/>
        public override INode Copy() => new Tag(this.Name);
    }

    internal static class NodeUtilities
    {
        /// <summary>
        /// Builds a repetition of the given expression.
        /// </summary>
        /// <param name="expression">The expression to repeat.</param>
        /// <param name="min">The minimum value to repeat.</param>
        /// <param name="max">The maximum number to repeat.</param>
        /// <returns>THe <see cref="ILogicalNode"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="min"/> is out of range.</exception>
        public static ILogicalNode BuildRepetition(ILogicalNode expression, int min, double max = double.PositiveInfinity)
        {
            if (min < 0 || min > max)
            {
                throw new ArgumentOutOfRangeException(nameof(min), $"Invalid repetition range: {min} {max}");
            }

            ILogicalNode? result = null;
            for (int i = 0; i < min; i++)
            {
                result = Concat(result, (ILogicalNode)expression.Copy());
            }

            return max == double.PositiveInfinity
                ? Concat(result, new Repeat((ILogicalNode)expression.Copy(), "*"))
                : Concat(result, new Repeat((ILogicalNode)expression.Copy(), "?"));
        }

        /// <summary>
        /// COncatinates two nodes.
        /// </summary>
        /// <param name="a">The first node.</param>
        /// <param name="b">The second node.</param>
        /// <returns>The combined <see cref="ILogicalNode"/>.</returns>
        public static ILogicalNode Concat(ILogicalNode? a, ILogicalNode b)
        {
            if (a is null)
            {
                return b;
            }

            return new Concatenation(a, b);
        }

        /// <summary>
        /// Creates a union of two node sequences.
        /// </summary>
        /// <param name="a">The first node sequence.</param>
        /// <param name="b">The second node sequence.</param>
        /// <returns>The <see cref="HashSet{INode}"/>.</returns>
        public static HashSet<INode> Union(HashSet<INode> a, HashSet<INode> b)
        {
            var s = new HashSet<INode>(a);
            AddAll(s, b);
            return s;
        }

        /// <summary>
        /// Adds all the elements from set <paramref name="b"/> to <paramref name="a"/>.
        /// </summary>
        /// <param name="a">The first node sequence.</param>
        /// <param name="b">The second node sequence.</param>
        public static void AddAll(HashSet<INode> a, HashSet<INode> b)
        {
            foreach (INode n in b)
            {
                _ = a.Add(n);
            }
        }

        /// <summary>
        /// Determines whether two sets are equal.
        /// </summary>
        /// <param name="a">The first node sequence.</param>
        /// <param name="b">The second node sequence.</param>
        /// <returns>
        /// <see langword="true"/> if the two source sequences are of equal length and their corresponding
        /// elements are equal according to the default equality comparer for their type;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Equal(ICollection<INode> a, ICollection<INode> b) => a.SequenceEqual(b);
    }
}