//
//  ResolveVariablesAstVisitor.cs
//
//  Author:
//       scemino <scemino74@gmail.com>
//
//  Copyright (c) 2014 
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using System.Collections.Generic;

namespace NScumm.Dump
{
    public class ResolveVariablesAstVisitor: DefaultVisitor<IAstNode>
    {
        IDictionary<int, string> KnownVariables{ get; set; }

        public ResolveVariablesAstVisitor(IDictionary<int,string> knownVariables)
        {
            KnownVariables = knownVariables;
        }

        protected override IAstNode DefaultVisit(IAstNode node)
        {
            return node;
        }

        public override IAstNode Visit(BinaryExpression node)
        {
            return new BinaryExpression((Expression)node.Left.Accept(this), node.Operator, (Expression)node.Right.Accept(this));
        }

        public override IAstNode Visit(CompilationUnit node)
        {
            return new CompilationUnit((BlockStatement)node.Statement.Accept(this));
        }

        public override IAstNode Visit(ExpressionStatement node)
        {
            return new ExpressionStatement((Expression)node.Expression.Accept(this)){ StartOffset = node.StartOffset, EndOffset = node.EndOffset };
        }

        public override IAstNode Visit(MemberAccess node)
        {
            return new MemberAccess((Expression)node.Target.Accept(this), node.Field);
        }

        public override IAstNode Visit(UnaryExpression node)
        {
            return new UnaryExpression((Expression)node.Expression.Accept(this), node.Operator);
        }

        public override IAstNode Visit(MethodInvocation node)
        {
            return new MethodInvocation((Expression)node.Target.Accept(this)).AddArguments(node.Arguments.Select(arg => (Expression)arg.Accept(this)));
        }

        public override IAstNode Visit(JumpStatement node)
        {
            return new JumpStatement((Expression)node.Condition.Accept(this), node.JumpOffset){ StartOffset = node.StartOffset, EndOffset = node.EndOffset };
        }

        public override IAstNode Visit(ElementAccess node)
        {
            var name = node.Target as SimpleName;
            if (name != null && name.Name == "Variables" && node.Indices.Count == 1)
            {
                var index = node.Indices[0] as IntegerLiteralExpression;
                if (index != null)
                {
                    if (KnownVariables.ContainsKey(index.Value))
                    {
                        return new SimpleName(KnownVariables[index.Value]);
                    }
                }
            }
            return new ElementAccess((Expression)node.Target.Accept(this), node.Indices.Select(idx => (Expression)idx.Accept(this)));
        }
    }

}