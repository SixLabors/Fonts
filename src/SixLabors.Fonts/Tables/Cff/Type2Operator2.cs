// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

namespace SixLabors.Fonts.Tables.Cff;

/// <summary>
/// Two-byte Type 2 charstring operators (preceded by the escape byte 12).
/// <see href="https://adobe-type-tools.github.io/font-tech-notes/pdfs/5177.Type2.pdf"/>
/// </summary>
internal enum Type2Operator2 : byte
{
    /// <summary>
    /// Reserved (0).
    /// </summary>
    Reserved0_ = 0,

    /// <summary>
    /// Reserved (1).
    /// </summary>
    Reserved1_,

    /// <summary>
    /// Reserved (2).
    /// </summary>
    Reserved2_,

    /// <summary>
    /// Logical AND (3). Pops two booleans, pushes their conjunction.
    /// </summary>
    And,

    /// <summary>
    /// Logical OR (4). Pops two booleans, pushes their disjunction.
    /// </summary>
    Or,

    /// <summary>
    /// Logical NOT (5). Pops a boolean, pushes its negation.
    /// </summary>
    Not,

    /// <summary>
    /// Reserved (6).
    /// </summary>
    Reserved6_,

    /// <summary>
    /// Reserved (7).
    /// </summary>
    Reserved7_,

    /// <summary>
    /// Reserved (8).
    /// </summary>
    Reserved8_,

    /// <summary>
    /// Absolute value (9).
    /// </summary>
    Abs,

    /// <summary>
    /// Addition (10). Pops two values, pushes their sum.
    /// </summary>
    Add,

    /// <summary>
    /// Subtraction (11). Pops two values, pushes their difference.
    /// </summary>
    Sub,

    /// <summary>
    /// Division (12). Pops two values, pushes their quotient.
    /// </summary>
    Div,

    /// <summary>
    /// Reserved (13).
    /// </summary>
    Reserved13_,

    /// <summary>
    /// Negation (14). Pops a value, pushes its negation.
    /// </summary>
    Neg,

    /// <summary>
    /// Equality (15). Pops two values, pushes 1 if equal, 0 otherwise.
    /// </summary>
    Eq,

    /// <summary>
    /// Reserved (16).
    /// </summary>
    Reserved16_,

    /// <summary>
    /// Reserved (17).
    /// </summary>
    Reserved17_,

    /// <summary>
    /// Drop (18). Removes the top element from the stack.
    /// </summary>
    Drop,

    /// <summary>
    /// Reserved (19).
    /// </summary>
    Reserved19_,

    /// <summary>
    /// Put (20). Stores a value in the transient array.
    /// </summary>
    Put,

    /// <summary>
    /// Get (21). Retrieves a value from the transient array.
    /// </summary>
    Get,

    /// <summary>
    /// If-else (22). Conditional operator.
    /// </summary>
    Ifelse,

    /// <summary>
    /// Random (23). Pushes a pseudo-random number.
    /// </summary>
    Random,

    /// <summary>
    /// Multiplication (24). Pops two values, pushes their product.
    /// </summary>
    Mul,

    /// <summary>
    /// Reserved (25).
    /// </summary>
    Reserved25_,

    /// <summary>
    /// Square root (26). Pops a value, pushes its square root.
    /// </summary>
    Sqrt,

    /// <summary>
    /// Duplicate (27). Duplicates the top stack element.
    /// </summary>
    Dup,

    /// <summary>
    /// Exchange (28). Swaps the top two elements on the argument stack.
    /// </summary>
    Exch,

    /// <summary>
    /// Index (29). Copies an indexed element to the top of the stack.
    /// </summary>
    Index,

    /// <summary>
    /// Roll (30). Rotates the top N stack elements.
    /// </summary>
    Roll,

    /// <summary>
    /// Reserved (31).
    /// </summary>
    Reserved31_,

    /// <summary>
    /// Reserved (32).
    /// </summary>
    Reserved32_,

    /// <summary>
    /// Reserved (33).
    /// </summary>
    Reserved33_,

    /// <summary>
    /// Horizontal flex (34). A flex mechanism for horizontal curves.
    /// </summary>
    Hflex,

    /// <summary>
    /// Flex (35). A general flex mechanism for curves.
    /// </summary>
    Flex,

    /// <summary>
    /// Horizontal flex variant 1 (36).
    /// </summary>
    Hflex1,

    /// <summary>
    /// Flex variant 1 (37). A general flex with more control points.
    /// </summary>
    Flex1
}
